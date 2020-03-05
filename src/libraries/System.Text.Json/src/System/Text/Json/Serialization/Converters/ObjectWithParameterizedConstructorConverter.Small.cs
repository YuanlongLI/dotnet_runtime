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
    internal sealed class SmallObjectWithParameterizedConstructorConverter<TypeToConvert, TArg0, TArg1, TArg2, TArg3> :
        ObjectWithParameterizedConstructorConverter<TypeToConvert>
    {
        private JsonClassInfo.ParameterizedConstructorDelegate<TypeToConvert, TArg0, TArg1, TArg2, TArg3>? _createObject;

        // Whether or not the extension data is typeof(object) or typoef(JsonElement).
        private bool _extensionDataIsObject;

        internal override void Initialize(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            base.Initialize(constructor, options);

            _createObject = options.MemberAccessorStrategy.CreateParameterizedConstructor<TypeToConvert, TArg0, TArg1, TArg2, TArg3>(constructor)!;
            _extensionDataIsObject =
                DataExtensionProperty != null &&
                typeof(IDictionary<string, object>).IsAssignableFrom(DataExtensionProperty.RuntimeClassInfo.Type);
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TypeToConvert value)
        {
            bool shouldReadPreservedReferences = options.ReferenceHandling.ShouldReadPreservedReferences();

            JsonClassInfo classInfo = state.Current.JsonClassInfo;

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

                if (_createObject == null)
                {
                    // TODO: New helper saying number of arguments is more than the threshold.
                    throw new NotSupportedException();
                }

                InitializeConstructorArgumentCache(ref state.Current, options);

                // Read until we've parsed all constructor arguments or hit the end token.
                ReadConstructorArguments(ref reader, options, ref state);

                Debug.Assert(state.Current.ConstructorArguments != null);
                var argCache = (ArgumentCache<TArg0, TArg1, TArg2, TArg3>)state.Current.ConstructorArguments;

                obj = _createObject(
                    argCache.Arg0,
                    argCache.Arg1,
                    argCache.Arg2,
                    argCache.Arg3)!;

                Debug.Assert(state.Current.ConstructorArgumentState != null);
                ArrayPool<bool>.Shared.Return(state.Current.ConstructorArgumentState, clearArray: true);

                // Set the properties we've parsed so far.
                for (int i = 0; i < state.Current.CachedPropertyCount; i++)
                {
                    JsonPropertyInfo jsonPropertyInfo = state.Current.ObjectProperties![i].Item1;
                    object? propValue = state.Current.ObjectProperties[i].Item2;

                    jsonPropertyInfo.SetValueAsObject(obj, propValue);
                }

                // Set extension data, if any.
                if (state.Current.ExtensionData != null)
                {
                    DataExtensionProperty!.SetValueAsObject(obj, state.Current.ExtensionData);
                }

                Debug.Assert(state.Current.ObjectProperties != null);
                ArrayPool<ValueTuple<JsonPropertyInfo, object?>>.Shared.Return(state.Current.ObjectProperties);

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

        protected override void ReadConstructorArguments(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

            state.Current.ObjectProperties = ArrayPool<ValueTuple<JsonPropertyInfo, object?>>.Shared.Rent(PropertyCache.Count);

            while (true)
            {
                // Read the property name or EndObject.
                reader.Read();

                JsonTokenType tokenType = reader.TokenType;
                if (tokenType == JsonTokenType.EndObject)
                {
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
                        Debug.Assert(jsonPropertyInfo != null);

                        HandleProperty(ref state, ref reader, unescapedPropertyName, jsonPropertyInfo, options);

                        // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                        state.Current.PropertyIndex++;
                        continue;
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

                        HandleProperty(ref state, ref reader, unescapedPropertyName, jsonPropertyInfo, options);

                        // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
                        state.Current.PropertyIndex++;
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
                    state.Current.FirstPropertyIndex = state.Current.ConstructorParameterIndex + state.Current.PropertyIndex;

                    // Position reader to the next property name or EndObject token.
                    reader.Read();
                    return;
                }
            }
        }

        private void HandleProperty(ref ReadStack state, ref Utf8JsonReader reader, ReadOnlySpan<byte> unescapedPropertyName, JsonPropertyInfo jsonPropertyInfo, JsonSerializerOptions options)
        {
            bool useExtensionProperty;
            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                if (DataExtensionProperty != null)
                {
                    if (state.Current.ExtensionData == null)
                    {
                        if (DataExtensionProperty.RuntimeClassInfo.CreateObject == null)
                        {
                            throw new NotSupportedException();
                        }

                        state.Current.ExtensionData = DataExtensionProperty.RuntimeClassInfo.CreateObject();
                    }

                    state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
                    jsonPropertyInfo = DataExtensionProperty;
                    state.Current.JsonPropertyInfo = jsonPropertyInfo;
                    useExtensionProperty = true;
                }
                else
                {
                    //state.Current.EndProperty();
                    reader.Skip();
                    return;
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

            if (!jsonPropertyInfo.ShouldDeserialize)
            {
                state.Current.EndProperty();
                reader.Skip();
                reader.Read();
                return;
            }

            // Set the property value
            reader.Read();

            if (!useExtensionProperty)
            {
                jsonPropertyInfo.ReadJsonAsObject(ref state, ref reader, out object? propValue);

                // Case where we can't fit all the JSON properties in the rented pool, we have to grow.
                if (state.Current.CachedPropertyCount == state.Current.ObjectProperties!.Length)
                {
                    ValueTuple<JsonPropertyInfo, object>[] cache = state.Current.ObjectProperties!;

                    var newCache = ArrayPool<ValueTuple<JsonPropertyInfo, object>>.Shared.Rent(cache.Length * 2);
                    cache.CopyTo(newCache, 0);

                    ArrayPool<ValueTuple<JsonPropertyInfo, object>>.Shared.Return(cache, clearArray: true);
                    state.Current.ObjectProperties = newCache!;
                }

                state.Current.ObjectProperties![state.Current.CachedPropertyCount++] = (jsonPropertyInfo, propValue);
            }
            else
            {
                Debug.Assert(state.Current.ExtensionData != null);

                if (_extensionDataIsObject)
                {
                    jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out object? extDataValue);
                    ((IDictionary<string, object>)state.Current.ExtensionData!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                }
                else
                {
                    jsonPropertyInfo.ReadJsonExtensionDataValue(ref state, ref reader, out JsonElement extDataValue);
                    ((IDictionary<string, JsonElement>)state.Current.ExtensionData!)[state.Current.JsonPropertyNameAsString!] = extDataValue!;
                }
            }

            // Ensure any exception thrown in the next read does not have a property in its JsonPath.
            state.Current.EndProperty();
        }

        protected override void ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options)
        {
            Debug.Assert(state.Current.ConstructorArguments != null);
            Debug.Assert(state.Current.ConstructorArgumentsArray == null);

            var argCache = (ArgumentCache<TArg0, TArg1, TArg2, TArg3>)state.Current.ConstructorArguments;

            switch (jsonParameterInfo.Position)
            {
                case 0:
                    ((JsonParameterInfo<TArg0>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg0 arg0);
                    argCache.Arg0 = arg0;
                    break;
                case 1:
                    ((JsonParameterInfo<TArg1>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg1 arg1);
                    argCache.Arg1 = arg1;
                    break;
                case 2:
                    ((JsonParameterInfo<TArg2>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg2 arg2);
                    argCache.Arg2 = arg2;
                    break;
                case 3:
                    ((JsonParameterInfo<TArg3>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg3 arg3);
                    argCache.Arg3 = arg3;
                    break;
                default:
                    Debug.Fail("This should never happen.");
                    break;
            }
        }

        private void InitializeConstructorArgumentCache(ref ReadStackFrame frame, JsonSerializerOptions options)
        {
            var argCache = new ArgumentCache<TArg0, TArg1, TArg2, TArg3>();

            foreach (JsonParameterInfo parameterInfo in ParameterCache.Values)
            {
                int position = parameterInfo.Position;

                switch (position)
                {
                    case 0:
                        argCache.Arg0 = ((JsonParameterInfo<TArg0>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 1:
                        argCache.Arg1 = ((JsonParameterInfo<TArg1>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 2:
                        argCache.Arg2 = ((JsonParameterInfo<TArg2>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 3:
                        argCache.Arg3 = ((JsonParameterInfo<TArg3>)parameterInfo).TypedDefaultValue!;
                        break;
                    default:
                        Debug.Fail("We should never get here.");
                        break;
                }
            }

            frame.ConstructorArguments = argCache;

            frame.ConstructorArgumentState = ArrayPool<bool>.Shared.Rent(ParameterCount);
            frame.JsonPropertyKindIndicator = ArrayPool<bool>.Shared.Rent(PropertyNameCountCacheThreshold);
        }
    }
}
