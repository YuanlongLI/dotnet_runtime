﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class SingleConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetSingle();
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override float ReadWithQuotes(ref Utf8JsonReader reader)
        {
            if (!reader.TryGetSingleCore(out float value))
            {
                throw ThrowHelper.GetFormatException(NumericType.Single);
            }

            return value;
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, [DisallowNull] float value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }

        internal override bool CanBeDictionaryKey => true;
    }
}
