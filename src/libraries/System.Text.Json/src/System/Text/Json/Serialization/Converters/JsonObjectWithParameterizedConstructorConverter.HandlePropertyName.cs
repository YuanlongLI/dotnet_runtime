﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed partial class JsonObjectWithParameterizedConstructorConverter<T> : JsonObjectConverter<T>
    {
        private const int ParameterNameKeyLength = 7;

        // The limit to how many constructor parameter names from the JSON are cached in _parameterRefsSorted before using _parameterCache.
        private const int ParameterNameCountCacheThreshold = 64;

        // Fast cache of constructor parameters by first JSON ordering; may not contain all parameters. Accessed before _parameterCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile ParameterRef[]? _parameterRefsSorted;

        private const int PropertyNameKeyLength = 7;

        // The limit to how many property names from the JSON are cached in _propertyRefsSorted before using PropertyCache.
        private const int PropertyNameCountCacheThreshold = 64;

        // Fast cache of properties by first JSON ordering; may not contain all properties. Accessed before PropertyCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile PropertyRef[]? _propertyRefsSorted;

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        // AggressiveInlining used although a large method it is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryLookupConstructorParameter(
            ref Utf8JsonReader reader,
            ref ReadStack state,
            out JsonParameterInfo? jsonParameterInfo,
            out ReadOnlySpan<byte> unescapedPropertyName,
            out string? unescapedStringPropertyName,
            JsonSerializerOptions options)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            ReadOnlySpan<byte> escapedPropertyName = JsonSerializer.GetSpan(ref reader);

            if (reader._stringHasEscaping)
            {
                int idx = escapedPropertyName.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(idx != -1);
                unescapedPropertyName = JsonSerializer.GetUnescapedString(escapedPropertyName, idx);
            }
            else
            {
                unescapedPropertyName = escapedPropertyName;
            }

            if (options.ReferenceHandling.ShouldReadPreservedReferences())
            {
                if (escapedPropertyName.Length > 0 && escapedPropertyName[0] == '$')
                {
                    JsonSerializer.ThrowUnexpectedMetadataException(escapedPropertyName, ref reader, ref state);
                }
            }

            if (TryGetParameter(unescapedPropertyName, ref state.Current, out jsonParameterInfo, out unescapedStringPropertyName, options))
            {
                // Increment ConstructorParameterIndex so GetProperty() starts with the next parameter the next time this function is called.
                Debug.Assert(jsonParameterInfo != null);

                state.Current.ConstructorParameterIndex++;
                state.Current.JsonConstructorParameterInfo = jsonParameterInfo;

                // Set state.Current.JsonPropertyInfo to null so there's no conflict on state.Push()
                state.Current.JsonPropertyInfo = null!;

                return true;
            }

            return false;

            //// Determine if we should use the extension property.
            //if (jsonParameterInfo == JsonPropertyInfo.s_missingProperty)
            //{
            //    JsonPropertyInfo? dataExtProperty = state.Current.JsonClassInfo.DataExtensionProperty;
            //    if (dataExtProperty != null)
            //    {
            //        state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
            //        CreateDataExtensionProperty(obj, dataExtProperty);
            //        jsonParameterInfo = dataExtProperty;
            //    }

            //    state.Current.JsonPropertyInfo = jsonParameterInfo;
            //    useExtensionProperty = true;
            //    return;
            //}

            //// Support JsonException.Path.
            //Debug.Assert(
            //    jsonParameterInfo.JsonPropertyName == null ||
            //    options.PropertyNameCaseInsensitive ||
            //    unescapedPropertyName.SequenceEqual(jsonParameterInfo.JsonPropertyName));

            //state.Current.JsonPropertyInfo = jsonParameterInfo;

            //if (jsonParameterInfo.JsonPropertyName == null)
            //{
            //    byte[] propertyNameArray = unescapedPropertyName.ToArray();
            //    if (options.PropertyNameCaseInsensitive)
            //    {
            //        // Each payload can have a different name here; remember the value on the temporary stack.
            //        state.Current.JsonPropertyName = propertyNameArray;
            //    }
            //    else
            //    {
            //        // Prevent future allocs by caching globally on the JsonPropertyInfo which is specific to a Type+PropertyName
            //        // so it will match the incoming payload except when case insensitivity is enabled (which is handled above).
            //        state.Current.JsonPropertyInfo.JsonPropertyName = propertyNameArray;
            //    }
            //}

            //state.Current.JsonPropertyInfo = jsonParameterInfo;
            //useExtensionProperty = false;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetParameter(
            ReadOnlySpan<byte> propertyName,
            ref ReadStackFrame frame,
            out JsonParameterInfo? jsonParameterInfo,
            out string? stringPropertyName,
            JsonSerializerOptions options)
        {
            JsonParameterInfo? info = null;

            // Keep a local copy of the cache in case it changes by another thread.
            ParameterRef[]? localParameterRefsSorted = _parameterRefsSorted;

            ulong key = JsonClassInfo.GetKey(propertyName);

            // If there is an existing cache, then use it.
            if (localParameterRefsSorted != null)
            {
                // Start with the current parameter index, and then go forwards\backwards.
                int parameterIndex = frame.ConstructorParameterIndex;

                int count = localParameterRefsSorted.Length;
                int iForward = Math.Min(parameterIndex, count);
                int iBackward = iForward - 1;

                while (true)
                {
                    if (iForward < count)
                    {
                        ParameterRef parameterRef = localParameterRefsSorted[iForward];
                        if (TryIsParameterRefEqual(parameterRef, propertyName, key, ref info))
                        {
                            stringPropertyName = null;
                            jsonParameterInfo = info;
                            return true;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            parameterRef = localParameterRefsSorted[iBackward];
                            if (TryIsParameterRefEqual(parameterRef, propertyName, key, ref info))
                            {
                                stringPropertyName = null;
                                jsonParameterInfo = info;
                                return true;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        ParameterRef parameterRef = localParameterRefsSorted[iBackward];
                        if (TryIsParameterRefEqual(parameterRef, propertyName, key, ref info))
                        {
                            stringPropertyName = null;
                            jsonParameterInfo = info;
                            return true;
                        }

                        --iBackward;
                    }
                    else
                    {
                        // Property was not found.
                        break;
                    }
                }
            }

            // No cached item was found. Try the main list which has all of the properties.

            stringPropertyName = JsonHelpers.Utf8GetString(propertyName);

            Debug.Assert(_parameterCache != null);

            if (!_parameterCache.TryGetValue(stringPropertyName, out info))
            {
                // Constructor parameter not found. We'll check if it's a property next.
                jsonParameterInfo = null;
                return false;
            }

            jsonParameterInfo = info;
            Debug.Assert(info != null);

            // Two code paths to get here:
            // 1) key == info.PropertyNameKey. Exact match found.
            // 2) key != info.PropertyNameKey. Match found due to case insensitivity.
            Debug.Assert(key == info.ParameterNameKey || stringPropertyName.Equals(info.NameAsString, StringComparison.OrdinalIgnoreCase));

            // Check if we should add this to the cache.
            // Only cache up to a threshold length and then just use the dictionary when an item is not found in the cache.
            int cacheCount = 0;
            if (localParameterRefsSorted != null)
            {
                cacheCount = localParameterRefsSorted.Length;
            }

            // Do a quick check for the stable (after warm-up) case.
            if (cacheCount < ParameterNameCountCacheThreshold)
            {
                // Do a slower check for the warm-up case.
                if (frame.ParameterRefCache != null)
                {
                    cacheCount += frame.ParameterRefCache.Count;
                }

                // Check again to append the cache up to the threshold.
                if (cacheCount < ParameterNameCountCacheThreshold)
                {
                    if (frame.ParameterRefCache == null)
                    {
                        frame.ParameterRefCache = new List<ParameterRef>();
                    }

                    ParameterRef parameterRef = new ParameterRef(key, jsonParameterInfo);
                    frame.ParameterRefCache.Add(parameterRef);
                }
            }

            return true;
        }

        /// <summary>
        /// Lookup the property given its name in the reader.
        /// </summary>
        // AggressiveInlining used although a large method it is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LookupProperty(
            ref ReadStack state,
            out JsonPropertyInfo jsonPropertyInfo,
            out bool useExtensionProperty,
            JsonSerializerOptions options,
            ReadOnlySpan<byte> propertyName,
            string stringPropertyName)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            jsonPropertyInfo = GetProperty(ref state.Current, propertyName, options, stringPropertyName);

            // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
            state.Current.PropertyIndex++;

            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                if (_dataExtensionProperty != null)
                {
                    state.Current.JsonPropertyNameAsString = stringPropertyName;
                    jsonPropertyInfo = _dataExtensionProperty;
                }

                state.Current.JsonPropertyInfo = jsonPropertyInfo;
                useExtensionProperty = true;
                return;
            }

            // Support JsonException.Path.
            Debug.Assert(
                jsonPropertyInfo.JsonPropertyName == null ||
                options.PropertyNameCaseInsensitive ||
                propertyName.SequenceEqual(jsonPropertyInfo.JsonPropertyName));

            state.Current.JsonPropertyInfo = jsonPropertyInfo;

            if (jsonPropertyInfo.JsonPropertyName == null)
            {
                byte[] propertyNameArray = propertyName.ToArray();
                if (options.PropertyNameCaseInsensitive)
                {
                    // Each payload can have a different name here; remember the value on the temporary stack.
                    state.Current.JsonPropertyName = propertyNameArray;
                }
                else
                {
                    // Prevent future allocs by caching globally on the JsonPropertyInfo which is specific to a Type+PropertyName
                    // so it will match the incoming payload except when case insensitivity is enabled (which is handled above).
                    state.Current.JsonPropertyInfo.JsonPropertyName = propertyNameArray;
                }
            }

            state.Current.JsonPropertyInfo = jsonPropertyInfo;
            useExtensionProperty = false;
        }

        /// <summary>
        /// Lookup the property given its name in the reader.
        /// </summary>
        // AggressiveInlining used although a large method it is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LookupProperty(
            ref Utf8JsonReader reader,
            ref ReadStack state,
            out JsonPropertyInfo jsonPropertyInfo,
            out bool useExtensionProperty,
            JsonSerializerOptions options)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            ReadOnlySpan<byte> escapedPropertyName = JsonSerializer.GetSpan(ref reader);
            ReadOnlySpan<byte> unescapedPropertyName;

            if (reader._stringHasEscaping)
            {
                int idx = escapedPropertyName.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(idx != -1);
                unescapedPropertyName = JsonSerializer.GetUnescapedString(escapedPropertyName, idx);
            }
            else
            {
                unescapedPropertyName = escapedPropertyName;
            }

            if (options.ReferenceHandling.ShouldReadPreservedReferences())
            {
                if (escapedPropertyName.Length > 0 && escapedPropertyName[0] == '$')
                {
                    JsonSerializer.ThrowUnexpectedMetadataException(escapedPropertyName, ref reader, ref state);
                }
            }

            jsonPropertyInfo = GetProperty(ref state.Current, unescapedPropertyName, options);

            // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
            state.Current.PropertyIndex++;

            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                if (_dataExtensionProperty != null)
                {
                    state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
                    jsonPropertyInfo = _dataExtensionProperty;
                }

                state.Current.JsonPropertyInfo = jsonPropertyInfo;
                useExtensionProperty = true;
                return;
            }

            // Support JsonException.Path.
            Debug.Assert(
                jsonPropertyInfo.JsonPropertyName == null ||
                options.PropertyNameCaseInsensitive ||
                unescapedPropertyName.SequenceEqual(jsonPropertyInfo.JsonPropertyName));

            state.Current.JsonPropertyInfo = jsonPropertyInfo;

            if (jsonPropertyInfo.JsonPropertyName == null)
            {
                byte[] propertyNameArray = unescapedPropertyName.ToArray();
                if (options.PropertyNameCaseInsensitive)
                {
                    // Each payload can have a different name here; remember the value on the temporary stack.
                    state.Current.JsonPropertyName = propertyNameArray;
                }
                else
                {
                    // Prevent future allocs by caching globally on the JsonPropertyInfo which is specific to a Type+PropertyName
                    // so it will match the incoming payload except when case insensitivity is enabled (which is handled above).
                    state.Current.JsonPropertyInfo.JsonPropertyName = propertyNameArray;
                }
            }

            state.Current.JsonPropertyInfo = jsonPropertyInfo;
            useExtensionProperty = false;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonPropertyInfo GetProperty(ref ReadStackFrame frame, ReadOnlySpan<byte> propertyName, JsonSerializerOptions options, string? stringPropertyName = null)
        {
            JsonPropertyInfo? info = null;

            // Keep a local copy of the cache in case it changes by another thread.
            PropertyRef[]? localPropertyRefsSorted = _propertyRefsSorted;

            ulong key = JsonClassInfo.GetKey(propertyName);

            // If there is an existing cache, then use it.
            if (localPropertyRefsSorted != null)
            {
                // Start with the current property index, and then go forwards\backwards.
                int propertyIndex = frame.PropertyIndex;

                int count = localPropertyRefsSorted.Length;
                int iForward = Math.Min(propertyIndex, count);
                int iBackward = iForward - 1;

                while (true)
                {
                    if (iForward < count)
                    {
                        PropertyRef propertyRef = localPropertyRefsSorted[iForward];
                        if (TryIsPropertyRefEqual(propertyRef, propertyName, key, ref info))
                        {
                            return info;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            propertyRef = localPropertyRefsSorted[iBackward];
                            if (TryIsPropertyRefEqual(propertyRef, propertyName, key, ref info))
                            {
                                return info;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        PropertyRef propertyRef = localPropertyRefsSorted[iBackward];
                        if (TryIsPropertyRefEqual(propertyRef, propertyName, key, ref info))
                        {
                            return info;
                        }

                        --iBackward;
                    }
                    else
                    {
                        // Property was not found.
                        break;
                    }
                }
            }

            // No cached item was found. Try the main list which has all of the properties.

            if (stringPropertyName == null)
            {
                stringPropertyName = JsonHelpers.Utf8GetString(propertyName);
            }

            Debug.Assert(_propertyCache != null);

            if (!_propertyCache.TryGetValue(stringPropertyName, out info))
            {
                info = JsonPropertyInfo.s_missingProperty;
            }

            Debug.Assert(info != null);

            // Three code paths to get here:
            // 1) info == s_missingProperty. Property not found.
            // 2) key == info.PropertyNameKey. Exact match found.
            // 3) key != info.PropertyNameKey. Match found due to case insensitivity.
            Debug.Assert(info == JsonPropertyInfo.s_missingProperty || key == info.PropertyNameKey || options.PropertyNameCaseInsensitive);

            // Check if we should add this to the cache.
            // Only cache up to a threshold length and then just use the dictionary when an item is not found in the cache.
            int cacheCount = 0;
            if (localPropertyRefsSorted != null)
            {
                cacheCount = localPropertyRefsSorted.Length;
            }

            // Do a quick check for the stable (after warm-up) case.
            if (cacheCount < PropertyNameCountCacheThreshold)
            {
                // Do a slower check for the warm-up case.
                if (frame.PropertyRefCache != null)
                {
                    cacheCount += frame.PropertyRefCache.Count;
                }

                // Check again to append the cache up to the threshold.
                if (cacheCount < PropertyNameCountCacheThreshold)
                {
                    if (frame.PropertyRefCache == null)
                    {
                        frame.PropertyRefCache = new List<PropertyRef>();
                    }

                    PropertyRef propertyRef = new PropertyRef(key, info);
                    frame.PropertyRefCache.Add(propertyRef);
                }
            }

            return info;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryIsParameterRefEqual(in ParameterRef parameterRef, ReadOnlySpan<byte> parameterName, ulong key, [NotNullWhen(true)] ref JsonParameterInfo? info)
        {
            if (key == parameterRef.Key)
            {
                // We compare the whole name, although we could skip the first 7 bytes (but it's not any faster)
                if (parameterName.Length <= ParameterNameKeyLength ||
                    parameterName.SequenceEqual(parameterRef.Info.ParameterName))
                {
                    info = parameterRef.Info;
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryIsPropertyRefEqual(in PropertyRef propertyRef, ReadOnlySpan<byte> propertyName, ulong key, [NotNullWhen(true)] ref JsonPropertyInfo? info)
        {
            if (key == propertyRef.Key)
            {
                // We compare the whole name, although we could skip the first 7 bytes (but it's not any faster)
                if (propertyName.Length <= PropertyNameKeyLength ||
                    propertyName.SequenceEqual(propertyRef.Info.Name))
                {
                    info = propertyRef.Info;
                    return true;
                }
            }

            return false;
        }

        public void UpdateSortedParameterCache(ref ReadStackFrame frame)
        {
            Debug.Assert(frame.ParameterRefCache != null);

            // frame.PropertyRefCache is only read\written by a single thread -- the thread performing
            // the deserialization for a given object instance.

            List<ParameterRef> listToAppend = frame.ParameterRefCache;

            // _parameterRefsSorted can be accessed by multiple threads, so replace the reference when
            // appending to it. No lock() is necessary.

            if (_parameterRefsSorted != null)
            {
                List<ParameterRef> replacementList = new List<ParameterRef>(_parameterRefsSorted);
                Debug.Assert(replacementList.Count <= ParameterNameCountCacheThreshold);

                // Verify replacementList will not become too large.
                while (replacementList.Count + listToAppend.Count > ParameterNameCountCacheThreshold)
                {
                    // This code path is rare; keep it simple by using RemoveAt() instead of RemoveRange() which requires calculating index\count.
                    listToAppend.RemoveAt(listToAppend.Count - 1);
                }

                // Add the new items; duplicates are possible but that is tolerated during property lookup.
                replacementList.AddRange(listToAppend);
                _parameterRefsSorted = replacementList.ToArray();
            }
            else
            {
                _parameterRefsSorted = listToAppend.ToArray();
            }

            frame.PropertyRefCache = null;
        }

        private void UpdateSortedPropertyCache(ref ReadStackFrame frame)
        {
            Debug.Assert(frame.PropertyRefCache != null);

            // frame.PropertyRefCache is only read\written by a single thread -- the thread performing
            // the deserialization for a given object instance.

            List<PropertyRef> listToAppend = frame.PropertyRefCache;

            // _propertyRefsSorted can be accessed by multiple threads, so replace the reference when
            // appending to it. No lock() is necessary.

            if (_propertyRefsSorted != null)
            {
                List<PropertyRef> replacementList = new List<PropertyRef>(_propertyRefsSorted);
                Debug.Assert(replacementList.Count <= PropertyNameCountCacheThreshold);

                // Verify replacementList will not become too large.
                while (replacementList.Count + listToAppend.Count > PropertyNameCountCacheThreshold)
                {
                    // This code path is rare; keep it simple by using RemoveAt() instead of RemoveRange() which requires calculating index\count.
                    listToAppend.RemoveAt(listToAppend.Count - 1);
                }

                // Add the new items; duplicates are possible but that is tolerated during property lookup.
                replacementList.AddRange(listToAppend);
                _propertyRefsSorted = replacementList.ToArray();
            }
            else
            {
                _propertyRefsSorted = listToAppend.ToArray();
            }

            frame.PropertyRefCache = null;
        }
    }
}
