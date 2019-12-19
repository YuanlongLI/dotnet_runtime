// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        internal static T Deserialize<T>(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state, string? propertyName = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            state.Current.InitializeReEntry(typeof(T), options, propertyName);

            return (T)ReadCoreReEntry(options, ref reader, ref state)!;
        }
    }
}
