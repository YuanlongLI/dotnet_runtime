﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal abstract partial class ObjectWithParameterizedConstructorConverter<TypeToConvert> : JsonObjectConverter<TypeToConvert>
    {
        // All of the serializable properties on a POCO (except the optional extension property) keyed on property name.
        protected volatile Dictionary<string, JsonPropertyInfo> _propertyCache = null!;

        // All of the serializable properties on a POCO including the optional extension property.
        // Used for performance during serialization instead of 'PropertyCache' above.
        private volatile JsonPropertyInfo[]? _propertyCacheArray;

        protected JsonPropertyInfo? DataExtensionProperty;

        // Whether the extension data is typeof(object) or typoef(JsonElement).
        private bool DataExtensionIsObject;

        protected volatile Dictionary<string, JsonParameterInfo> ParameterCache = null!;

        protected int ParameterCount { get; private set; }

        internal override void Initialize(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            // The parameter cache must be before the property caches.
            CreateParameterCache(constructor, options);

            CreatePropertyCaches(options);
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TypeToConvert value)
        {
            bool shouldReadPreservedReferences = options.ReferenceHandling.ShouldReadPreservedReferences();

            object? obj = null;

            if (!state.SupportContinuation && !shouldReadPreservedReferences)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    // This includes `null` tokens for structs as they can't be `null`.
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(base.TypeToConvert);
                }

                // Set state.Current.JsonPropertyInfo to null so there's no conflict on state.Push()
                state.Current.JsonPropertyInfo = null!;

                InitializeConstructorArgumentCaches(ref state.Current, options);

                ReadOnlySpan<byte> originalSpan = reader.OriginalSpan;

                // Read until we've parsed all constructor arguments or hit the end token.
                ReadConstructorArguments(ref reader, options, ref state);

                obj = CreateObject(ref state);

                if (state.Current.FoundPropertyCount > 0)
                {
                    Utf8JsonReader tempReader;
                    // Set the properties we've parsed so far.
                    for (int i = 0; i < state.Current.FoundPropertyCount; i++)
                    {
                        JsonPropertyInfo jsonPropertyInfo = state.Current.FoundProperties![i].Item1;
                        long resumptionByteIndex = state.Current.FoundProperties[i].Item3;
                        byte[]? propertyNameArray = state.Current.FoundProperties[i].Item4;

                        tempReader = new Utf8JsonReader(
                            originalSpan.Slice(checked((int)resumptionByteIndex)),
                            isFinalBlock: true,
                            state: state.Current.FoundProperties[i].Item2);

                        Debug.Assert(tempReader.TokenType == JsonTokenType.PropertyName);
                        tempReader.Read();

                        if (propertyNameArray == null)
                        {
                            propertyNameArray = jsonPropertyInfo.JsonPropertyName;
                        }
                        else
                        {
                            Debug.Assert(options.PropertyNameCaseInsensitive);
                            state.Current.JsonPropertyName = propertyNameArray;
                        }

                        // Support JsonException.Path.
                        Debug.Assert(
                            jsonPropertyInfo.JsonPropertyName == null ||
                            options.PropertyNameCaseInsensitive ||
                            ((ReadOnlySpan<byte>)propertyNameArray!).SequenceEqual(jsonPropertyInfo.JsonPropertyName));

                        state.Current.JsonPropertyInfo = jsonPropertyInfo;

                        jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref tempReader);
                    }

                    ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, long, byte[]?>>.Shared.Return(
                        state.Current.FoundProperties!,
                        clearArray: true);
                }
#if DEBUG
                else
                {
                    Debug.Assert(state.Current.FoundProperties == null);
                }
