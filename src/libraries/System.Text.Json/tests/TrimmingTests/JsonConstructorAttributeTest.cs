// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the public parameterless ctor of the ConverterType property on
    /// JsonConstructorAttribute is preserved when needed in a trimmed application.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = JsonSerializer.Serialize(new ClassWithDay());
            return json == @"{""Day"":""Friday""}" ? 100 : -1;
        }
    }

    internal class ClassWithDay
    {
        [JsonConverterAttribute(typeof(JsonStringEnumConverter))]
        public DayOfWeek Day { get; set; } = DayOfWeek.Friday;
    }
}
