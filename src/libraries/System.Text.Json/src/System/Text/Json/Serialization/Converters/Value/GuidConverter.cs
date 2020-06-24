﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class GuidConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetGuid();
        }

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        internal override Guid ReadWithQuotes(ref Utf8JsonReader reader)
        {
            if (!reader.TryGetGuidCore(out Guid value))
            {
                throw ThrowHelper.GetFormatException(DataType.Guid);
            }

            return value;
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }
    }
}
