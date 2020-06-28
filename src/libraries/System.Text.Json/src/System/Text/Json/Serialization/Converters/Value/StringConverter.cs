﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class StringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        internal override string ReadWithQuotes(ref Utf8JsonReader reader)
        {
            return reader.GetString()!;
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, [DisallowNull] string? value, JsonSerializerOptions options, ref WriteStack state)
        {
            JsonSerializer.WriteDictionaryStringKey(writer, value, options, ref state);
        }
    }
}
