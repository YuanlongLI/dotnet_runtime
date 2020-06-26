// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the serializer's JsonSerializer.Deserialize(string json, Type returnType, JsonSerializerOptions options)
    /// overload has the appropriate linker annotations.
    /// A collection and a POCO are used. Public constructors and public properties are expected to be preserved.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            int[] arr = (int[])JsonSerializer.Deserialize(json, typeof(int[]));
            if (arr[0] != 1)
            {
                return -1;
            }

            json = @"{""X"":1,""Y"":2}";
            MyStruct obj = (MyStruct)JsonSerializer.Deserialize(json, typeof(MyStruct));
            if (obj.X != 1 || obj.Y != 2)
            {
                return -1;
            }

            return 100;
        }
    }
}
