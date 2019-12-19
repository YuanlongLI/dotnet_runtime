// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class JsonIEnumerableDefaultConverter<TCollection, TElement> : JsonArrayConverter<TCollection, TElement>
    {
        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TCollection value)
        {
            if (!state.SupportContinuation)
            {
                // Fast path that avoids maintaining state variables.

                // Read StartArray.
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                CreateCollection(ref state);

                JsonConverter<TElement> elementConverter = GetElementConverter(ref state);
                if (elementConverter.CanUseDirectReadOrWrite)
                {
                    // Fast path that avoids validation and extra indirection.
                    while (true)
                    {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }

                        // Obtain the CLR value from the JSON and apply to the object.
                        TElement element = elementConverter.Read(ref reader, elementConverter.TypeToConvert, options);
                        Add(element, ref state);
                    }
                }
                else
                {
                    // Read all items.
                    while (true)
                    {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }

                        // Obtain the CLR value from the JSON and apply to the object.
                        elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement element);
                        Add(element, ref state);
                    }
                }
            }
            else
            {
                // Read StartArray.
                if (!state.Current.ProcessedStartToken)
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    state.Current.ProcessedStartToken = true;
                    CreateCollection(ref state);
                    state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!;
                }

                JsonConverter<TElement> elementConverter = GetElementConverter(ref state);

                // Read all items.
                while (true)
                {
                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.ReadValue)
                    {
                        state.Current.ProcessedPropertyState = StackFramePropertyState.ReadValue;

                        if (!SingleValueReadWithReadAhead(elementConverter.ClassType, ref reader, ref state))
                        {
                            value = default!;
                            return false;
                        }
                    }

                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.ReadValueIsEnd)
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }

                        state.Current.ProcessedPropertyState = StackFramePropertyState.ReadValueIsEnd;
                    }

                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.Value)
                    {
                        // Obtain the CLR value from the JSON and apply to the object.
                        bool success = elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out TElement element);
                        if (!success)
                        {
                            value = default!;
                            return false;
                        }

                        state.Current.ProcessedPropertyState = StackFramePropertyState.Value;
                        Add(element, ref state);

                        state.Current.EndElement();
                    }
                }
            }

            ConvertCollection(ref state);
            value = (TCollection)state.Current.ReturnValue!;
            return true;
        }

        internal override sealed bool OnTryWrite(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            bool success;

            if (value == null)
            {
                writer.WriteNullValue();
                success = true;
            }
            else
            {
                if (!state.Current.ProcessedStartToken)
                {
                    state.Current.ProcessedStartToken = true;
                    writer.WriteStartArray();
                    state.Current.DeclaredJsonPropertyInfo = state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!;
                }

                success = OnWriteResume(writer, value, options, ref state);
                if (success)
                {
                    if (!state.Current.ProcessedEndToken)
                    {
                        state.Current.ProcessedEndToken = true;
                        writer.WriteEndArray();
                    }
                }
            }

            return success;
        }

        protected abstract void Add(TElement value, ref ReadStack state);

        protected static JsonConverter<TElement> GetElementConverter(ref ReadStack state)
        {
            JsonConverter<TElement>? converter = state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!.ConverterBase as JsonConverter<TElement>;
            if (converter == null)
            {
                state.Current.JsonClassInfo.ElementClassInfo.PolicyProperty.ThrowCollectionNotSupportedException();
            }

            return converter!;
        }

        protected static JsonConverter<TElement> GetElementConverter(ref WriteStack state)
        {
            JsonConverter<TElement>? converter = state.Current.DeclaredJsonPropertyInfo.ConverterBase as JsonConverter<TElement>;
            if (converter == null)
            {
                state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!.ThrowCollectionNotSupportedException();
            }

            return converter!;
        }

        protected virtual void CreateCollection(ref ReadStack state) { }
        protected virtual void ConvertCollection(ref ReadStack state) { }
        protected abstract bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state);
    }
}