#endif
            }
            else
            {
                // Slower path that supports continuation and preserved references.

                if (state.Current.ObjectState == StackFrameObjectState.None)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(base.TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.StartToken;

                    // Set state.Current.JsonPropertyInfo to null so there's no conflict on state.Push()
                    state.Current.JsonPropertyInfo = null!;

                    InitializeConstructorArgumentCaches(ref state.Current, options);
                }

                // Handle the metadata properties.
                if (state.Current.ObjectState < StackFrameObjectState.MetadataPropertyValue)
                {
                    if (shouldReadPreservedReferences)
                    {
                        if (!reader.Read())
                        {
                            value = default!;
                            return false;
                        }

                        ReadOnlySpan<byte> propertyName = reader.GetSpan();
                        MetadataPropertyName metadata = JsonSerializer.GetMetadataPropertyName(propertyName);

                        if (metadata == MetadataPropertyName.Id ||
                            metadata == MetadataPropertyName.Ref ||
                            metadata == MetadataPropertyName.Values)
                        {
                            throw new NotSupportedException();
                        }

                        // Skip the read of the first property name, since we already read it above.
                        state.Current.PropertyState = StackFramePropertyState.ReadName;
                    }

                    state.Current.ObjectState = StackFrameObjectState.MetadataPropertyValue;
                }

                //if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                //{
                //    if (state.Current.JsonClassInfo.CreateObject == null)
                //    {
                //        ThrowHelper.ThrowNotSupportedException_DeserializeNoParameterlessConstructor(state.Current.JsonClassInfo.Type);
                //    }

                //    obj = state.Current.JsonClassInfo.CreateObject!()!;
                //    if (state.Current.MetadataId != null)
                //    {
                //        if (!state.ReferenceResolver.AddReferenceOnDeserialize(state.Current.MetadataId, obj))
                //        {
                //            ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(state.Current.MetadataId, ref state);
                //        }
                //    }

                //    state.Current.ReturnValue = obj;
                //    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                //}
                //else
                //{
                //    obj = state.Current.ReturnValue!;
                //    Debug.Assert(obj != null);
                //}

                // Process all properties.
                while (true)
                {
                    // Determine the property.
                    if (state.Current.PropertyState == StackFramePropertyState.None)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadName;

                        if (!reader.Read())
                        {
                            value = default!;
                            return false;
                        }
                    }

                    JsonParameterInfo? jsonParameterInfo = null;
                    JsonPropertyInfo? jsonPropertyInfo = null;

                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        state.Current.PropertyState = StackFramePropertyState.Name;

                        JsonTokenType tokenType = reader.TokenType;
                        if (tokenType == JsonTokenType.EndObject)
                        {
                            // We are done with the first pass.
                            break;
                        }
                        else if (tokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(base.TypeToConvert);
                        }

                        ReadOnlySpan<byte> escapedPropertyName = reader.GetSpan();
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
                                ThrowHelper.ThrowUnexpectedMetadataException(escapedPropertyName, ref reader, ref state);
                            }
                        }

                        if (!TryLookupConstructorParameterFromFastCache(
                            ref state,
                            unescapedPropertyName,
                            out jsonParameterInfo))
                        {
                            if (TryGetPropertyFromFastCache(
                                ref state.Current,
                                unescapedPropertyName,
                                out jsonPropertyInfo))
                            {
                                Debug.Assert(jsonPropertyInfo != null);

                                // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                                state.Current.PropertyIndex++;

                                if (!TryDetermineExtensionData(ref state, ref reader, unescapedPropertyName, jsonPropertyInfo, options))
                                {
                                    value = default!;
                                    return false;
                                }
                            }
                            else if (!TryLookupConstructorParameterFromSlowCache(
                                ref state,
                                unescapedPropertyName,
                                out string unescapedPropertyNameAsString,
                                out jsonParameterInfo))
                            {
                                jsonPropertyInfo = GetPropertyFromSlowCache(
                                    ref state.Current,
                                    unescapedPropertyName,
                                    unescapedPropertyNameAsString,
                                    options);

                                Debug.Assert(jsonPropertyInfo != null);

                                // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                                state.Current.PropertyIndex++;

                                if (!TryDetermineExtensionData(ref state, ref reader, unescapedPropertyName, jsonPropertyInfo, options))
                                {
                                    value = default!;
                                    return false;
                                }
                            }
                        }
                    }
                    else if (state.Current.JsonConstructorParameterInfo != null)
                    {
                        jsonParameterInfo = state.Current.JsonConstructorParameterInfo;
                    }
                    else
                    {
                        Debug.Assert(state.Current.JsonPropertyInfo != null);
                        jsonPropertyInfo = state.Current.JsonPropertyInfo!;
                    }

                    // Handle either constructor arg or property.
                    if (jsonParameterInfo != null)
                    {
                        if (!TryHandleConstructorArgument(ref state, ref reader, jsonParameterInfo, options))
                        {
                            value = default!;
                            return false;
                        }
                    }
                    else
                    {
                        if (!TryHandlePropertyValue(ref state, ref reader, jsonPropertyInfo!))
                        {
                            value = default!;
                            return false;
                        }
                    }
                }

                obj = CreateObject(ref state);

                if (state.Current.FoundPropertyCount > 0)
                {
                    Utf8JsonReader tempReader;
                    // Set the properties we've parsed so far.
                    for (int i = 0; i < state.Current.FoundPropertyCount; i++)
                    {
                        JsonPropertyInfo jsonPropertyInfo = state.Current.FoundPropertiesAsync![i].Item1;
                        byte[] propertyValueArray = state.Current.FoundPropertiesAsync[i].Item3;
                        byte[]? propertyNameArray = state.Current.FoundPropertiesAsync[i].Item4;

                        tempReader = new Utf8JsonReader(
                            propertyValueArray,
                            isFinalBlock: true,
                            state: state.Current.FoundPropertiesAsync[i].Item2);

                        Debug.Assert(tempReader.TokenType == JsonTokenType.PropertyName);
                        tempReader.Read();

                        if (propertyNameArray == null)
                        {
                            propertyNameArray = jsonPropertyInfo.JsonPropertyName;
                        }
                        else
                        {
                            Debug.Assert(options.PropertyNameCaseInsensitive);
                            state.Current.JsonPropertyName = propertyNameArray;
                        }

                        // Support JsonException.Path.
                        Debug.Assert(
                            jsonPropertyInfo.JsonPropertyName == null ||
                            options.PropertyNameCaseInsensitive ||
                            ((ReadOnlySpan<byte>)propertyNameArray!).SequenceEqual(jsonPropertyInfo.JsonPropertyName));

                        state.Current.JsonPropertyInfo = jsonPropertyInfo;

                        jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref tempReader);
                    }

                    ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, byte[]?, byte[]?>>.Shared.Return(
                        state.Current.FoundPropertiesAsync!,
                        clearArray: true);
                }
