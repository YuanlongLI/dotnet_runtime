// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ConstrutorTests
    {
        [Theory]
        [InlineData(typeof(Point_2D))]
        //[InlineData(typeof(Point_3D))]
        public static void ReturnNullForNullObjects(Type type)
        {
            Assert.Null(JsonSerializer.Deserialize("null", type));
        }

        [Fact]
        public static void JsonExceptionWhenAssigningNullToStruct()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Point_2D_With_ExtData>("null"));
        }

        [Fact]
        public static void MatchJsonPropertyToConstructorParameters_CaseSensitive_NoDefaultValues()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""x"":1}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""y"":2}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""Y"":2}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""y"":2,""X"":1}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""a"":1,""b"":2}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{}"));
        }

        public class Point_2D
        {
            public int X { get; }

            public int Y { get; }

            [JsonConstructor]
            public Point_2D(int x, int y) => (X, Y) = (x, y);
        }

        [Fact]
        public static void MatchJsonPropertyToConstructorParameters_CaseInsensitive_NoDefaultValues()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":1,""y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""x"":1}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""y"":2}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""X"":1}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""Y"":2}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{""a"":1,""b"":2}"));
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Point_2D>(@"{}"));
        }

        [Fact]
        public static void MatchJsonPropertyToConstructorParameters_CaseSensitive_WithDefaultValues()
        {
            var options = new JsonSerializerOptions()
            {
                UseConstructorParameterDefaultValues = true,
            };

            Point_3D point = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""y"":2,""z"":3}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(50, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);
            Assert.Equal(50, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{}", options);
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
            Assert.Equal(50, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1,""y"":2,""Z"":3}", options);
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(50, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1,""y"":2}", options);
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(50, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1,""Y"":2}", options);
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
            Assert.Equal(50, point.Z);
        }

        private class Point_3D
        {
            public int X { get; }

            public int Y { get; }

            public int Z { get; }

            [JsonConstructor]
            public Point_3D(int x, int y, int z = 50) => (X, Y, Z) = (x, y, z);
        }

        [Fact]
        public static void MatchJsonPropertyToConstructorParameters_CaseInsensitive_WithDefaultValues()
        {
            var options = new JsonSerializerOptions()
            {
                ConstructorParameterNameCaseInsensitive = true,
                UseConstructorParameterDefaultValues = true,
            };

            Point_3D point = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""y"":2,""z"":3}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1,""Y"":2,""Z"":3}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""Y"":2,""z"":3}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(50, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""Z"":3}", options);
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
            Assert.Equal(30, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{}", options);
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
            Assert.Equal(50, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1,""y"":2,""Z"":3}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1,""y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(50, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1,""Y"":2}", options);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(50, point.Z);
        }

        [Fact]
        public static void MatchJsonPropertyToConstructorParameters_ExtraProperties_AreIgnored()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""y"":2,""b"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public static void MatchJsonPropertyToConstructorParameters_ExtraProperties_GoInExtensionData()
        {
            Point_2D_With_ExtData point = JsonSerializer.Deserialize<Point_2D_With_ExtData>(@"{""x"":1,""y"":2,""b"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.ExtensionData["b"].GetInt32());
        }

        public struct Point_2D_With_ExtData
        {
            public int X { get; }

            public int Y { get; }

            [JsonConstructor]
            public Point_2D_With_ExtData(int x, int y)
            {
                X = x;
                Y = y;

                // Users will have to do this as well
                ExtensionData = new Dictionary<string, JsonElement>();
            }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }

        [Fact]
        public static void PropertiesNotSet_WhenJSON_MapsToConstructorParameters()
        {
            var options = new JsonSerializerOptions
            {
                ConstructorParameterNameCaseInsensitive = true,
            };

            var obj = JsonSerializer.Deserialize<Point_PropertiesHavePropertyNames>(@"{""A"":1,""b"":2}", options);
            Assert.Equal(40, obj.X); // Would be 1 if property were set directly.
            Assert.Equal(60, obj.Y); // Would be 2 if property were set directly.

            options = new JsonSerializerOptions
            {
                ConstructorParameterNameCaseInsensitive = true,
                PropertyNamingPolicy = new PointPropertyNamingPolicy()
            };

            var obj2 = JsonSerializer.Deserialize<Point_2D_Mutable>(@"{""A"":1,""b"":2}", options);
            Assert.Equal(40, obj2.X); // Would be 1 if property were set directly.
            Assert.Equal(60, obj2.Y); // Would be 2 if property were set directly.
        }

        public class Point_PropertiesHavePropertyNames
        {
            [JsonPropertyName("A")]
            public int X { get; set; }

            [JsonPropertyName("b")]
            public int Y { get; set; }

            [JsonConstructor]
            public Point_PropertiesHavePropertyNames(int a, int b)
            {
                X = 40;
                Y = 60;
            }
        }

        public class Point_2D_Mutable
        {
            public int X { get; set; }

            public int Y { get; set; }

            [JsonConstructor]
            public Point_2D_Mutable(int a, int b) => (X, Y) = (a, b);
        }

        public class PointPropertyNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                if (name == "X")
                {
                    return "A";
                }
                else if (name == "Y")
                {
                    return "B";
                }

                throw new ArgumentOutOfRangeException();
            }
        }

        [Fact]
        public static void PropertiesSet_WhenJSON_DoesNotMapToConstructorParameters()
        {
            var obj = JsonSerializer.Deserialize<Point_2D_Mutable>(@"{""X"":1,""Y"":2}");
            Assert.Equal(1, obj.X); // Would be 40 if constructor were called.
            Assert.Equal(2, obj.Y); // Would be 60 if constructor were called.
        }

        [Fact]
        public static void OptionsImmutableAfterSerializationStarts()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var obj = JsonSerializer.Deserialize<Point_2D_Mutable>(@"{""X"":1,""Y"":2}", options);
            Assert.Equal(1, obj.X);
            Assert.Equal(2, obj.Y);

            Assert.Throws<InvalidOperationException>(() => options.ConstructorParameterNameCaseInsensitive == true);
            Assert.Throws<InvalidOperationException>(() => options.UseConstructorParameterDefaultValues == true);
        }

        [Fact]
        public static void MappingInvalidJsonToConstructorParameterFails()
        {

        }

        [Fact]
        public static void SettingNullableParameterToNullWorks()
        {

        }

        [Fact]
        public static void SettingNonNullableParameterToNullFails()
        {

        }

        [Fact]
        public static void IgnoreNullValues_DoesNotApply_ToConstructorArguments()
        {

        }

        [Fact]
        public static void NumerousSimpleAndComplexParameters()
        {

        }

        private class ClassWithSimpleAndComplexParameters : ITestClass
        {
            public byte MyByte { get; }
            public sbyte MySByte { get; set; }
            public char MyChar { get; }
            public string MyString { get; set; }
            public decimal MyDecimal { get; }
            public bool MyBooleanTrue { get; set; }
            public bool MyBooleanFalse { get; }
            public float MySingle { get; set; }
            public double MyDouble { get; }
            public DateTime MyDateTime { get; set; }
            public DateTimeOffset MyDateTimeOffset { get; }
            public Guid MyGuid { get; }
            public Uri MyUri { get; set; }
            public SampleEnum MyEnum { get; }
            public SampleEnumInt64 MyInt64Enum { get; }
            public SampleEnumUInt64 MyUInt64Enum { get; }
            public SimpleStruct MySimpleStruct { get; }
            public SimpleTestStruct MySimpleTestStruct { get; set; }
            public int[][][] MyInt16ThreeDimensionArray { get; }
            public List<List<List<int>>> MyInt16ThreeDimensionList { get;  }
            public List<string> MyStringList { get; }
            public IEnumerable MyStringIEnumerable { get; set; }
            public IList MyStringIList { get; }
            public ICollection MyStringICollection { get; set; }
            public IEnumerable<string> MyStringIEnumerableT { get; }


            public IReadOnlyList<string> MyStringIReadOnlyListT { get; }
            public ISet<string> MyStringISetT { get; set; }
            public KeyValuePair<string, string> MyStringToStringKeyValuePair { get; }
            public IDictionary MyStringToStringIDict { get; set; }
            public Dictionary<string, string> MyStringToStringGenericDict { get; }
            public IDictionary<string, string> MyStringToStringGenericIDict { get; set; }

            public IImmutableDictionary<string, string> MyStringToStringIImmutableDict { get; }
            public ImmutableQueue<string> MyStringImmutablQueueT { get; set; }
            public ImmutableSortedSet<string> MyStringImmutableSortedSetT { get; }
            public List<string> MyListOfNullString { get; }

            public ClassWithSimpleAndComplexParameters(
                byte myByte,
                char myChar,
                string myString,
                decimal myDecimal,
                bool myBooleanFalse,
                double myDouble,
                DateTimeOffset myDateTimeOffset,
                Guid myGuid,
                SampleEnum myEnum,
                SampleEnumInt64 myInt64Enum,
                SampleEnumUInt64 myUInt64Enum,
                SimpleStruct mySimpleStruct,
                int[][][] myInt16ThreeDimensionArray,
                List<List<List<int>>> myInt16ThreeDimensionList,
                List<string> myStringList,
                IList myStringIList,
                IEnumerable<string> myStringIEnumerableT,
                IReadOnlyList<string> myStringIReadOnlyListT,
                KeyValuePair<string, string> myStringToStringKeyValuePair,
                Dictionary<string, string> myStringToStringGenericDict,
                IImmutableDictionary<string, string> myStringToStringIImmutableDict,
                ImmutableSortedSet<string> myStringImmutableSortedSetT,
                List<string> myListOfNullString)
            {
                MyByte = myByte;
                MyChar = myChar;
                MyString = myString;
                MyDecimal = myDecimal;
                MyBooleanFalse = myBooleanFalse;
                MyDouble = myDouble;
                MyDateTimeOffset = myDateTimeOffset;
                MyGuid = myGuid;
                MyEnum = myEnum;
                MyInt64Enum = myInt64Enum;
                MyUInt64Enum = myUInt64Enum;
                MySimpleStruct = mySimpleStruct;
                MyInt16ThreeDimensionArray = myInt16ThreeDimensionArray;
                MyInt16ThreeDimensionList = myInt16ThreeDimensionList;
                MyStringList = myStringList;
                MyStringIList = myStringIList;
                MyStringIEnumerableT = myStringIEnumerableT;
                MyStringIReadOnlyListT = myStringIReadOnlyListT;
                MyStringToStringKeyValuePair = myStringToStringKeyValuePair;
                MyStringToStringGenericDict = myStringToStringGenericDict;
                MyStringToStringIImmutableDict = myStringToStringIImmutableDict;
                MyStringImmutableSortedSetT = myStringImmutableSortedSetT;
                MyListOfNullString = myListOfNullString;
            }

            public static readonly string s_json =
                @"{" +
                @"""MyByte"" : 7," +
                @"""MySByte"" : 8," +
                @"""MyChar"" : ""a""," +
                @"""MyString"" : ""Hello""," +
                @"""MyBooleanTrue"" : true," +
                @"""MyBooleanFalse"" : false," +
                @"""MySingle"" : 1.1," +
                @"""MyDouble"" : 2.2," +
                @"""MyDecimal"" : 3.3," +
                @"""MyDateTime"" : ""2019-01-30T12:01:02.0000000Z""," +
                @"""MyDateTimeOffset"" : ""2019-01-30T12:01:02.0000000+01:00""," +
                @"""MyGuid"" : ""1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6""," +
                @"""MyUri"" : ""https://github.com/dotnet/runtime""," +
                @"""MyEnum"" : 2," + // int by default
                @"""MyInt64Enum"" : -9223372036854775808," +
                @"""MyUInt64Enum"" : 18446744073709551615," +
                @"""MyInt16ThreeDimensionArray"" : [[[11, 12],[13, 14]],[[21,22],[23,24]]]," +
                @"""MyInt16ThreeDimensionList"" : [[[11, 12],[13, 14]],[[21,22],[23,24]]]," +
                @"""MyStringList"" : [""Hello""]," +
                @"""MyStringIEnumerable"" : [""Hello""]," +
                @"""MyStringIList"" : [""Hello""]," +
                @"""MyStringICollection"" : [""Hello""]," +
                @"""MyStringIEnumerableT"" : [""Hello""]," +
                @"""MyStringIReadOnlyListT"" : [""Hello""]," +
                @"""MyStringISetT"" : [""Hello""]," +
                @"""MyStringToStringKeyValuePair"" : {""Key"" : ""myKey"", ""Value"" : ""myValue""}," +
                @"""MyStringToStringIDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringGenericDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringGenericIDict"" : {""key"" : ""value""}," +
                @"""MyStringToStringIImmutableDict"" : {""key"" : ""value""}," +
                @"""MyStringImmutablQueueT"" : [""Hello""]," +
                @"""MyStringImmutableSortedSetT"" : [""Hello""]," +
                @"""MyListOfNullString"" : [null]" +
                @"}";

            public void Initialize() { }

            public void Verify()
            {
                Assert.Equal((byte)7, MyByte);
                Assert.Equal((sbyte)8, MySByte);
                Assert.Equal('a', MyChar);
                Assert.Equal("Hello", MyString);
                Assert.Equal(3.3m, MyDecimal);
                Assert.False(MyBooleanFalse);
                Assert.True(MyBooleanTrue);
                Assert.Equal(1.1f, MySingle);
                Assert.Equal(2.2d, MyDouble);
                Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), MyDateTime);
                Assert.Equal(new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)), MyDateTimeOffset);
                Assert.Equal(SampleEnum.Two, MyEnum);
                Assert.Equal(SampleEnumInt64.MinNegative, MyInt64Enum);
                Assert.Equal(SampleEnumUInt64.Max, MyUInt64Enum);
                Assert.Equal(11, MySimpleStruct.One);
                Assert.Equal(1.9999, MySimpleStruct.Two);

                Assert.Equal(11, MyInt16ThreeDimensionArray[0][0][0]);
                Assert.Equal(12, MyInt16ThreeDimensionArray[0][0][1]);
                Assert.Equal(13, MyInt16ThreeDimensionArray[0][1][0]);
                Assert.Equal(14, MyInt16ThreeDimensionArray[0][1][1]);
                Assert.Equal(21, MyInt16ThreeDimensionArray[1][0][0]);
                Assert.Equal(22, MyInt16ThreeDimensionArray[1][0][1]);
                Assert.Equal(23, MyInt16ThreeDimensionArray[1][1][0]);
                Assert.Equal(24, MyInt16ThreeDimensionArray[1][1][1]);

                Assert.Equal(11, MyInt16ThreeDimensionList[0][0][0]);
                Assert.Equal(12, MyInt16ThreeDimensionList[0][0][1]);
                Assert.Equal(13, MyInt16ThreeDimensionList[0][1][0]);
                Assert.Equal(14, MyInt16ThreeDimensionList[0][1][1]);
                Assert.Equal(21, MyInt16ThreeDimensionList[1][0][0]);
                Assert.Equal(22, MyInt16ThreeDimensionList[1][0][1]);
                Assert.Equal(23, MyInt16ThreeDimensionList[1][1][0]);
                Assert.Equal(24, MyInt16ThreeDimensionList[1][1][1]);

                Assert.Equal("Hello", MyStringList[0]);

                IEnumerator enumerator = MyStringIEnumerable.GetEnumerator();
                enumerator.MoveNext();
                Assert.Equal("Hello", ((JsonElement)enumerator.Current).GetString());

                Assert.Equal("Hello", ((JsonElement)MyStringIList[0]).GetString());
                
                enumerator = MyStringICollection.GetEnumerator();
                enumerator.MoveNext();
                Assert.Equal("Hello", ((JsonElement)enumerator.Current).GetString());

                Assert.Equal("Hello", MyStringIEnumerableT.First());

                Assert.Equal("Hello", MyStringIReadOnlyListT[0]);
                Assert.Equal("Hello", MyStringISetT.First());

                Assert.Equal("myKey", MyStringToStringKeyValuePair.Key);
                Assert.Equal("myValue", MyStringToStringKeyValuePair.Value);

                enumerator = MyStringToStringIDict.GetEnumerator();
                enumerator.MoveNext();
                JsonElement currentJsonElement = (JsonElement)enumerator.Current;
                IEnumerator jsonEnumerator = currentJsonElement.EnumerateObject();
                jsonEnumerator.MoveNext();

                JsonProperty property = (JsonProperty)jsonEnumerator.Current;

                Assert.Equal("key", property.Name);
                Assert.Equal("value", property.Value.GetString());

                Assert.Equal("value", MyStringToStringGenericDict["key"]);
                Assert.Equal("value", MyStringToStringGenericIDict["key"]);
                Assert.Equal("value", MyStringToStringIImmutableDict["key"]);

                Assert.Equal("Hello", MyStringImmutablQueueT.First());
                Assert.Equal("Hello", MyStringImmutableSortedSetT.First());

                Assert.Null(MyListOfNullString[0]);
            }
        }

        [Fact]
        public static void NumerousSimpleAndComplexParameters_WithCaseInsensitivity_And_DefaultValuePopulation()
        {

        }

        [Fact]
        public static void TupleDeserializationWorks()
        {
            var tuple = JsonSerializer.Deserialize<Tuple<string, double>>(@"{""Item1"":""New York"",""Item2"":32.68}");
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);
        }

        [Fact]
        public static void NoConctructorHandlingWhenObjectHasConverter()
        {
        }

        [Fact]
        public static void ConstructorHandlingHonorsCustomConverters()
        {

        }

        [Fact]
        public static void CanDeserializeConstrutorWith260Parameters()
        {

        }

        [Fact]
        public static void FailsTo_DeserializeConstrutorWith_MoreThan260Parameters()
        {

        }
    }
}
