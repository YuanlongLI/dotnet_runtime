﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class UInt32Converter : JsonConverter<uint>
    {
        public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetUInt32();
        }

        public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override uint ReadWithQuotes(ref Utf8JsonReader reader)
        {
            if (!reader.TryGetUInt32Core(out uint value))
            {
                throw ThrowHelper.GetFormatException(NumericType.UInt16);
            }

            return value;
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, uint value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }

        internal override bool CanBeDictionaryKey => true;
    }
}
