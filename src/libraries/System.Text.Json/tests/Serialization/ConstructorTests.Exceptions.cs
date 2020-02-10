// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ConstructorTests
    {
        [Fact]
        public static void DeserializePathForObjectFails()
        {
            const string GoodJson = "{\"Property\u04671\":1}";
            const string GoodJsonEscaped = "{\"Property\\u04671\":1}";
            const string BadJson = "{\"Property\u04671\":bad}";
            const string BadJsonEscaped = "{\"Property\\u04671\":bad}";
            const string Expected = "$.Property\u04671";

            ClassWithUnicodePropertyName obj;

            // Baseline.
            obj = JsonSerializer.Deserialize<ClassWithUnicodePropertyName>(GoodJson);
            Assert.Equal(1, obj.Property\u04671);

            obj = JsonSerializer.Deserialize<ClassWithUnicodePropertyName>(GoodJsonEscaped);
            Assert.Equal(1, obj.Property\u04671);

            JsonException e;

            // Exception.
            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithUnicodePropertyName>(BadJson));
            Assert.Equal(Expected, e.Path);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithUnicodePropertyName>(BadJsonEscaped));
            Assert.Equal(Expected, e.Path);
        }

        private class ClassWithUnicodePropertyName
        {
            public int Property\u04671 { get; } // contains a trailing "1"

            public ClassWithUnicodePropertyName(int property\u04671)
            {
                Property\u04671 = property\u04671;
            }
        }

        [Fact]
        public static void PathForChildPropertyFails()
        {
            try
            {
                JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""MyInt"":bad]}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal("$.Child.MyInt", e.Path);
            }
        }

        public class RootClass
        {
            public ChildClass Child { get; }

            public RootClass(ChildClass child)
            {
                Child = child;
            }
        }

        public class ChildClass
        {
            public int MyInt { get; set; }
            public int[] MyIntArray { get; set; }
            public Dictionary<string, ChildClass> MyDictionary { get; set; }
            public ChildClass[] Children { get; set; }
        }

        [Fact]
        public static void PathForChildListFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""MyIntArray"":[1, bad]}"));
            Assert.Equal("$.Child.MyIntArray[1]", e.Path);
        }

        [Fact]
        public static void PathForChildDictionaryFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""MyDictionary"":{""Key"": bad]"));
            Assert.Equal("$.Child.MyDictionary.Key", e.Path);
        }

        [Fact]
        public static void PathForSpecialCharacterFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""MyDictionary"":{""Key1"":{""Children"":[{""MyDictionary"":{""K.e.y"":"""));
            Assert.Equal("$.Child.MyDictionary.Key1.Children[0].MyDictionary['K.e.y']", e.Path);
        }

        [Fact]
        public static void PathForSpecialCharacterNestedFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RootClass>(@"{""Child"":{""Children"":[{}, {""MyDictionary"":{""K.e.y"": {""MyInt"":bad"));
            Assert.Equal("$.Child.Children[1].MyDictionary['K.e.y'].MyInt", e.Path);
        }

        [Fact]
        public static void EscapingFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Parameterized_ClassWithUnicodeProperty>("{\"A\u0467\":bad}"));
            Assert.Equal("$.A\u0467", e.Path);
        }

        public class Parameterized_ClassWithUnicodeProperty
        {
            public int A\u0467 { get; }

            public Parameterized_ClassWithUnicodeProperty(int a\u0467)
            {
                A\u0467 = a\u0467;
            }
        }

        [Fact]
        [ActiveIssue("JsonElement needs to support Path")]
        public static void ExtensionPropertyRoundTripFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Parameterized_ClassWithExtensionProperty>(@"{""MyNestedClass"":{""UnknownProperty"":bad}}"));
            Assert.Equal("$.MyNestedClass.UnknownProperty", e.Path);
        }

        private class Parameterized_ClassWithExtensionProperty
        {
            public SimpleTestClass MyNestedClass { get; }
            public int MyInt { get; }

            [JsonExtensionData]
            public IDictionary<string, JsonElement> MyOverflow { get; set; }

            public Parameterized_ClassWithExtensionProperty(SimpleTestClass myNestedClass, int myInt)
            {
                MyNestedClass = myNestedClass;
                MyInt = myInt;
            }
        }

        [Fact]
        [ActiveIssue("Add naming policy for constructor parameters")]
        public static void CaseInsensitiveFails()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            // Baseline (no exception)
            {
                var obj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(@"{""mydecimal"":1}", options);
                Assert.Equal(1, obj.MyDecimal);
            }

            {
                var obj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(@"{""MYDECIMAL"":1}", options);
                Assert.Equal(1, obj.MyDecimal);
            }

            JsonException e;

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(@"{""mydecimal"":bad}", options));
            Assert.Equal("$.myint32", e.Path);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(@"{""MYDECIMAL"":bad}", options));
            Assert.Equal("$.MYINT32", e.Path);
        }

        [Fact]
        public static void ClassWithUnsupportedCollectionTypes()
        {
            Exception e;

            e = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithInvalidArray>(@"{""UnsupportedArray"":[]}"));

            // The exception should contain the parent type and the property name.
            Assert.Contains("ClassWithInvalidArray.unsupportedArray", e.ToString());

            e = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithInvalidDictionary>(@"{""UnsupportedDictionary"":{}}"));
            Assert.Contains("System.Int32[,]", e.ToString());

            // The exception for element types do not contain the parent type and the property name
            // since the verification occurs later and is no longer bound to the parent type.
            Assert.DoesNotContain("ClassWithInvalidDictionary.unsupportedDictionary", e.ToString());
        }

        private class ClassWithInvalidArray
        {
            public int[,] UnsupportedArray { get; set; }

            public ClassWithInvalidArray(int[,] unsupportedArray)
            {
                UnsupportedArray = unsupportedArray;
            }
        }

        private class ClassWithInvalidDictionary
        {
            public Dictionary<string, int[,]> UnsupportedDictionary { get; set; }

            public ClassWithInvalidDictionary(Dictionary<string, int[,]> unsupportedDictionary)
            {
                UnsupportedDictionary = unsupportedDictionary;
            }
        }
    }
}
