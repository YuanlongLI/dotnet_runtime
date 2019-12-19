// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        internal static void Serialize<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state, string? propertyName = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            JsonConverter jsonConverter = state.Current.InitializeReEntry(typeof(T), options, propertyName);
            Write(writer, value, options, ref state, jsonConverter);
        }

        internal static void Serialize(Utf8JsonWriter writer, object? value, Type inputType, JsonSerializerOptions options, ref WriteStack state, string? propertyName = null)
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            JsonConverter jsonConverter = state.Current.InitializeReEntry(inputType, options, propertyName);
            Write(writer, value, options, ref state, jsonConverter);
        }
    }
}
