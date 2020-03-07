// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal abstract partial class ObjectWithParameterizedConstructorConverter<TypeToConvert> : JsonObjectConverter<TypeToConvert>
    {
        // All of the serializable properties on a POCO (except the optional extension property) keyed on property name.
        protected volatile Dictionary<string, JsonPropertyInfo> PropertyCache = null!;

        // All of the serializable properties on a POCO including the optional extension property.
        // Used for performance during serialization instead of 'PropertyCache' above.
        private volatile JsonPropertyInfo[]? _propertyCacheArray;

        protected JsonPropertyInfo? DataExtensionProperty;

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

                InitializeConstructorArgumentCache(ref state.Current, options);

                // Read until we've parsed all constructor arguments or hit the end token.
                ReadConstructorArguments(ref reader, options, ref state);

                obj = CreateObject(ref state);

                // Check if we are trying to build the sorted property cache.
                if (state.Current.ParameterRefCache != null)
                {
                    UpdateSortedParameterCache(ref state.Current);
                }

                // Check if we are trying to build the sorted property cache.
                if (state.Current.PropertyRefCache != null)
                {
                    UpdateSortedPropertyCache(ref state.Current);
                }

                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                    // Read the rest of the payload and populate members.
                    ReadPropertiesAndPopulateMembers(obj, ref reader, options, ref state);

                    // Check if we are trying to build the sorted property cache.
                    if (state.Current.PropertyRefCache != null)
                    {
                        UpdateSortedPropertyCache(ref state.Current);
                    }
                }

                Debug.Assert(state.Current.JsonPropertyKindIndicator != null);
                ArrayPool<bool>.Shared.Return(state.Current.JsonPropertyKindIndicator, clearArray: true);
            }
            //else
            //{
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
            //}

            Debug.Assert(obj != null);
            value = (TypeToConvert)obj;

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

            parameterMatches.Values.CopyTo(cacheArray, 0);
            propertyCache.Values.CopyTo(cacheArray, parameterMatches.Count);

            foreach (KeyValuePair<string, JsonPropertyInfo> pair in parameterMatches)
            {
                propertyCache.Add(pair.Key, pair.Value);
            }

            PropertyCache = propertyCache;
            _propertyCacheArray = cacheArray;
        }

        internal override bool ConstructorIsParameterized => true;
    }
}
