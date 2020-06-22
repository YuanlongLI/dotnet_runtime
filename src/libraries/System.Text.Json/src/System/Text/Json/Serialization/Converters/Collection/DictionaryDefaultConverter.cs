﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Default base class implementation of <cref>JsonDictionaryConverter{TCollection}</cref> .
    /// </summary>
    internal abstract class DictionaryDefaultConverter<TCollection, TKey, TValue>
        : JsonDictionaryConverter<TCollection>
        where TKey : notnull
    {
        /// <summary>
        /// When overridden, adds the value to the collection.
        /// </summary>
        protected abstract void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state);

        /// <summary>
        /// When overridden, converts the temporary collection held in state.Current.ReturnValue to the final collection.
        /// This is used with immutable collections.
        /// </summary>
        protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options) { }

        /// <summary>
        /// When overridden, create the collection. It may be a temporary collection or the final collection.
        /// </summary>
        protected virtual void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state) { }

        internal override Type ElementType => typeof(TValue);
        internal override Type KeyType => typeof(TKey);
        protected bool IsStringKey = typeof(TKey) == typeof(string);

        protected static JsonConverter<TValue> GetElementConverter(ref ReadStack state)
        {
            JsonConverter<TValue> converter = (JsonConverter<TValue>)state.Current.JsonClassInfo.ElementClassInfo!.PropertyInfoForClassInfo.ConverterBase;
            Debug.Assert(converter != null); // It should not be possible to have a null converter at this point.

            return converter;
        }

        protected static JsonConverter<TValue> GetValueConverter(ref WriteStack state)
        {
            JsonConverter<TValue> converter = (JsonConverter<TValue>)state.Current.JsonClassInfo.ElementClassInfo!.PropertyInfoForClassInfo.ConverterBase;
            Debug.Assert(converter != null); // It should not be possible to have a null converter at this point.

            return converter;
        }

        protected static JsonConverter<TKey> GetKeyConverter(JsonClassInfo classInfo)
        {
            var converter = (JsonConverter<TKey>)classInfo.KeyClassInfo!.PropertyInfoForClassInfo.ConverterBase;
            if (!converter.CanBeDictionaryKey)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(converter.TypeToConvert);
            }

            return converter;
        }

        internal sealed override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            [MaybeNullWhen(false)] out TCollection value)
        {
            if (state.UseFastPath)
            {
                // Fast path that avoids maintaining state variables and dealing with preserved references.

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                }

                CreateCollection(ref reader, ref state);

                JsonConverter<TValue> elementConverter = GetElementConverter(ref state);
                if (elementConverter.CanUseDirectReadOrWrite)
                {
                    // Process all elements.
                    while (true)
                    {
                        // Read the key name.
                        reader.ReadWithVerify();

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        // Read method would have thrown if otherwise.
                        Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                        if (options.ReferenceHandler != null)
                        {
                            ReadOnlySpan<byte> propertyName = reader.GetSpan();
                            if (propertyName.Length > 0 && propertyName[0] == '$')
                            {
                                ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                            }
                        }

                        JsonConverter<TKey> keyConverter = GetKeyConverter(state.Current.JsonClassInfo);
                        TKey key = keyConverter.ReadWithQuotes(ref reader);

                        // Copy key name for JSON Path support in case of error.
                        string unescapedPropertyNameAsString;
                        if (IsStringKey)
                        {
                            // Special case to avoid calling again GetString()
                            unescapedPropertyNameAsString = (string)(object)key;
                        }
                        else
                        {
                            unescapedPropertyNameAsString = reader.GetString()!;
                        }

                        state.Current.JsonPropertyNameAsString = unescapedPropertyNameAsString;
                        // Read the value and add.
                        reader.ReadWithVerify();
                        TValue element = elementConverter.Read(ref reader, typeof(TValue), options);
                        Add(key, element!, options, ref state);
                    }
                }
                else
                {
                    // Process all elements.
                    while (true)
                    {
                        // Read the key name.
                        reader.ReadWithVerify();

                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        // Read method would have thrown if otherwise.
                        Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                        if (options.ReferenceHandler != null)
                        {
                            ReadOnlySpan<byte> propertyName = reader.GetSpan();
                            if (propertyName.Length > 0 && propertyName[0] == '$')
                            {
                                ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                            }
                        }

                        JsonConverter<TKey> keyConverter = GetKeyConverter(state.Current.JsonClassInfo);
                        TKey key = keyConverter.ReadWithQuotes(ref reader);

                        // Copy key name for JSON Path support in case of error.
                        string unescapedPropertyNameAsString;
                        if (IsStringKey)
                        {
                            // Special case to avoid calling again GetString()
                            unescapedPropertyNameAsString = (string)(object)key;
                        }
                        else
                        {
                            unescapedPropertyNameAsString = reader.GetString()!;
                        }

                        state.Current.JsonPropertyNameAsString = unescapedPropertyNameAsString;
                        reader.ReadWithVerify();

                        // Get the value from the converter and add it.
                        elementConverter.TryRead(ref reader, typeof(TValue), options, ref state, out TValue element);
                        Add(key, element!, options, ref state);
                    }
                }
            }
            else
            {
                // Slower path that supports continuation and preserved references.

                if (state.Current.ObjectState == StackFrameObjectState.None)
                {
                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.StartToken;
                }

                // Handle the metadata properties.
                bool preserveReferences = options.ReferenceHandler != null;
                if (preserveReferences && state.Current.ObjectState < StackFrameObjectState.PropertyValue)
                {
                    if (JsonSerializer.ResolveMetadata(this, ref reader, ref state))
                    {
                        if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
                        {
                            value = (TCollection)state.Current.ReturnValue!;
                            return true;
                        }
                    }
                    else
                    {
                        value = default;
                        return false;
                    }
                }

                // Create the dictionary.
                if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
                {
                    CreateCollection(ref reader, ref state);

                    if (state.Current.MetadataId != null)
                    {
                        Debug.Assert(CanHaveIdMetadata);

                        value = (TCollection)state.Current.ReturnValue!;
                        state.ReferenceResolver.AddReference(state.Current.MetadataId, value);
                        // Clear metadata name, if the next read fails
                        // we want to point the JSON path to the property's object.
                        state.Current.JsonPropertyName = null;
                    }

                    state.Current.ObjectState = StackFrameObjectState.CreatedObject;
                }

                // Process all elements.
                JsonConverter<TValue> elementConverter = GetElementConverter(ref state);
                while (true)
                {
                    if (state.Current.PropertyState == StackFramePropertyState.None)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadName;

                        // Read the key name.
                        if (!reader.Read())
                        {
                            value = default;
                            return false;
                        }
                    }

                    // Determine the property.
                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        // Read method would have thrown if otherwise.
                        Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                        state.Current.PropertyState = StackFramePropertyState.Name;

                        if (options.ReferenceHandler != null)
                        {
                            ReadOnlySpan<byte> propertyName = reader.GetSpan();
                            if (propertyName.Length > 0 && propertyName[0] == '$')
                            {
                                ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                            }
                        }

                        JsonConverter<TKey> keyConverter = GetKeyConverter(state.Current.JsonClassInfo);
                        TKey key = keyConverter.ReadWithQuotes(ref reader);

                        // Copy key name for JSON Path support in case of error.
                        string unescapedPropertyNameAsString;
                        if (IsStringKey)
                        {
                            // Special case to avoid calling again GetString()
                            unescapedPropertyNameAsString = (string)(object)key;
                        }
                        else
                        {
                            unescapedPropertyNameAsString = reader.GetString()!;
                        }

                        state.Current.JsonPropertyNameAsString = unescapedPropertyNameAsString;
                        state.Current.DictionaryKey = key;
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                    {
                        state.Current.PropertyState = StackFramePropertyState.ReadValue;

                        if (!SingleValueReadWithReadAhead(elementConverter.ClassType, ref reader, ref state))
                        {
                            value = default;
                            return false;
                        }
                    }

                    if (state.Current.PropertyState < StackFramePropertyState.TryRead)
                    {
                        // Get the value from the converter and add it.
                        bool success = elementConverter.TryRead(ref reader, typeof(TValue), options, ref state, out TValue element);
                        if (!success)
                        {
                            value = default;
                            return false;
                        }

                        TKey key = (TKey)state.Current.DictionaryKey!;
                        Add(key, element!, options, ref state);
                        state.Current.EndElement();
                    }
                }
            }

            ConvertCollection(ref state, options);
            value = (TCollection)state.Current.ReturnValue!;
            return true;
        }

        internal sealed override bool OnTryWrite(
            Utf8JsonWriter writer,
            TCollection dictionary,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            if (dictionary == null)
            {
                writer.WriteNullValue();
                return true;
            }

            if (!state.Current.ProcessedStartToken)
            {
                state.Current.ProcessedStartToken = true;
                writer.WriteStartObject();

                if (options.ReferenceHandler != null)
                {
                    if (JsonSerializer.WriteReferenceForObject(this, dictionary, ref state, writer) == MetadataPropertyName.Ref)
                    {
                        return true;
                    }
                }

                state.Current.DeclaredJsonPropertyInfo = state.Current.JsonClassInfo.ElementClassInfo!.PropertyInfoForClassInfo;
            }

            bool success = OnWriteResume(writer, dictionary, options, ref state);
            if (success)
            {
                if (!state.Current.ProcessedEndToken)
                {
                    state.Current.ProcessedEndToken = true;
                    writer.WriteEndObject();
                }
            }

            return success;
        }
    }
}
