// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
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
    internal abstract partial class ObjectWithParameterizedConstructorConverter<TypeToConvert> : JsonObjectConverter<TypeToConvert>
    {
        private const int JsonPropertyNameKeyLength = 7;

        // The limit to how many constructor parameter names from the JSON are cached in _parameterRefsSorted before using _parameterCache.
        private const int ParameterNameCountCacheThreshold = 32;

        // Fast cache of constructor parameters by first JSON ordering; may not contain all parameters. Accessed before _parameterCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile ParameterRef[]? _parameterRefsSorted;

        // The limit to how many property names from the JSON are cached in _propertyRefsSorted before using PropertyCache.
        protected const int PropertyNameCountCacheThreshold = 64;

        // Fast cache of properties by first JSON ordering; may not contain all properties. Accessed before PropertyCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile PropertyRef[]? _propertyRefsSorted;

        protected void ReadConstructorArguments(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

            // This tells us whether we have found the first property in the JSON which does not match to a constructor argument.
            bool foundProperty = false;

            // The state of the reader to resume from on the second pass for properties
            JsonReaderState resumptionState = default!;
            long resumptionByteIndex = -1;

            while (true)
            {
                if (!foundProperty)
                {
                    resumptionState = reader.CurrentState;
                    resumptionByteIndex = reader.BytesConsumed;
                }

                // Read the property name or EndObject.
                reader.Read();

                JsonTokenType tokenType = reader.TokenType;
                if (tokenType == JsonTokenType.EndObject)
                {
                    PositionReader(ref state, ref reader, foundProperty, ref resumptionState, resumptionByteIndex);
                    return;
                }

                if (tokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(base.TypeToConvert);
                }

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

                if (!TryLookupConstructorParameterFromFastCache(
                    ref state,
                    unescapedPropertyName,
                    out JsonParameterInfo? jsonParameterInfo))
                {
                    if (TryGetPropertyFromFastCache(
                        ref state.Current,
                        unescapedPropertyName,
                        out JsonPropertyInfo? jsonPropertyInfo))
                    {
                        if (!foundProperty)
                        {
                            state.Current.FirstPropertyIndex = state.Current.ConstructorParameterIndex;
                            foundProperty = true;
                        }

                        Debug.Assert(jsonPropertyInfo != null);

                        // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                        state.Current.PropertyIndex++;

                        // Skip the value of this property, we'll read it on the second pass.
                        reader.Skip();
                        continue;
                    }
                    else if (!TryLookupConstructorParameterFromSlowCache(
                        ref state,
                        unescapedPropertyName,
                        out string unescapedPropertyNameAsString,
                        out jsonParameterInfo))
                    {
                        if (!foundProperty)
                        {
                            state.Current.FirstPropertyIndex = state.Current.ConstructorParameterIndex;
                            foundProperty = true;
                        }

                        jsonPropertyInfo = GetPropertyFromSlowCache(
                            ref state.Current,
                            unescapedPropertyName,
                            unescapedPropertyNameAsString,
                            options);

                        Debug.Assert(jsonPropertyInfo != null);

                        // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                        state.Current.PropertyIndex++;

                        // Skip the value of this property, we'll read it on the second pass.
                        reader.Skip();
                        continue;
                    }
                }

                Debug.Assert(jsonParameterInfo != null);
                int position = jsonParameterInfo.Position;

                Debug.Assert(state.Current.ConstructorArgumentState != null);

                if (state.Current.ConstructorArgumentState[position])
                {
                    // Maintain first-one-wins semantics for performance.
                    reader.Skip();
                    continue;
                }

                state.Current.ConstructorArgumentState[jsonParameterInfo.Position] = true;

                // Set the property value.
                reader.Read();

                ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo, options);

                state.Current.EndConstructorParameter();

                bool finished = true;
                for (int i = 0; i < ParameterCount; i++)
                {
                    if (!state.Current.ConstructorArgumentState![i])
                    {
                        finished = false;
                        break;
                    }
                }

                if (finished)
                {
                    if (!foundProperty)
                    {
                        // This means that all the constructor arguments were at the start of the JSON.
                        state.Current.FirstPropertyIndex = state.Current.ConstructorParameterIndex;
                    }

                    // Position reader to the next property name or EndObject token.
                    reader.Read();

                    PositionReader(ref state, ref reader, foundProperty, ref resumptionState, resumptionByteIndex);
                    return;
                }
            }

            static void PositionReader(
                ref ReadStack state,
                ref Utf8JsonReader reader,
                bool foundMember,
                ref JsonReaderState resumptionState,
                long resumptionByteIndex)
            {
                if (foundMember)
                {
                    Debug.Assert(resumptionByteIndex != -1);

                    // Reposition to the first JSON property that didn't match a constructor argument.
                    reader = new Utf8JsonReader(
                        reader.OriginalSpan.Slice(checked((int)resumptionByteIndex)),
                        //reader.OriginalSpan,
                        isFinalBlock: reader.IsFinalBlock,
                        state: resumptionState);
                    state.BytesConsumed = resumptionByteIndex;

                    if (reader.OriginalSpan.Length > 0)
                    {
                        // Read to the next property name or end object token.
                        reader.Read();
                    }
                }
            }
        }

        protected abstract void ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReadPropertiesAndPopulateMembers(object obj, ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

            // We should be resuming from the first object property in the payload.
            int index = state.Current.FirstPropertyIndex;
            Debug.Assert(!state.Current.JsonPropertyKindIndicator![index]);

            bool[] indicator = state.Current.JsonPropertyKindIndicator!;

            while (true)
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(base.TypeToConvert);
                }

                bool isConstructorArg = index < indicator.Length ? indicator[index++] : false;

                if (isConstructorArg)
                {
                    reader.Skip();
                }
                else
                {
                    ReadOnlySpan<byte> unescapedPropertyName = GetUnescapedPropertyName(ref reader);

                    if (!TryGetPropertyFromFastCache(ref state.Current, unescapedPropertyName, out JsonPropertyInfo? jsonPropertyInfo))
                    {
                        // This should be rare as we would normally cache this on the first pass.
                        string unescapedPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
                        jsonPropertyInfo = GetProperty(ref state.Current, unescapedPropertyName, unescapedPropertyNameAsString, options);
                    }

                    Debug.Assert(jsonPropertyInfo != null);

                    bool useExtensionProperty;
                    // Determine if we should use the extension property.
                    if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
                    {
                        if (_dataExtensionProperty != null)
                        {
                            state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
                            jsonPropertyInfo = _dataExtensionProperty;
                            state.Current.JsonPropertyInfo = jsonPropertyInfo;
                            useExtensionProperty = true;
                            JsonSerializer.CreateDataExtensionProperty(obj, _dataExtensionProperty);
                        }
                        else
                        {
                            state.Current.EndProperty();
                            reader.Skip();
                            reader.Read();
                            continue;
                        }
                    }
                    else
                    {
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

                    // Skip the property if not found.
                    if (!jsonPropertyInfo.ShouldDeserialize)
                    {
                        state.Current.EndProperty();
                        reader.Skip();
                        reader.Read();
                        continue;
                    }

                    // Set the property value.
                    reader.Read();

                    if (!useExtensionProperty)
                    {
                        jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref reader);
                    }
                    else
                    {
                        jsonPropertyInfo.ReadJsonAndAddExtensionProperty(obj, ref state, ref reader);
                    }

                    // Ensure any exception thrown in the next read does not have a property in its JsonPath.
                    state.Current.EndProperty();
                }

                reader.Read();
            }
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, TypeToConvert value, JsonSerializerOptions options, ref WriteStack state)
        {
            // Minimize boxing for structs by only boxing once here
            object? objectValue = value;

            if (!state.SupportContinuation)
            {
                if (objectValue == null)
                {
                    writer.WriteNullValue();
                    return true;
                }

                writer.WriteStartObject();

                if (options.ReferenceHandling.ShouldWritePreservedReferences())
                {
                    if (JsonSerializer.WriteReferenceForObject(this, objectValue, ref state, writer) == MetadataPropertyName.Ref)
                    {
                        return true;
                    }
                }

                int propertyCount;
                if (_propertyCacheArray != null)
                {
                    propertyCount = _propertyCacheArray.Length;
                }
                else
                {
                    propertyCount = 0;
                }

                for (int i = 0; i < propertyCount; i++)
                {
                    JsonPropertyInfo jsonPropertyInfo = _propertyCacheArray![i];

                    // Remember the current property for JsonPath support if an exception is thrown.
                    state.Current.DeclaredJsonPropertyInfo = jsonPropertyInfo;

                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        if (jsonPropertyInfo == _dataExtensionProperty)
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJsonExtensionData(objectValue, ref state, writer))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJson(objectValue, ref state, writer))
                            {
                                Debug.Assert(jsonPropertyInfo.ConverterBase.ClassType != ClassType.Value);
                                return false;
                            }
                        }
                    }

                    state.Current.EndProperty();
                }

                writer.WriteEndObject();
                return true;
            }
            else
            {
                if (!state.Current.ProcessedStartToken)
                {
                    if (objectValue == null)
                    {
                        writer.WriteNullValue();
                        return true;
                    }

                    writer.WriteStartObject();

                    if (options.ReferenceHandling.ShouldWritePreservedReferences())
                    {
                        if (JsonSerializer.WriteReferenceForObject(this, objectValue, ref state, writer) == MetadataPropertyName.Ref)
                        {
                            return true;
                        }
                    }

                    state.Current.ProcessedStartToken = true;
                }

                int propertyCount;
                if (_propertyCacheArray != null)
                {
                    propertyCount = _propertyCacheArray.Length;
                }
                else
                {
                    propertyCount = 0;
                }

                while (propertyCount > state.Current.EnumeratorIndex)
                {
                    JsonPropertyInfo jsonPropertyInfo = _propertyCacheArray![state.Current.EnumeratorIndex];
                    state.Current.DeclaredJsonPropertyInfo = jsonPropertyInfo;

                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        if (jsonPropertyInfo == _dataExtensionProperty)
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJsonExtensionData(objectValue!, ref state, writer))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJson(objectValue!, ref state, writer))
                            {
                                Debug.Assert(jsonPropertyInfo.ConverterBase.ClassType != ClassType.Value);
                                return false;
                            }
                        }
                    }

                    state.Current.EndProperty();
                    state.Current.EnumeratorIndex++;

                    if (ShouldFlush(writer, ref state))
                    {
                        return false;
                    }
                }

                if (!state.Current.ProcessedEndToken)
                {
                    state.Current.ProcessedEndToken = true;
                    writer.WriteEndObject();
                }

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> GetUnescapedPropertyName(ref Utf8JsonReader reader)
        {
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

            return unescapedPropertyName;
        }

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        // AggressiveInlining used although a large method it is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryLookupConstructorParameterFromFastCache(
            ref ReadStack state,
            ReadOnlySpan<byte> unescapedPropertyName,
            out JsonParameterInfo? jsonParameterInfo)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            if (TryGetParameterFromFastCache(unescapedPropertyName, ref state.Current, out jsonParameterInfo))
            {
                Debug.Assert(jsonParameterInfo != null);
                HandleParameterName(ref state.Current, unescapedPropertyName, jsonParameterInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryLookupConstructorParameterFromSlowCache(
            ref ReadStack state,
            ReadOnlySpan<byte> unescapedPropertyName,
            out string unescapedPropertyNameAsString,
            out JsonParameterInfo? jsonParameterInfo)
        {
            if (TryGetParameterFromSlowCache(
                ref state.Current,
                unescapedPropertyName,
                out unescapedPropertyNameAsString,
                out jsonParameterInfo))
            {
                Debug.Assert(jsonParameterInfo != null);
                HandleParameterName(ref state.Current, unescapedPropertyName, jsonParameterInfo);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        public void HandleParameterName(ref ReadStackFrame frame, ReadOnlySpan<byte> unescapedPropertyName, JsonParameterInfo jsonParameterInfo)
        {
            Debug.Assert(jsonParameterInfo != null);

            int propertyPosition = frame.ConstructorParameterIndex + frame.PropertyIndex;

            Debug.Assert(frame.JsonPropertyKindIndicator != null);

            // Case where we can't fit the kind of all the JSON properties in the rented pool, we have to grow.
            if (propertyPosition == frame.JsonPropertyKindIndicator.Length)
            {
                bool[] indicator = frame.JsonPropertyKindIndicator;

                bool[] newIndicator = ArrayPool<bool>.Shared.Rent(indicator.Length * 2);
                indicator.CopyTo(newIndicator, 0);

                ArrayPool<bool>.Shared.Return(indicator, clearArray: true);
                frame.JsonPropertyKindIndicator = newIndicator;
            }

            // Indicate that the property name at this position points to a constructor argument.
            Debug.Assert(propertyPosition < frame.JsonPropertyKindIndicator.Length);
            frame.JsonPropertyKindIndicator[propertyPosition] = true;

            // Increment ConstructorParameterIndex so GetProperty() starts with the next parameter the next time this function is called.
            frame.ConstructorParameterIndex++;

            // Support JsonException.Path.
            Debug.Assert(
                jsonParameterInfo.JsonPropertyName == null ||
                // TODO: options.ConstructorParameterNameCaseInsensitive
                //options.PropertyNameCaseInsensitive ||
                unescapedPropertyName.SequenceEqual(jsonParameterInfo.JsonPropertyName));

            frame.JsonConstructorParameterInfo = jsonParameterInfo;

            if (jsonParameterInfo.JsonPropertyName == null)
            {
                byte[] propertyNameArray = unescapedPropertyName.ToArray();
                //if (options.ConstructorParameterNameCaseInsensitive)
                //{
                //    // Each payload can have a different name here; remember the value on the temporary stack.
                //    frame.JsonPropertyName = propertyNameArray;
                //}
                //else
                //{
                    // Prevent future allocs by caching globally on the JsonPropertyInfo which is specific to a Type+PropertyName
                    // so it will match the incoming payload except when case insensitivity is enabled (which is handled above).
                    frame.JsonConstructorParameterInfo.JsonPropertyName = propertyNameArray;
                //}
            }

            frame.JsonConstructorParameterInfo = jsonParameterInfo;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetParameterFromFastCache(
            ReadOnlySpan<byte> unescapedPropertyName,
            ref ReadStackFrame frame,
            out JsonParameterInfo? jsonParameterInfo)
        {
            ulong unescapedPropertyNameKey = JsonClassInfo.GetKey(unescapedPropertyName);

            JsonParameterInfo? info = null;

            // Keep a local copy of the cache in case it changes by another thread.
            ParameterRef[]? localParameterRefsSorted = _parameterRefsSorted;

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
                        if (TryIsParameterRefEqual(parameterRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                        {
                            jsonParameterInfo = info;
                            return true;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            parameterRef = localParameterRefsSorted[iBackward];
                            if (TryIsParameterRefEqual(parameterRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                            {
                                jsonParameterInfo = info;
                                return true;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        ParameterRef parameterRef = localParameterRefsSorted[iBackward];
                        if (TryIsParameterRefEqual(parameterRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                        {
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

            jsonParameterInfo = null;
            return false;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetParameterFromSlowCache(
            ref ReadStackFrame frame,
            ReadOnlySpan<byte> unescapedPropertyName,
            out string unescapedPropertyNameAsString,
            out JsonParameterInfo? jsonParameterInfo)
        {
            unescapedPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);

            Debug.Assert(ParameterCache != null);

            if (!ParameterCache.TryGetValue(unescapedPropertyNameAsString, out JsonParameterInfo? info))
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
            // TODO: recheck these conditions
            Debug.Assert(JsonClassInfo.GetKey(unescapedPropertyName) == info.ParameterNameKey ||
                unescapedPropertyNameAsString.Equals(info.NameAsString, StringComparison.OrdinalIgnoreCase));

            // Keep a local copy of the cache in case it changes by another thread.
            ParameterRef[]? localParameterRefsSorted = _parameterRefsSorted;

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

                    ParameterRef parameterRef = new ParameterRef(JsonClassInfo.GetKey(unescapedPropertyName), jsonParameterInfo);
                    frame.ParameterRefCache.Add(parameterRef);
                }
            }

            return true;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonPropertyInfo GetProperty(
            ref ReadStackFrame frame,
            ReadOnlySpan<byte> unescapedPropertyName,
            string unescapedPropertyNameAsString,
            JsonSerializerOptions options)
        {
            if (TryGetPropertyFromFastCache(ref frame, unescapedPropertyName, out JsonPropertyInfo? info))
            {
                Debug.Assert(info != null);
                return info;
            }

            return GetPropertyFromSlowCache(
                ref frame,
                unescapedPropertyName,
                unescapedPropertyNameAsString,
                options);
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetPropertyFromFastCache(
            ref ReadStackFrame frame,
            ReadOnlySpan<byte> unescapedPropertyName,
            out JsonPropertyInfo? jsonPropertyInfo)
        {
            ulong unescapedPropertyNameKey = JsonClassInfo.GetKey(unescapedPropertyName);

            JsonPropertyInfo? info = null;

            // Keep a local copy of the cache in case it changes by another thread.
            PropertyRef[]? localPropertyRefsSorted = _propertyRefsSorted;

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
                        if (TryIsPropertyRefEqual(propertyRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                        {
                            jsonPropertyInfo = info;
                            return true;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            propertyRef = localPropertyRefsSorted[iBackward];
                            if (TryIsPropertyRefEqual(propertyRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                            {
                                jsonPropertyInfo = info;
                                return true;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        PropertyRef propertyRef = localPropertyRefsSorted[iBackward];
                        if (TryIsPropertyRefEqual(propertyRef, unescapedPropertyName, unescapedPropertyNameKey, ref info))
                        {
                            jsonPropertyInfo = info;
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

            jsonPropertyInfo = null;
            return false;
        }

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonPropertyInfo GetPropertyFromSlowCache(
            ref ReadStackFrame frame,
            ReadOnlySpan<byte> unescapedPropertyName,
            string unescapedPropertyNameAsString,
            JsonSerializerOptions options)
        {
            // Keep a local copy of the cache in case it changes by another thread.
            PropertyRef[]? localPropertyRefsSorted = _propertyRefsSorted;

            Debug.Assert(_propertyCache != null);

            if (!_propertyCache.TryGetValue(unescapedPropertyNameAsString, out JsonPropertyInfo? info))
            {
                info = JsonPropertyInfo.s_missingProperty;
            }

            Debug.Assert(info != null);

            // Three code paths to get here:
            // 1) info == s_missingProperty. Property not found.
            // 2) key == info.PropertyNameKey. Exact match found.
            // 3) key != info.PropertyNameKey. Match found due to case insensitivity.
            Debug.Assert(
                info == JsonPropertyInfo.s_missingProperty ||
                JsonClassInfo.GetKey(unescapedPropertyName) == info.PropertyNameKey ||
                options.PropertyNameCaseInsensitive);

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

                    PropertyRef propertyRef = new PropertyRef(JsonClassInfo.GetKey(unescapedPropertyName), info);
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
                if (parameterName.Length <= JsonPropertyNameKeyLength ||
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
                if (propertyName.Length <= JsonPropertyNameKeyLength ||
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

            frame.ParameterRefCache = null;
        }

        protected void UpdateSortedPropertyCache(ref ReadStackFrame frame)
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
