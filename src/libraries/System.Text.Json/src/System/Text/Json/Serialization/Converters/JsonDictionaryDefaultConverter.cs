﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class JsonDictionaryDefaultConverter<TCollection, TValue> : JsonDictionaryConverter<TCollection>
    {
        protected abstract void Add(TValue value, JsonSerializerOptions options, ref ReadStack state);
        protected virtual void ConvertCollection(ref ReadStack state) { }
        protected virtual void CreateCollection(ref ReadStack state) { }
        internal override Type ElementType => typeof(TValue);

        protected static JsonConverter<TValue> GetElementConverter(ref ReadStack state)
        {
            JsonConverter<TValue>? converter = state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!.ConverterBase as JsonConverter<TValue>;
            if (converter == null)
            {
                state.Current.JsonClassInfo.ElementClassInfo.PolicyProperty.ThrowCollectionNotSupportedException();
            }

            return converter!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected string GetKeyName(string key, ref WriteStack state, JsonSerializerOptions options)
        {
            if (options.DictionaryKeyPolicy != null && !state.Current.IgnoreDictionaryKeyPolicy)
            {
                key = options.DictionaryKeyPolicy.ConvertName(key);

                if (key == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerDictionaryKeyNull(options.DictionaryKeyPolicy.GetType());
                }
            }

            return key;
        }

        protected JsonConverter<TValue> GetValueConverter(ref WriteStack state)
        {
            JsonConverter<TValue> converter = (JsonConverter<TValue>)state.Current.DeclaredJsonPropertyInfo.ConverterBase;
            if (converter == null)
            {
                state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!.ThrowCollectionNotSupportedException();
            }

            return converter!;
        }

        internal override sealed bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            out TCollection value)
        {
            if (!state.SupportContinuation)
            {
                // Fast path that avoids maintaining state variables.

                // Read StartObject.
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                CreateCollection(ref state);

                JsonConverter<TValue> elementConverter = GetElementConverter(ref state);
                if (elementConverter.CanUseDirectReadOrWrite)
                {
                    while (true)
                    {
                        // Read the key name.
                        reader.Read();

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.KeyName = reader.GetString();

                        // Read the value and add.
                        reader.Read();
                        TValue element = elementConverter.Read(ref reader, typeof(TValue), options);
                        Add(element, options, ref state);
                    }
                }
                else
                {
                    while (true)
                    {
                        // Read the key name.
                        reader.Read();

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.KeyName = reader.GetString();

                        // Read the value and add.
                        reader.Read();
                        elementConverter.TryRead(ref reader, typeof(TValue), options, ref state, out TValue element);
                        Add(element, options, ref state);
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
                    CreateCollection(ref state);
                }

                JsonConverter<TValue> elementConverter = GetElementConverter(ref state);

                while (true)
                {
                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.ReadName)
                    {
                        state.Current.ProcessedPropertyState = StackFramePropertyState.ReadName;

                        // Read the key name.
                        if (!reader.Read())
                        {
                            value = default!;
                            return false;
                        }
                    }

                    // Determine the property.
                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.Name)
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        state.Current.ProcessedPropertyState = StackFramePropertyState.Name;
                        state.Current.KeyName = reader.GetString();
                    }

                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.ReadValue)
                    {
                        state.Current.ProcessedPropertyState = StackFramePropertyState.ReadValue;

                        if (!SingleValueReadWithReadAhead(elementConverter.ClassType, ref reader, ref state))
                        {
                            value = default!;
                            return false;
                        }
                    }

                    if (state.Current.ProcessedPropertyState < StackFramePropertyState.Value)
                    {
                        // Read the value and add.
                        bool success = elementConverter.TryRead(ref reader, typeof(TValue), options, ref state, out TValue element);
                        if (!success)
                        {
                            value = default!;
                            return false;
                        }

                        Add(element, options, ref state);
                        state.Current.EndElement();
                    }
                }
            }

            ConvertCollection(ref state);
            value = (TCollection)state.Current.ReturnValue!;
            return true;
        }

        internal override sealed bool OnTryWrite(
            Utf8JsonWriter writer,
            TCollection dictionary,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            bool success;

            if (dictionary == null)
            {
                writer.WriteNullValue();
                success = true;
            }
            else
            {
                if (!state.Current.ProcessedStartToken)
                {
                    state.Current.ProcessedStartToken = true;
                    writer.WriteStartObject();
                    state.Current.DeclaredJsonPropertyInfo = state.Current.JsonClassInfo.ElementClassInfo!.PolicyProperty!;
                }

                success = OnWriteResume(writer, dictionary, options, ref state);
                if (success)
                {
                    if (!state.Current.ProcessedEndToken)
                    {
                        state.Current.ProcessedEndToken = true;
                        writer.WriteEndObject();
                    }
                }
            }

            return success;
        }
    }
}
