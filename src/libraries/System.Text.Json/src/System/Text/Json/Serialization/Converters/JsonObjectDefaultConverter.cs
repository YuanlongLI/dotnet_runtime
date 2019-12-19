// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonObjectDefaultConverter<T> : JsonObjectConverter<T>
    {
        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T value)
        {
            object obj;

            if (!state.SupportContinuation)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                if (state.Current.JsonClassInfo.CreateObject == null)
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeCreateObjectDelegateIsNull(state.Current.JsonClassInfo.Type);
                }

                obj = state.Current.JsonClassInfo.CreateObject!()!;
                state.Current.ReturnValue = obj;

                // Read all properties.
                while (true)
                {
                    // Lookup the property.
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

                    bool useExtensionProperty = JsonSerializer.LookupProperty(ref reader, options, ref state, out JsonPropertyInfo jsonPropertyInfo);

                    // Skip the property if not found.
                    if (!jsonPropertyInfo.ShouldDeserialize)
                    {
                        reader.TrySkip();
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
                }
            }
            else
            {
                // Read StartObject.
                if (!state.Current.ProcessedStartToken)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    state.Current.ProcessedStartToken = true;
                }

                // Create the object.
                if (state.Current.ReturnValue == null)
                {
                    if (state.Current.JsonClassInfo.CreateObject == null)
                    {
                        ThrowHelper.ThrowNotSupportedException_DeserializeCreateObjectDelegateIsNull(state.Current.JsonClassInfo.Type);
                    }

                    state.Current.ReturnValue = state.Current.JsonClassInfo.CreateObject();
                }

                obj = state.Current.ReturnValue!;

                // Read all properties.
                while (true)
                {
                    // Determine the property.
                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.ReadName)
                    {
                        state.Current.ProcessedPropertyState = StackFramePropertyState.ReadName;

                        if (!reader.Read())
                        {
                            // The read-ahead functionality will do the Read().
                            value = default!;
                            return false;
                        }
                    }

                    JsonPropertyInfo jsonPropertyInfo;

                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.Name)
                    {
                        state.Current.ProcessedPropertyState = StackFramePropertyState.Name;

                        JsonTokenType tokenType = reader.TokenType;

                        if (tokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (tokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.UseExtensionProperty = JsonSerializer.LookupProperty(ref reader, options, ref state, out jsonPropertyInfo);
                    }
                    else
                    {
                        jsonPropertyInfo = state.Current.JsonPropertyInfo;
                    }

                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.ReadValue)
                    {
                        if (!jsonPropertyInfo.ShouldDeserialize)
                        {
                            if (!reader.TrySkip())
                            {
                                value = default!;
                                return false;
                            }

                            state.Current.EndProperty();
                            continue;
                        }

                        // Returning false below will cause the read-ahead functionality to finish the read.
                        state.Current.ProcessedPropertyState = StackFramePropertyState.ReadValue;

                        if (!state.Current.UseExtensionProperty)
                        {
                            if (!SingleValueReadWithReadAhead(jsonPropertyInfo.ConverterBase.ClassType, ref reader, ref state))
                            {
                                value = default!;
                                return false;
                            }
                        }
                        else
                        {
                            // The actual converter is JsonElement, so force a read-ahead.
                            if (!SingleValueReadWithReadAhead(ClassType.Value, ref reader, ref state))
                            {
                                value = default!;
                                return false;
                            }
                        }
                    }

                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.Value)
                    {
                        // Obtain the CLR value from the JSON and set the member.
                        if (!state.Current.UseExtensionProperty)
                        {
                            if (!jsonPropertyInfo.ReadJsonAndSetMember(obj, ref state, ref reader))
                            {
                                value = default!;
                                return false;
                            }
                        }
                        else
                        {
                            if (!jsonPropertyInfo.ReadJsonAndAddExtensionProperty(obj, ref state, ref reader))
                            {
                                // No need to set 'value' here since JsonElement must be read in full.
                                value = default!;
                                return false;
                            }
                        }

                        state.Current.EndProperty();
                    }
                }
            }

            // Check if we are trying to build the sorted cache.
            if (state.Current.PropertyRefCache != null)
            {
                state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);
            }

            value = (T)obj;

            return true;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (!state.SupportContinuation)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return true;
                }

                writer.WriteStartObject();

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
                            if (!jsonPropertyInfo.GetMemberAndWriteJsonExtensionData(value, ref state, writer))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJson(value, ref state, writer))
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
                    if (value == null)
                    {
                        writer.WriteNullValue();
                        return true;
                    }

                    writer.WriteStartObject();
                    state.Current.ProcessedStartToken = true;
                }

                state.Current.CurrentValue = value;

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
                            if (!jsonPropertyInfo.GetMemberAndWriteJsonExtensionData(value!, ref state, writer))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!jsonPropertyInfo.GetMemberAndWriteJson(value!, ref state, writer))
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
    }
}