#if DEBUG
                else
                {
                    Debug.Assert(state.Current.FoundProperties == null);
                }
#endif
            }

            // Set extension data, if any.
            if (state.Current.DataExtension != null)
            {
                DataExtensionProperty!.SetValueAsObject(obj, state.Current.DataExtension);
            }

            // Check if we are trying to build the sorted parameter cache.
            if (state.Current.ParameterRefCache != null)
            {
                UpdateSortedParameterCache(ref state.Current);
            }

            // Check if we are trying to build the sorted property cache.
            if (state.Current.PropertyRefCache != null)
            {
                UpdateSortedPropertyCache(ref state.Current);
            }

            Debug.Assert(obj != null);
            value = (TypeToConvert)obj;

            return true;
        }

        private bool TryHandleConstructorArgument(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonParameterInfo jsonParameterInfo,
            JsonSerializerOptions options)
        {
            if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
            {
                // Returning false below will cause the read-ahead functionality to finish the read.
                state.Current.PropertyState = StackFramePropertyState.ReadValue;

                if (!SingleValueReadWithReadAhead(jsonParameterInfo.ConverterBase.ClassType, ref reader, ref state))
                {
                    return false;
                }
            }

            if (state.Current.PropertyState < StackFramePropertyState.TryRead)
            {
                if (!ReadAndCacheConstructorArgument(ref state, ref reader, jsonParameterInfo, options))
                {
                    return false;
                }
            }

            state.Current.EndConstructorParameter();
            return true;
        }

        private bool TryDetermineExtensionData(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            ReadOnlySpan<byte> unescapedPropertyName,
            JsonPropertyInfo jsonPropertyInfo,
            JsonSerializerOptions options)
        {
            bool useExtensionProperty;
            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                if (DataExtensionProperty != null)
                {
                    if (state.Current.DataExtension == null)
                    {
                        if (DataExtensionProperty.RuntimeClassInfo.CreateObject == null)
                        {
                            // TODO: better exception here.
                            throw new NotSupportedException();
                        }

                        state.Current.DataExtension = DataExtensionProperty.RuntimeClassInfo.CreateObject();
                    }

                    state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
                    jsonPropertyInfo = DataExtensionProperty;
                    //state.Current.JsonPropertyInfo = jsonPropertyInfo;
                    useExtensionProperty = true;
                }
                else
                {
                    if (!reader.TrySkip())
                    {
                        return false;
                    }

                    state.Current.EndProperty();
                    return true;
                }
            }
            else
            {
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
                        jsonPropertyInfo.JsonPropertyName = propertyNameArray;
                    }
                }

                useExtensionProperty = false;
            }

            state.Current.JsonPropertyInfo = jsonPropertyInfo;

            state.Current.UseExtensionProperty = useExtensionProperty;
            return true;
        }

        private bool TryHandlePropertyValue(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonPropertyInfo jsonPropertyInfo)
        {
            if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
            {
                if (!state.Current.UseExtensionProperty)
                {
                    if (!jsonPropertyInfo.ShouldDeserialize)
                    {
                        if (!reader.TrySkip())
                        {
                            return false;
                        }

                        state.Current.EndProperty();
                        return true;
                    }

                    state.Current.PropertyState = StackFramePropertyState.ReadValue;

                    if (state.Current.FoundPropertiesAsync == null)
                    {
                        state.Current.FoundPropertiesAsync =
                            ArrayPool<ValueTuple<
                                JsonPropertyInfo,
                                JsonReaderState,
                                byte[],
                                byte[]?>>.Shared.Rent(PropertyNameCountCacheThreshold);
                    }
                    else if (state.Current.FoundPropertyCount == state.Current.FoundPropertiesAsync!.Length)
                    {
                        // Case where we can't fit all the JSON properties in the rented pool, we have to grow.
                        var newCache = ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, byte[], byte[]?>>.Shared.Rent(
                            state.Current.FoundPropertiesAsync!.Length * 2);
                        state.Current.FoundPropertiesAsync!.CopyTo(newCache, 0);

                        ArrayPool<ValueTuple<JsonPropertyInfo, JsonReaderState, byte[], byte[]?>>.Shared.Return(
                            state.Current.FoundPropertiesAsync!, clearArray: true);
                        state.Current.FoundPropertiesAsync = newCache!;
                    }

                    Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                    state.Current.ReaderState = reader.CurrentState;

                    // We want to cache all the bytes needed to create the value, so force a read-ahead.
                    if (!SingleValueReadWithReadAhead(ClassType.Value, ref reader, ref state))
                    {
                        return false;
                    }
                }
                else
                {
                    state.Current.PropertyState = StackFramePropertyState.ReadValue;

                    // The actual converter is JsonElement, so force a read-ahead.
                    if (!SingleValueReadWithReadAhead(ClassType.Value, ref reader, ref state))
                    {
                        return false;
                    }
                }
            }

            if (!state.Current.UseExtensionProperty)
            {
                if (state.Current.PropertyState < StackFramePropertyState.ReadValueIsEnd)
                {
                    state.Current.PropertyState = StackFramePropertyState.ReadValueIsEnd;

                    byte[]? propertyNameArray;

                    if (jsonPropertyInfo.JsonPropertyName == null)
                    {
                        Debug.Assert(state.Current.JsonPropertyName != null);
                        propertyNameArray = state.Current.JsonPropertyName;
                    }
                    else
                    {
                        propertyNameArray = null;
                    }

                    state.Current.FoundPropertiesAsync![state.Current.FoundPropertyCount++] = (
                            jsonPropertyInfo,
                            state.Current.ReaderState,
                            reader.OriginalSpan.Slice(checked((int)reader.TokenStartIndex)).ToArray(),
                            propertyNameArray);


                    if (!reader.TrySkip())
                    {
                        return false;
                    }
                }

                if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                {
                    state.Current.EndProperty();
                    return true;
                }
            }

            if (state.Current.PropertyState < StackFramePropertyState.TryRead)
            {
                // Obtain the CLR value from the JSON and set the member.
                if (state.Current.UseExtensionProperty)
                {
                    if (DataExtensionIsObject)
                    {
                        if (!jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out object? extDataValue))
                        {
                            return false;
                        }
                        ((IDictionary<string, object>)state.Current.DataExtension!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                    }
                    else
                    {
                        if (!jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out JsonElement extDataValue))
                        {
                            return false;
                        }
                        ((IDictionary<string, JsonElement>)state.Current.DataExtension!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                    }
                }
            }

            state.Current.EndProperty();
            return true;
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
                        if (jsonPropertyInfo == DataExtensionProperty)
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
                        if (jsonPropertyInfo == DataExtensionProperty)
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

        protected abstract object CreateObject(ref ReadStack state);

        private void CreateParameterCache(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            ParameterInfo[] parameters = constructor.GetParameters();

            ParameterCount = parameters.Length;

            // TODO: honor options.ParameterCaseInsensitvity
            var cache = new Dictionary<string, JsonParameterInfo>(ParameterCount);
            foreach (ParameterInfo parameter in parameters)
            {
                JsonParameterInfo jsonParameterInfo = AddConstructorParameterProperty(parameter.ParameterType, parameter, base.TypeToConvert, options);
                if (!JsonHelpers.TryAdd(cache, jsonParameterInfo.NameAsString, jsonParameterInfo))
                {
                    // TODO: Add test for this; use throw helper. This could be many-to-one naming policy.
                    throw new InvalidOperationException();
                }
            }

            // Set field when finished to avoid concurrency issues.
            ParameterCache = cache;
        }

        private JsonParameterInfo AddConstructorParameterProperty(Type parameterType, ParameterInfo parameterInfo, Type parentType, JsonSerializerOptions options)
        {
            ClassType classType = JsonClassInfo.GetClassType(
                parameterType,
                parentType,
                propertyInfo: null,
                out Type? runtimeType,
                out Type? _,
                out JsonConverter? converter,
                options);

            if (converter == null)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(parameterType, parentType, parameterInfo);
            }

            return CreateConstructorParameter(
                declaredParameterType: parameterType,
                runtimeParameterType: runtimeType!,
                parameterInfo,
                parentType,
                converter,
                classType,
                options);
        }

        private static JsonParameterInfo CreateConstructorParameter(
            Type declaredParameterType,
            Type runtimeParameterType,
            ParameterInfo parameterInfo,
            Type parentType,
            JsonConverter converter,
            ClassType classType,
            JsonSerializerOptions options)
        {
            // Create the JsonParameterInfo instance.
            JsonParameterInfo jsonParameterInfo = converter.CreateJsonParameterInfo();

            jsonParameterInfo.Initialize(
                declaredParameterType,
                runtimeParameterType,
                parameterInfo,
                parentClassType: parentType,
                converter,
                classType,
                options);

            return jsonParameterInfo;
        }

        public Dictionary<string, JsonPropertyInfo> CreatePropertyCache(int capacity, JsonSerializerOptions options)
        {
            if (options.PropertyNameCaseInsensitive)
            {
                return new Dictionary<string, JsonPropertyInfo>(capacity, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                return new Dictionary<string, JsonPropertyInfo>(capacity);
            }
        }

        private void CreatePropertyCaches(JsonSerializerOptions options)
        {
            PropertyInfo[] properties = base.TypeToConvert.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            Dictionary<string, JsonPropertyInfo> propertyCache = CreatePropertyCache(properties.Length, options);
            Dictionary<string, JsonPropertyInfo> parameterMatches = new Dictionary<string, JsonPropertyInfo>(ParameterCount);
            Dictionary<string, JsonPropertyInfo> cacheToPopulate;

            foreach (PropertyInfo propertyInfo in properties)
            {
                // Ignore indexers
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                // For now we only support public getters\setters
                if (propertyInfo.GetMethod?.IsPublic == true ||
                    propertyInfo.SetMethod?.IsPublic == true)
                {
                    JsonPropertyInfo jsonPropertyInfo = JsonClassInfo.AddProperty(propertyInfo.PropertyType, propertyInfo, base.TypeToConvert, options);
                    Debug.Assert(jsonPropertyInfo != null && jsonPropertyInfo.NameAsString != null);

                    // If there is a case-insensitive match, this means that the property will probably be assigned through the constructor.
                    // We want to put this property at the front of the queue to be serialized, so we can get a quicker match on deserialization.
                    cacheToPopulate = ParameterCache.ContainsKey(jsonPropertyInfo.NameAsString) ? parameterMatches : propertyCache;

                    // If the JsonPropertyNameAttribute or naming policy results in collisions, throw an exception.
                    if (!JsonHelpers.TryAdd(cacheToPopulate, jsonPropertyInfo.NameAsString, jsonPropertyInfo))
                    {
                        JsonPropertyInfo other = cacheToPopulate[jsonPropertyInfo.NameAsString];

                        if (other.ShouldDeserialize == false && other.ShouldSerialize == false)
                        {
                            // Overwrite the one just added since it has [JsonIgnore].
                            cacheToPopulate[jsonPropertyInfo.NameAsString] = jsonPropertyInfo;
                        }
                        else if (jsonPropertyInfo.ShouldDeserialize == true || jsonPropertyInfo.ShouldSerialize == true)
                        {
                            ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(base.TypeToConvert, jsonPropertyInfo);
                        }
                        // else ignore jsonPropertyInfo since it has [JsonIgnore].
                    }
                }
            }

            JsonPropertyInfo[] cacheArray;

            JsonPropertyInfo? dataExtensionProperty;
            if (JsonClassInfo.TryDetermineExtensionDataProperty(base.TypeToConvert, propertyCache, options, out dataExtensionProperty))
            {
                Debug.Assert(dataExtensionProperty != null);
                DataExtensionProperty = dataExtensionProperty;

                // Remove from propertyCache since it is handled independently.
                propertyCache.Remove(DataExtensionProperty.NameAsString!);

                int propertyCount = propertyCache.Count + parameterMatches.Count;
                cacheArray = new JsonPropertyInfo[propertyCount + 1];

                // Set the last element to the extension property.
                cacheArray[propertyCount] = DataExtensionProperty;
            }
            else if (JsonClassInfo.TryDetermineExtensionDataProperty(base.TypeToConvert, parameterMatches, options, out dataExtensionProperty))
            {
                Debug.Assert(dataExtensionProperty != null);
                DataExtensionProperty = dataExtensionProperty;

                // Remove from parameterCache since it is handled independently.
                ParameterCache.Remove(DataExtensionProperty.NameAsString!);

                int propertyCount = propertyCache.Count + parameterMatches.Count;
                cacheArray = new JsonPropertyInfo[propertyCount + 1];

                // Set the last element to the extension property.
                cacheArray[propertyCount] = DataExtensionProperty;
            }
            else
            {
                cacheArray = new JsonPropertyInfo[propertyCache.Count + parameterMatches.Count];
            }

            if (DataExtensionProperty != null)
            {
                DataExtensionIsObject = typeof(IDictionary<string, object>).IsAssignableFrom(DataExtensionProperty.RuntimeClassInfo.Type);
            }

            parameterMatches.Values.CopyTo(cacheArray, 0);
            propertyCache.Values.CopyTo(cacheArray, parameterMatches.Count);

            foreach (KeyValuePair<string, JsonPropertyInfo> pair in parameterMatches)
            {
                propertyCache.Add(pair.Key, pair.Value);
            }

            _propertyCache = propertyCache;
            _propertyCacheArray = cacheArray;
        }

        internal override bool ConstructorIsParameterized => true;
    }
}
