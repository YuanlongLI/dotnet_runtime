﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// A converter that is able to resume after reading or writing to a buffer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class JsonResumableConverter<T> : JsonConverter<T>
    {
        public override sealed T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Bridge from resumable to value converters.
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            ReadStack state = default;
            state.Current.InitializeRoot(typeToConvert, options);
            TryRead(ref reader, typeToConvert, options, ref state, out T value);
            return value;
        }

        public override sealed void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // Bridge from resumable to value converters.
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            WriteStack state = default;
            state.Current.InitializeRoot(typeof(T), options);
            state.Current.CurrentValue = value;
            TryWrite(writer, value, options, ref state);
        }
    }
}
