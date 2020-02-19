// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal sealed partial class JsonObjectWithParameterizedConstructorConverter<T> : JsonObjectConverter<T>
    {
        // Parameter info
        private readonly ParameterInfo[] _parameters;
        private readonly int _parameterCount;
        private Dictionary<string, JsonParameterInfo>? _parameterCache;
        private readonly JsonClassInfo.ParameterizedConstructorDelegate<T> _createObject;

        // All of the serializable properties on a POCO (except the optional extension property) keyed on property name.
        public volatile Dictionary<string, JsonPropertyInfo>? _propertyCache;

        // All of the serializable properties on a POCO including the optional extension property.
        // Used for performance during serialization instead of 'PropertyCache' above.
        public volatile JsonPropertyInfo[]? _propertyCacheArray;

        public JsonPropertyInfo? _dataExtensionProperty;

        public JsonObjectWithParameterizedConstructorConverter(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            _parameters = constructor.GetParameters();
            _parameterCount = _parameters.Length;

            Debug.Assert(constructor != null);
            _createObject = options.MemberAccessorStrategy.CreateParameterizedConstructor<T>(constructor)!;
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T value)
        {
            bool shouldReadPreservedReferences = options.ReferenceHandling.ShouldReadPreservedReferences();

            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            object? obj;

            Dictionary<string, JsonParameterInfo> parameterCache = GetParameterCache(options);
            CreatePropertyCaches(parameterCache);

            if (!state.SupportContinuation && !shouldReadPreservedReferences)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    // This includes `null` tokens for structs as they can't be `null`.
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                if (_createObject == null)
                {
                    // TODO: New helper saying number of arguments is more than the threshold.
                    throw new NotSupportedException();
                }

                state.Current.InitializeObjectWithParameterizedConstructor(ref state.Current, parameterCache, _parameterCount);

                // Read all properties until we've parsed all constructor arguments or hit the end token.
                ReadAllConstructorArguments();

                // Construct object with arguments.
                Debug.Assert(state.Current.ConstructorArguments != null);
                obj = _createObject!(state.Current.ConstructorArguments)!;

                // Apply extension data.
                JsonPropertyInfo? extensionDataProperty = state.Current.JsonClassInfo.DataExtensionProperty;
                if (extensionDataProperty != null)
                {
                    JsonSerializer.CreateDataExtensionProperty(obj, extensionDataProperty);

                    if (state.Current.ExtensionDataIsObject)
                    {
                        Debug.Assert(state.Current.ObjectExtensionData != null);
                        extensionDataProperty.SetValueAsObject(obj, state.Current.ObjectExtensionData);
                    }
                    else
                    {
                        Debug.Assert(state.Current.JsonElementExtensionData != null);
                        extensionDataProperty.SetValueAsObject(obj, state.Current.JsonElementExtensionData);
                    }
                }

                // Read the rest of the payload and populate object.
                ReadPropertiesAndPopulateObject(obj, ref reader, options, ref state);
            }
            else
            {
                //if (state.Current.ObjectState < StackFrameObjectState.StartToken)
                //{
                //    if (reader.TokenType != JsonTokenType.StartObject)
                //    {
                //        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                //    }

                //    state.Current.ObjectState = StackFrameObjectState.StartToken;
                //}

                //// Handle the metadata properties.
                //if (state.Current.ObjectState < StackFrameObjectState.MetadataPropertyValue)
                //{
                //    if (shouldReadPreservedReferences)
                //    {
                //        if (this.ResolveMetadata(ref reader, ref state, out value))
                //        {
                //            if (state.Current.ObjectState == StackFrameObjectState.MetadataRefPropertyEndObject)
                //            {
                //                return true;
                //            }
                //        }
                //        else
                //        {
                //            return false;
                //        }
                //    }

                //    state.Current.ObjectState = StackFrameObjectState.MetadataPropertyValue;
                //}

                //if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                //{
                //    if (classInfo.CreateObject == null)
                //    {
                //        ThrowHelper.ThrowNotSupportedException_DeserializeNoParameterlessConstructor(classInfo.Type);
                //    }

                //    obj = classInfo.CreateObject!()!;
                //    if (state.Current.MetadataId != null)
                //    {
                //        if (!state.ReferenceResolver.AddReferenceOnDeserialize(state.Current.MetadataId, obj))
                //        {
                //            // Set so JsonPath throws exception with $id in it.
                //            state.Current.JsonPropertyName = JsonSerializer.s_metadataId.EncodedUtf8Bytes.ToArray();

                //            ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(state.Current.MetadataId);
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

                //// Read all properties.
                //while (true)
                //{
                //    // Determine the property.
                //    if (state.Current.PropertyState < StackFramePropertyState.ReadName)
                //    {
                //        state.Current.PropertyState = StackFramePropertyState.ReadName;

                //        if (!reader.Read())
                //        {
                //            // The read-ahead functionality will do the Read().
                //            state.Current.ReturnValue = obj;
                //            value = default!;
                //            return false;
                //        }
                //    }

                //    JsonPropertyInfo jsonPropertyInfo;

                //    if (state.Current.PropertyState < StackFramePropertyState.Name)
                //    {
                //        state.Current.PropertyState = StackFramePropertyState.Name;

                //        JsonTokenType tokenType = reader.TokenType;
                //        if (tokenType == JsonTokenType.EndObject)
                //        {
                //            // We are done reading properties.
                //            break;
                //        }
                //        else if (tokenType != JsonTokenType.PropertyName)
                //        {
                //            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                //        }

                //        JsonSerializer.LookupProperty(
                //            obj,
                //            ref reader,
                //            options,
                //            ref state,
                //            out jsonPropertyInfo,
                //            out bool useExtensionProperty);

                //        state.Current.UseExtensionProperty = useExtensionProperty;
                //    }
                //    else
                //    {
                //        jsonPropertyInfo = state.Current.JsonPropertyInfo;
                //    }

                //    if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                //    {
                //        if (!jsonPropertyInfo.ShouldDeserialize)
                //        {
                //            if (!reader.TrySkip())
                //            {
                //                state.Current.ReturnValue = obj;
                //                value = default!;
                //                return false;
                //            }

                //            state.Current.EndProperty();
                //            continue;
                //        }

                //        // Returning false below will cause the read-ahead functionality to finish the read.
                //        state.Current.PropertyState = StackFramePropertyState.ReadValue;

                //        if (!state.Current.UseExtensionProperty)
                //        {
                //            if (!SingleValueReadWithReadAhead(jsonPropertyInfo.ConverterBase.ClassType, ref reader, ref state))
                //            {
                //                state.Current.ReturnValue = obj;
                //                value = default!;
                //                return false;
                //            }
                //        }
                //        else
                //        {
                //            // The actual converter is JsonElement, so force a read-ahead.
                //            if (!SingleValueReadWithReadAhead(ClassType.Value, ref reader, ref state))
                //            {
                //                state.Current.ReturnValue = obj;
                //                value = default!;
                //                return false;
                //            }
                //        }
                //    }

                //    if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                //    {
                //        // Obtain the CLR value from the JSON and set the member.
                //        if (!state.Current.UseExtensionProperty)
                //        {
                //            if (!jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref reader))
                //            {
                //                state.Current.ReturnValue = obj;
                //                value = default!;
                //                return false;
                //            }
                //        }
                //        else
                //        {
                //            if (!jsonPropertyInfo.ReadJsonAndAddExtensionProperty(obj, ref state, ref reader))
                //            {
                //                // No need to set 'value' here since JsonElement must be read in full.
                //                state.Current.ReturnValue = obj;
                //                value = default!;
                //                return false;
                //            }
                //        }

                //        state.Current.EndProperty();
                //    }
                //}
            }

            // Apply properties read before we parsed all constructor arguments.
            Debug.Assert(state.Current.PropertyValues != null);
            foreach (KeyValuePair<JsonPropertyInfo, object?> pair in state.Current.PropertyValues)
            {
                JsonPropertyInfo jsonPropertyInfo = pair.Key;

                object? propertyValue = pair.Value;
                jsonPropertyInfo.SetValueAsObject(obj, propertyValue);
            }

            // Check if we are trying to build the sorted property cache.
            if (state.Current.ParameterRefCache != null)
            {
                UpdateSortedParameterCache(ref state.Current);
            }

            // Check if we are trying to build the sorted property cache.
            if (state.Current.PropertyRefCache != null)
            {
                classInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            value = (T)obj;

            return true;
        }

        private void ReadAllConstructorArguments(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
        {
            while (true)
            {
                // Read the property name or EndObject.
                reader.Read();

                JsonTokenType tokenType = reader.TokenType;
                if (tokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (tokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                if (TryLookupConstructorParameter(
                    ref reader,
                    ref state,
                    out JsonParameterInfo? jsonParameterInfo,
                    out ReadOnlySpan<byte> unescapedPropertyName,
                    out string? unescapedStringPropertyName,
                    options))
                {
                    // Set the property value.
                    reader.Read();

                    Debug.Assert(jsonParameterInfo != null);
                    jsonParameterInfo.ReadJson(ref state, ref reader, options, out object? argument);

                    Debug.Assert(state.Current.ConstructorArguments != null);
                    state.Current.ConstructorArguments[jsonParameterInfo.Position] = argument!;
                    state.Current.ConstructorArgumentState[jsonParameterInfo.Position] = true;

                    state.Current.EndConstructorParameter();

                    foreach (bool finished in state.Current.ConstructorArgumentState)
                    {

                    }
                }
                else
                {
                    Debug.Assert(unescapedPropertyName != default);
                    Debug.Assert(unescapedStringPropertyName != null);

                    LookupProperty(
                        ref state,
                        out JsonPropertyInfo jsonPropertyInfo,
                        out bool useExtensionProperty,
                        options,
                        unescapedPropertyName,
                        unescapedStringPropertyName);

                    // Skip the property if not found.
                    if (!jsonPropertyInfo.ShouldDeserialize)
                    {
                        reader.Skip();
                        state.Current.EndProperty();
                        continue;
                    }

                    // Set the property value.
                    reader.Read();

                    if (!useExtensionProperty)
                    {
                        jsonPropertyInfo.ReadJson(ref state, ref reader, out object? argument);

                        Debug.Assert(state.Current.PropertyValues != null);
                        state.Current.PropertyValues[jsonPropertyInfo] = argument;
                    }
                    else if (state.Current.ExtensionDataIsObject)
                    {
                        jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out object? extensionDataValue);

                        if (state.Current.ObjectExtensionData != null)
                        {
                            Debug.Assert(state.Current.JsonPropertyNameAsString != null);
                            state.Current.ObjectExtensionData[state.Current.JsonPropertyNameAsString] = extensionDataValue!;
                        }
                    }
                    else
                    {
                        jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out JsonElement extensionDataValue);

                        if (state.Current.JsonElementExtensionData != null)
                        {
                            Debug.Assert(state.Current.JsonPropertyNameAsString != null);
                            state.Current.JsonElementExtensionData[state.Current.JsonPropertyNameAsString] = extensionDataValue;
                        }
                    }

                    // Ensure any exception thrown in the next read does not have a property in its JsonPath.
                    state.Current.EndProperty();
                }
            }
        }

        private void ReadPropertiesAndPopulateObject(object obj, ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
        {
            while (true)
            {
                // Read the property name or EndObject.
                reader.Read();

                JsonTokenType tokenType = reader.TokenType;
                if (tokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (tokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                LookupProperty(
                    ref state,
                    out JsonPropertyInfo jsonPropertyInfo,
                    out bool useExtensionProperty,
                    options);

                // Skip the property if not found.
                if (!jsonPropertyInfo.ShouldDeserialize)
                {
                    reader.Skip();
                    state.Current.EndProperty();
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
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
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

                JsonPropertyInfo? dataExtensionProperty = state.Current.JsonClassInfo.DataExtensionProperty;

                int propertyCount;
                JsonPropertyInfo[]? propertyCacheArray = state.Current.JsonClassInfo.PropertyCacheArray;
                if (propertyCacheArray != null)
                {
                    propertyCount = propertyCacheArray.Length;
                }
                else
                {
                    propertyCount = 0;
                }

                for (int i = 0; i < propertyCount; i++)
                {
                    JsonPropertyInfo jsonPropertyInfo = propertyCacheArray![i];

                    // Remember the current property for JsonPath support if an exception is thrown.
                    state.Current.DeclaredJsonPropertyInfo = jsonPropertyInfo;

                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        if (jsonPropertyInfo == dataExtensionProperty)
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

                JsonPropertyInfo? dataExtensionProperty = state.Current.JsonClassInfo.DataExtensionProperty;

                int propertyCount;
                JsonPropertyInfo[]? propertyCacheArray = state.Current.JsonClassInfo.PropertyCacheArray;
                if (propertyCacheArray != null)
                {
                    propertyCount = propertyCacheArray.Length;
                }
                else
                {
                    propertyCount = 0;
                }

                while (propertyCount > state.Current.EnumeratorIndex)
                {
                    JsonPropertyInfo jsonPropertyInfo = propertyCacheArray![state.Current.EnumeratorIndex];
                    state.Current.DeclaredJsonPropertyInfo = jsonPropertyInfo;

                    if (jsonPropertyInfo.ShouldSerialize)
                    {
                        if (jsonPropertyInfo == dataExtensionProperty)
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

        internal override bool ConstructorIsParameterized => true;

        private void CreatePropertyCaches(Dictionary<string, JsonParameterInfo> parameterCache, JsonSerializerOptions options)
        {
            if (_propertyCachesCreated)
            {
                return;
            }

            PropertyInfo[] properties = TypeToConvert.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            Dictionary<string, JsonPropertyInfo> propertyCache = CreatePropertyCache(PropertyCount);
            Dictionary<string, JsonPropertyInfo> parameterMatches = new Dictionary<string, JsonPropertyInfo>(_parameterCount);
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
                    JsonPropertyInfo jsonPropertyInfo = AddProperty(propertyInfo.PropertyType, propertyInfo, type, options);
                    Debug.Assert(jsonPropertyInfo != null && jsonPropertyInfo.NameAsString != null);

                    // If there is a case-insensitive match, this means that the property will probably be assigned through the constructor.
                    // We want to put this property at the front of the queue to be serialized, so we can get a quicker match on deserialization.
                    cacheToPopulate = parameterCache.ContainsKey(jsonPropertyInfo.NameAsString) ? parameterMatches : propertyCache;

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
                            ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(this, jsonPropertyInfo);
                        }
                        // else ignore jsonPropertyInfo since it has [JsonIgnore].
                    }
                }
            }

            JsonPropertyInfo[] cacheArray;

            JsonPropertyInfo? dataExtensionProperty;
            if (JsonClassInfo.TryDetermineExtensionDataProperty(propertyCache, out dataExtensionProperty))
            {
                Debug.Assert(dataExtensionProperty != null);
                _dataExtensionProperty = dataExtensionProperty;

                // Remove from propertyCache since it is handled independently.
                propertyCache.Remove(_dataExtensionProperty.NameAsString!);

                cacheArray = new JsonPropertyInfo[propertyCache.Count + parameterMatches.Count + 1];

                // Set the last element to the extension property.
                cacheArray[propertyCache.Count] = _dataExtensionProperty;
            }
            else if (JsonClassInfo.TryDetermineExtensionDataProperty(parameterCache, out dataExtensionProperty))
            {
                Debug.Assert(dataExtensionProperty != null);
                _dataExtensionProperty = dataExtensionProperty;

                // Remove from propertyCache since it is handled independently.
                parameterCache.Remove(_dataExtensionProperty.NameAsString!);

                cacheArray = new JsonPropertyInfo[propertyCache.Count + parameterMatches.Count + 1];

                // Set the last element to the extension property.
                cacheArray[propertyCache.Count] = _dataExtensionProperty;
            }
            else
            {
                cacheArray = new JsonPropertyInfo[propertyCache.Count + parameterMatches.Count];
            }

            parameterMatches.Values.CopyTo(cacheArray, 0);
            propertyCache.Values.CopyTo(cacheArray, parameterMatches.Count);
            
            foreach (KeyValuePair<string, JsonPropertyInfo> pair in parameterMatches)
            {
                propertyCache.Add(pair.Key, pair.Value);
            }

            _propertyCache = propertyCache;
            _propertyCacheArray = cacheArray;

            _propertyCachesCreated = true;
        }

        private Dictionary<string, JsonParameterInfo> GetParameterCache(JsonSerializerOptions options)
        {
            if (_parameterCache == null)
            {
                _parameterCache = new Dictionary<string, JsonParameterInfo>(_parameterCount, StringComparer.OrdinalIgnoreCase);

                foreach (ParameterInfo parameter in _parameters)
                {
                    JsonParameterInfo jsonParameterInfo = AddConstructorParameterProperty(parameter.ParameterType, parameter, parentType: null!, options);
                    if (!JsonHelpers.TryAdd(_parameterCache, jsonParameterInfo.NameAsString, jsonParameterInfo))
                    {
                        // TODO: Add test for this; use throw helper.
                        throw new InvalidOperationException();
                    }
                }
            }

            return _parameterCache;
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
                // TODO: throw more specific exception showing that the error is with a constructor parameter.
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(parameterType, parentType: null, memberInfo: null);
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
    }
}
