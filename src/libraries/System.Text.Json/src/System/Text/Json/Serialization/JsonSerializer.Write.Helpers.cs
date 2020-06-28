﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static void WriteCore<TValue>(
            Utf8JsonWriter writer,
            in TValue value,
            Type inputType,
            JsonSerializerOptions options)
        {
            Debug.Assert(writer != null);

            //  We treat typeof(object) special and allow polymorphic behavior.
            if (value != null && inputType == JsonClassInfo.ObjectType)
            {
                inputType = value!.GetType();
            }

            WriteStack state = default;
            JsonConverter jsonConverter = state.Initialize(inputType, options, supportContinuation: false);

            bool success = WriteCore(jsonConverter, writer, value, options, ref state);
            Debug.Assert(success);
        }

        private static bool WriteCore<TValue>(
            JsonConverter jsonConverter,
            Utf8JsonWriter writer,
            in TValue value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            Debug.Assert(writer != null);

            bool success;

            if (jsonConverter is JsonConverter<TValue> converter)
            {
                // Call the strongly-typed WriteCore that will not box structs.
                success = converter.WriteCore(writer, value, options, ref state);
            }
            else
            {
                // The non-generic API was called or we have a polymorphic case where TValue is not equal to the T in JsonConverter<T>.
                success = jsonConverter.WriteCoreAsObject(writer, value, options, ref state);
            }

            writer.Flush();
            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteDictionaryStringKey(Utf8JsonWriter writer, string value, JsonSerializerOptions options, ref WriteStack state)
        {
            Debug.Assert(value != null);

            if (options.DictionaryKeyPolicy != null && !state.Current.IgnoreDictionaryKeyPolicy)
            {
                value = options.DictionaryKeyPolicy.ConvertName(value);

                if (value == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(options.DictionaryKeyPolicy);
                }
            }

            writer.WritePropertyName(value);
        }
    }
}
