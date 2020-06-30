// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    internal static class TestHelper
    {
        /// <summary>
        /// Used when comparing JSON payloads with more than two properties.
        /// We cannot check for string equality since property ordering depends
        /// on reflection ordering which is not guaranteed.
        /// </summary>
        public static bool JsonEqual(string expected, string actual)
        {
            using JsonDocument expectedDom = JsonDocument.Parse(expected);
            using JsonDocument actualDom = JsonDocument.Parse(actual);
            return JsonEqual(expectedDom.RootElement, actualDom.RootElement);
        }

        private static bool JsonEqual(JsonElement expected, JsonElement actual)
        {
            JsonValueKind valueKind = expected.ValueKind;
            if (valueKind != actual.ValueKind)
            {
                return false;
            }

            switch (valueKind)
            {
                case JsonValueKind.Object:
                    var propertyNames = new HashSet<string>();

                    foreach (JsonProperty property in expected.EnumerateObject())
                    {
                        propertyNames.Add(property.Name);
                    }

                    foreach (JsonProperty property in actual.EnumerateObject())
                    {
                        propertyNames.Add(property.Name);
                    }

                    foreach (string name in propertyNames)
                    {
                        if (!JsonEqual(expected.GetProperty(name), actual.GetProperty(name)))
                        {
                            return false;
                        }
                    }

                    return true;
                case JsonValueKind.Array:
                    JsonElement.ArrayEnumerator expectedEnumerator = actual.EnumerateArray();
                    JsonElement.ArrayEnumerator actualEnumerator = expected.EnumerateArray();

                    while (expectedEnumerator.MoveNext())
                    {
                        if (!actualEnumerator.MoveNext())
                        {
                            return false;
                        }

                        if (!JsonEqual(expectedEnumerator.Current, actualEnumerator.Current))
                        {
                            return false;
                        }
                    }

                    return !actualEnumerator.MoveNext();
                case JsonValueKind.String:
                    return expected.GetString() == actual.GetString();
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    return expected.GetRawText() == actual.GetRawText();
                default:
                    throw new NotSupportedException($"Unexpected JsonValueKind: JsonValueKind.{valueKind}.");
            }
        }

        /// <summary>
        /// Verifies the result of deserialization by serializing and comparing the output
        /// with the expected payload. With this call pattern, the serialize should preserve
        /// properties on typeof(object), but that would not be helpful to the calling test.
        /// </summary>
        public static bool VerifyWithSerialize(object obj, string expected)
        {
            return JsonEqual(expected, JsonSerializer.Serialize(obj));
        }
    }

    internal class MyClass
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    internal struct MyStruct
    {
        public int X { get; }
        public int Y { get; }

        [JsonConstructor]
        public MyStruct(int x, int y) => (X, Y) = (x, y);
    }

    internal class MyBigClass
    {
        public string A { get; }
        public string B { get; }
        public string C { get; }
        public int One { get; }
        public int Two { get; }
        public int Three { get; }

        public MyBigClass(string a, string b, string c, int one, int two, int three)
        {
            A = a;
            B = b;
            C = c;
            One = one;
            Two = two;
            Three = three;
        }
    }
}
