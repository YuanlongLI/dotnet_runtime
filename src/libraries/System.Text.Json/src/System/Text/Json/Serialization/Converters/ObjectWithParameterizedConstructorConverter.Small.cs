// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed class SmallObjectWithParameterizedConstructorConverter<TypeToConvert, TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> :
        ObjectWithParameterizedConstructorConverter<TypeToConvert>
    {
        private JsonClassInfo.ParameterizedConstructorDelegate<TypeToConvert, TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>? _createObject;

        internal override void Initialize(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            base.Initialize(constructor, options);
            _createObject = options.MemberAccessorStrategy.CreateParameterizedConstructor<TypeToConvert, TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(constructor)!;
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
                var arguments = (ConstructorArguments<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>)state.Current.ConstructorArguments;

                obj = _createObject(
                    arguments.Arg0,
                    arguments.Arg1,
                    arguments.Arg2,
                    arguments.Arg3,
                    arguments.Arg4,
                    arguments.Arg5,
                    arguments.Arg6)!;

                Debug.Assert(state.Current.ConstructorArgumentState != null);
                ArrayPool<bool>.Shared.Return(state.Current.ConstructorArgumentState, clearArray: true);

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

        protected override void ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options)
        {
            Debug.Assert(state.Current.ConstructorArguments != null);
            Debug.Assert(state.Current.ConstructorArgumentsArray == null);

            var arguments = (ConstructorArguments<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>)state.Current.ConstructorArguments;

            switch (jsonParameterInfo.Position)
            {
                case 0:
                    ((JsonParameterInfo<TArg0>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg0 arg0);
                    arguments.Arg0 = arg0;
                    break;
                case 1:
                    ((JsonParameterInfo<TArg1>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg1 arg1);
                    arguments.Arg1 = arg1;
                    break;
                case 2:
                    ((JsonParameterInfo<TArg2>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg2 arg2);
                    arguments.Arg2 = arg2;
                    break;
                case 3:
                    ((JsonParameterInfo<TArg3>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg3 arg3);
                    arguments.Arg3 = arg3;
                    break;
                case 4:
                    ((JsonParameterInfo<TArg4>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg4 arg4);
                    arguments.Arg4 = arg4;
                    break;
                case 5:
                    ((JsonParameterInfo<TArg5>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg5 arg5);
                    arguments.Arg5 = arg5;
                    break;
                case 6:
                    ((JsonParameterInfo<TArg6>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg6 arg6);
                    arguments.Arg6 = arg6;
                    break;
                default:
                    Debug.Fail("This should never happen.");
                    break;
            }
        }

        private void InitializeConstructorArgumentCache(ref ReadStackFrame frame, JsonSerializerOptions options)
        {
            var arguments = new ConstructorArguments<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>();

            foreach (JsonParameterInfo parameterInfo in ParameterCache.Values)
            {
                int position = parameterInfo.Position;

                switch (position)
                {
                    case 0:
                        arguments.Arg0 = ((JsonParameterInfo<TArg0>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 1:
                        arguments.Arg1 = ((JsonParameterInfo<TArg1>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 2:
                        arguments.Arg2 = ((JsonParameterInfo<TArg2>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 3:
                        arguments.Arg3 = ((JsonParameterInfo<TArg3>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 4:
                        arguments.Arg4 = ((JsonParameterInfo<TArg4>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 5:
                        arguments.Arg5 = ((JsonParameterInfo<TArg5>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 6:
                        arguments.Arg6 = ((JsonParameterInfo<TArg6>)parameterInfo).TypedDefaultValue!;
                        break;
                    default:
                        Debug.Fail("We should never get here.");
                        break;
                }
            }

            frame.ConstructorArguments = arguments;

            frame.ConstructorArgumentState = ArrayPool<bool>.Shared.Rent(ParameterCount);
            frame.JsonPropertyKindIndicator = ArrayPool<bool>.Shared.Rent(PropertyNameCountCacheThreshold);
        }
    }
}
