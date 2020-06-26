// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Deserialize<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
    /// overload has the appropriate linker annotations. A collection and a POCO are used. Public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            int[] arr = new [] { 1 };
            if (JsonSerializer.Serialize(arr) != "[1]")
            {
                return -1;
            }

            MyStruct obj = new MyStruct(1, 2);
            if (!TestHelper.JsonEqual(@"{""X"":1,""Y"":2}", JsonSerializer.Serialize(obj)))
            {
                return -1;
            }

            return 100;
        }
    }
}
