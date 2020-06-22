﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int32Converter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetInt32();
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override int ReadWithQuotes(ref Utf8JsonReader reader)
        {
            if (!reader.TryGetInt32Core(out int value))
            {
                throw ThrowHelper.GetFormatException(NumericType.Int32);
            }

            return value;
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, int value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }

        internal override bool CanBeDictionaryKey => true;
    }
}
