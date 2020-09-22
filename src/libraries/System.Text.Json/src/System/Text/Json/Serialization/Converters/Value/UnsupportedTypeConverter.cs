// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class UnsupportedTypeConverter<T> : JsonConverter<T>
    {
        private static readonly string s_exceptionMessage = SR.Format(SR.SerializeTypeInstanceNotSupported, typeof(T).FullName);

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException(s_exceptionMessage);

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            throw new NotSupportedException(s_exceptionMessage);
    }
}
