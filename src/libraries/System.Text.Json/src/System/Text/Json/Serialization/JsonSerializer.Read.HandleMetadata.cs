// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        internal static bool ResolveMetadata<T>(
            this JsonConverter converter,
            ref Utf8JsonReader reader,
            ref ReadStack state,
            out T value)
        {
            if (state.Current.ObjectState < StackFrameObjectState.MetadataPropertyName)
            {
                // Read the first metadata property name.
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    // An enumerable needs metadata since it starts with StartObject.
                    if (converter.ClassType == ClassType.Enumerable)
                    {
                        ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(converter.TypeToConvert);
                    }

                    // The reader should have detected other invalid cases.
                    Debug.Assert(reader.TokenType == JsonTokenType.EndObject);

                    value = default!;
                    return true;
                }

                ReadOnlySpan<byte> propertyName = GetSpan(ref reader);
                MetadataPropertyName metadata = GetMetadataPropertyName(propertyName);
                if (metadata == MetadataPropertyName.Ref)
                {
                    state.Current.JsonPropertyName = propertyName.ToArray();
                    if (converter.TypeToConvert.IsValueType)
                    {
                        ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(converter.TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.MetadataRefProperty;
                }
                else if (metadata == MetadataPropertyName.Id)
                {
                    state.Current.JsonPropertyName = propertyName.ToArray();
                    if (!converter.CanHaveIdMetadata)
                    {
                        ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(converter.TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.MetadataIdProperty;
                }
                else if (metadata == MetadataPropertyName.Values)
                {
                    state.Current.JsonPropertyName = propertyName.ToArray();
                    if (converter.ClassType == ClassType.Enumerable)
                    {
                        ThrowHelper.ThrowJsonException_MetadataMissingIdBeforeValues();
                    }
                    else
                    {
                        ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, reader);
                    }
                }
                else
                {
                    Debug.Assert(metadata == MetadataPropertyName.NoMetadata);

                    // Having a StartObject without metadata properties is not allowed.
                    if (converter.ClassType == ClassType.Enumerable)
                    {
                        state.Current.JsonPropertyName = propertyName.ToArray();
                        ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(converter.TypeToConvert, reader, ref state);
                    }

                    // Skip the read of the first property name, since we already read it above.
                    state.Current.PropertyState = StackFramePropertyState.ReadName;
                    value = default!;
                    return true;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.MetadataRefProperty)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                }

                string key = reader.GetString()!;

                // todo: verify value is T and throw JsonException? (currently no test)
                value = (T)state.ReferenceResolver.ResolveReferenceOnDeserialize(key);

                state.Current.ReturnValue = value;
                state.Current.ObjectState = StackFrameObjectState.MetadataRefPropertyEndObject;
            }
            else if (state.Current.ObjectState == StackFrameObjectState.MetadataIdProperty)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                }

                state.Current.MetadataId = reader.GetString();

                // Clear the MetadataPropertyName since we are done processing Id.
                state.Current.JsonPropertyName = default;

                if (converter.ClassType == ClassType.Enumerable)
                {
                    // Need to Read $values property name.
                    state.Current.ObjectState = StackFrameObjectState.MetadataValuesPropertyName;
                }
                else
                {
                    // We are done reading metadata.
                    state.Current.ObjectState = StackFrameObjectState.MetadataPropertyValue;
                    value = default!;
                    return true;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.MetadataRefPropertyEndObject)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        state.Current.JsonPropertyName = GetSpan(ref reader).ToArray();
                        ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
                    }

                    ThrowHelper.ThrowJsonException();
                }

                value = (T)state.Current.ReturnValue!;
                return true;
            }

            if (state.Current.ObjectState == StackFrameObjectState.MetadataValuesPropertyName)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(converter.TypeToConvert);
                }

                ReadOnlySpan<byte> propertyName = GetSpan(ref reader);

                // Remember the property in case we get an exception.
                state.Current.JsonPropertyName = propertyName.ToArray();

                if (GetMetadataPropertyName(propertyName) != MetadataPropertyName.Values)
                {
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(converter.TypeToConvert, reader, ref state);
                }

                state.Current.ObjectState = StackFrameObjectState.MetadataValuesPropertyStartArray;
            }

            if (state.Current.ObjectState == StackFrameObjectState.MetadataValuesPropertyStartArray)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    ThrowHelper.ThrowJsonException_MetadataValuesInvalidToken(reader.TokenType);
                }

                state.Current.ObjectState = StackFrameObjectState.MetadataPropertyValue;
            }

            value = default!;
            return true;
        }

        internal static MetadataPropertyName GetMetadataPropertyName(ReadOnlySpan<byte> propertyName)
        {
            if (propertyName.Length > 0 && propertyName[0] == '$')
            {
                switch (propertyName.Length)
                {
                    case 3:
                        if (propertyName[1] == 'i' &&
                            propertyName[2] == 'd')
                        {
                            return MetadataPropertyName.Id;
                        }
                        break;

                    case 4:
                        if (propertyName[1] == 'r' &&
                            propertyName[2] == 'e' &&
                            propertyName[3] == 'f')
                        {
                            return MetadataPropertyName.Ref;
                        }
                        break;

                    case 7:
                        if (propertyName[1] == 'v' &&
                            propertyName[2] == 'a' &&
                            propertyName[3] == 'l' &&
                            propertyName[4] == 'u' &&
                            propertyName[5] == 'e' &&
                            propertyName[6] == 's')
                        {
                            return MetadataPropertyName.Values;
                        }
                        break;
                }
            }

            return MetadataPropertyName.NoMetadata;
        }

        internal static void ThrowUnexpectedMetadataException(
            ReadOnlySpan<byte> propertyName,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            state.Current.JsonPropertyName = propertyName.ToArray();

            MetadataPropertyName name = GetMetadataPropertyName(propertyName);
            if (name == MetadataPropertyName.Id)
            {
                ThrowHelper.ThrowJsonException_MetadataIdIsNotFirstProperty();
            }
            else if (name == MetadataPropertyName.Ref)
            {
                ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
            }
            else
            {
                ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, reader);
            }
        }
    }
}
