// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ConstructorTests
    {
        [Theory]
        [InlineData(typeof(Point_2D))]
        [InlineData(typeof(Point_3D))]
        public static void ReturnNullForNullObjects(Type type)
        {
            Assert.Null(JsonSerializer.Deserialize("null", type));
        }

        public class Point_2D
        {
            public int X { get; }

            public int Y { get; }

            [JsonConstructor]
            public Point_2D(int x, int y) => (X, Y) = (x, y);
        }

        public class Point_3D
        {
            public int X { get; }

            public int Y { get; }

            public int Z { get; }

            [JsonConstructor]
            public Point_3D(int x, int y, int z = 50) => (X, Y, Z) = (x, y, z);
        }

        [Fact]
        public static void Point()
        {
            string json = @"{""x"":1,""y"":2}";
            JsonSerializer.Deserialize<Point_2D>(json);
            //for (int i = 0; i < 10_000; i++)
            //{
            //    JsonSerializer.Deserialize<Point_2D>(json);
            //}
        }

        [Fact]
        public static void ParameterlessPoint()
        {
            string json = @"{""x"":1,""y"":2}";
            for (int i = 0; i < 10_000; i++)
            {
                JsonSerializer.Deserialize<Parameterless_Point>(json);
            }
        }

        [Fact]
        public static void JsonExceptionWhenAssigningNullToStruct()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Point_2D_With_ExtData>("null"));
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
        public static void MatchJsonPropertyToConstructorParameters()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""y"":2,""x"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""Y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1,""Y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""y"":2,""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public static void UseDefaultValues_When_NoJsonMatch()
        {
            // Using CLR value when `ParameterInfo.DefaultValue` is not set.
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""X"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(0, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""Y"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(2, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            point = JsonSerializer.Deserialize<Point_2D>(@"{""a"":1,""b"":2}");
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);

            // Using `ParameterInfo.DefaultValue` when set; using CLR value as fallback.
            Point_3D point3d = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""X"":1}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""Y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""Z"":3}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""Y"":2}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""Z"":3,""y"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(2, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""Z"":3}");
            Assert.Equal(1, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(3, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);

            point3d = JsonSerializer.Deserialize<Point_3D>(@"{""a"":1,""b"":2}");
            Assert.Equal(0, point3d.X);
            Assert.Equal(0, point3d.Y);
            Assert.Equal(50, point3d.Z);
        }

        [Fact]
        public static void VaryingOrderingOfJson()
        {
            Point_3D point = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""y"":2,""z"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""x"":1,""z"":3,""y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""y"":2,""z"":3,""x"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""y"":2,""x"":1,""z"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""z"":3,""y"":2,""x"":1}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);

            point = JsonSerializer.Deserialize<Point_3D>(@"{""z"":3,""x"":1,""y"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        [Fact]
        public static void AsListElement()
        {
            List<Point_3D> list = JsonSerializer.Deserialize<List<Point_3D>>(@"[{""y"":2,""z"":3,""x"":1},{""z"":10,""y"":30,""x"":20}]");
            Assert.Equal(1, list[0].X);
            Assert.Equal(2, list[0].Y);
            Assert.Equal(3, list[0].Z);
            Assert.Equal(20, list[1].X);
            Assert.Equal(30, list[1].Y);
            Assert.Equal(10, list[1].Z);
        }

        [Fact]
        public static void AsDictionaryValue()
        {
            Dictionary<string, Point_3D> dict = JsonSerializer.Deserialize<Dictionary<string, Point_3D>>(@"{""0"":{""y"":2,""z"":3,""x"":1},""1"":{""z"":10,""y"":30,""x"":20}}");
            Assert.Equal(1, dict["0"].X);
            Assert.Equal(2, dict["0"].Y);
            Assert.Equal(3, dict["0"].Z);
            Assert.Equal(20, dict["1"].X);
            Assert.Equal(30, dict["1"].Y);
            Assert.Equal(10, dict["1"].Z);
        }

        [Fact]
        public static void AsProperty_Of_ObjectWithParameterlessCtor()
        {
            WrapperForPoint_3D obj = JsonSerializer.Deserialize<WrapperForPoint_3D>(@"{""Point_3D"":{""y"":2,""z"":3,""x"":1}}");
            Point_3D point = obj.Point_3D;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        public struct WrapperForPoint_3D
        {
            public Point_3D Point_3D { get; set; }
        }

        [Fact]
        public static void AsProperty_Of_ObjectWithParameterizedCtor()
        {
            ClassWrapperForPoint_3D obj = JsonSerializer.Deserialize<ClassWrapperForPoint_3D>(@"{""Point_3D"":{""y"":2,""z"":3,""x"":1}}");
            Point_3D point = obj.Point_3D;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.Z);
        }

        public class ClassWrapperForPoint_3D
        {
            public Point_3D Point_3D { get; }

            public ClassWrapperForPoint_3D(Point_3D point_3d)
            {
                Point_3D = point_3d;
            }
        }

        [Fact]
        public static void At_Prefix_DefaultValue()
        {
            ClassWrapper_For_Int_String obj = JsonSerializer.Deserialize<ClassWrapper_For_Int_String>(@"{""Int"":1,""String"":""1""}");
            Assert.Equal(1, obj.Int);
            Assert.Equal("1", obj.String);
        }

        public class ClassWrapper_For_Int_String
        {
            public int Int { get; }

            public string String { get; }

            public ClassWrapper_For_Int_String(int @int, string @string) // Parameter names are "int" and "string"
            {
                Int = @int;
                String = @string;
            }
        }

        [Fact]
        public static void At_Prefix_NoMatch_UseDefaultValues()
        {
            ClassWrapper_For_Int_String obj = JsonSerializer.Deserialize<ClassWrapper_For_Int_String>(@"{""@Int"":1,""@String"":""1""}");
            Assert.Equal(0, obj.Int);
            Assert.Null(obj.String);
        }

        [Fact]
        public static void PassDefaultValueToComplexStruct()
        {
            ClassWrapperForPoint_3D obj = JsonSerializer.Deserialize<ClassWrapperForPoint_3D>(@"{}");
            Assert.True(obj.Point_3D == default);

            ClassWrapper_For_Int_Point_3D_String obj1 = JsonSerializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{}");
            Assert.Equal(0, obj1.MyInt);
            Assert.Equal(0, obj1.MyPoint3DStruct.X);
            Assert.Equal(0, obj1.MyPoint3DStruct.Y);
            Assert.Equal(0, obj1.MyPoint3DStruct.Z);
            Assert.Null(obj1.MyString);
        }

        public class ClassWrapper_For_Int_Point_3D_String
        {
            public int MyInt { get; }

            public Point_3D_Struct MyPoint3DStruct { get; }

            public string MyString { get; }

            public ClassWrapper_For_Int_Point_3D_String(Point_3D_Struct myPoint3dStruct)
            {
                MyInt = 0;
                MyPoint3DStruct = myPoint3dStruct;
                MyString = null;
            }

            [JsonConstructor]
            public ClassWrapper_For_Int_Point_3D_String(int myInt, Point_3D_Struct myPoint3dStruct, string myString)
            {
                MyInt = myInt;
                MyPoint3DStruct = myPoint3dStruct;
                MyString = myString;
            }
        }

        public struct Point_3D_Struct
        {
            public int X { get; }

            public int Y { get; }

            public int Z { get; }

            [JsonConstructor]
            public Point_3D_Struct(int x, int y, int z = 50) => (X, Y, Z) = (x, y, z);
        }

        [Fact]
        public static void Null_AsArgument_To_ParameterThat_CanBeNull()
        {
            ClassWrapper_For_Int_Point_3D_String obj1 = JsonSerializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{""MyInt"":1,""MyPoint3dStruct"":{},""MyString"":null}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Equal(0, obj1.MyPoint3DStruct.X);
            Assert.Equal(0, obj1.MyPoint3DStruct.Y);
            Assert.Equal(50, obj1.MyPoint3DStruct.Z);
            Assert.Null(obj1.MyString);
        }

        [Fact]
        public static void Null_AsArgument_To_ParameterThat_CanNotBeNull()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{""MyInt"":null,""MyString"":""1""}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWrapper_For_Int_Point_3D_String>(@"{""MyPoint3DStruct"":null,""MyString"":""1""}"));
        }

        [Theory]
        [InlineData(typeof(Person_Class))]
        [InlineData(typeof(Person_Struct))]
        public static void OtherPropertiesAreSet(Type type)
        {
            string json = @"{
                ""FirstName"":""John"",
                ""LastName"":""Doe"",
                ""EmailAddress"":""johndoe@live.com"",
                ""Id"":""f2c92fcc-459f-4287-90b6-a7cbd82aeb0e"",
                ""Age"":24,
                ""Point2D"":{""x"":1,""y"":2},
                ""ReadOnlyPoint2D"":{""x"":1,""y"":2},
                ""Point2DWithExtDataClass"":{""x"":1,""y"":2,""b"":3},
                ""ReadOnlyPoint2DWithExtDataClass"":{""x"":1,""y"":2,""b"":3},
                ""Point3DStruct"":{""x"":1,""y"":2,""z"":3},
                ""ReadOnlyPoint3DStruct"":{""x"":1,""y"":2,""z"":3},
                ""Point2DWithExtData"":{""x"":1,""y"":2,""b"":3},
                ""ReadOnlyPoint2DWithExtData"":{""x"":1,""y"":2,""b"":3}
                }";

            object person = JsonSerializer.Deserialize(json, type);

            Assert.Equal("John", (string)type.GetProperty("FirstName").GetValue(person)!);
            Assert.Equal("Doe", (string)type.GetProperty("LastName").GetValue(person)!);
            Assert.Equal("johndoe@live.com", (string)type.GetProperty("EmailAddress").GetValue(person)!);
            Assert.Equal("f2c92fcc-459f-4287-90b6-a7cbd82aeb0e", ((Guid)type.GetProperty("Id").GetValue(person)!).ToString());
            Assert.Equal(24, (int)type.GetProperty("Age").GetValue(person)!);

            string serialized = JsonSerializer.Serialize(person);
            Assert.Contains(@"""Point2D"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint2D"":{", serialized);
            Assert.Contains(@"""Point2DWithExtDataClass"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint2DWithExtDataClass"":{", serialized);
            Assert.Contains(@"""Point3DStruct"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint3DStruct"":{", serialized);
            Assert.Contains(@"""Point2DWithExtData"":{", serialized);
            Assert.Contains(@"""ReadOnlyPoint2DWithExtData"":{", serialized);

            Point_2D point2D = (Point_2D)type.GetProperty("Point2D").GetValue(person)!;
            serialized = JsonSerializer.Serialize(point2D);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);

            Point_2D readOnlyPoint2D = (Point_2D)type.GetProperty("ReadOnlyPoint2D").GetValue(person)!;
            serialized = JsonSerializer.Serialize(readOnlyPoint2D);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);

            Point_2D_With_ExtData_Class point2DWithExtDataClass = (Point_2D_With_ExtData_Class)type.GetProperty("Point2DWithExtDataClass").GetValue(person)!;
            serialized = JsonSerializer.Serialize(point2DWithExtDataClass);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);

            Point_2D_With_ExtData_Class readOnlyPoint2DWithExtDataClass = (Point_2D_With_ExtData_Class)type.GetProperty("ReadOnlyPoint2DWithExtDataClass").GetValue(person)!;
            serialized = JsonSerializer.Serialize(readOnlyPoint2DWithExtDataClass);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);

            Point_3D_Struct point3DStruct = (Point_3D_Struct)type.GetProperty("Point3DStruct").GetValue(person)!;
            serialized = JsonSerializer.Serialize(point3DStruct);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""Z"":3", serialized);

            Point_3D_Struct readOnlyPoint3DStruct = (Point_3D_Struct)type.GetProperty("ReadOnlyPoint3DStruct").GetValue(person)!;
            serialized = JsonSerializer.Serialize(readOnlyPoint3DStruct);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""Z"":3", serialized);

            Point_2D_With_ExtData point2DWithExtData = (Point_2D_With_ExtData)type.GetProperty("Point2DWithExtData").GetValue(person)!;
            serialized = JsonSerializer.Serialize(point2DWithExtData);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);

            Point_2D_With_ExtData readOnlyPoint2DWithExtData = (Point_2D_With_ExtData)type.GetProperty("ReadOnlyPoint2DWithExtData").GetValue(person)!;
            serialized = JsonSerializer.Serialize(readOnlyPoint2DWithExtData);
            Assert.Contains(@"""X"":1", serialized);
            Assert.Contains(@"""Y"":2", serialized);
            Assert.Contains(@"""b"":3", serialized);
        }

        private class Person_Class
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string EmailAddress { get; }
            public Guid Id { get; }
            public int Age { get; }

            public Point_2D Point2D { get; set; }
            public Point_2D ReadOnlyPoint2D { get; }

            public Point_2D_With_ExtData_Class Point2DWithExtDataClass { get; set; }
            public Point_2D_With_ExtData_Class ReadOnlyPoint2DWithExtDataClass { get; }

            public Point_3D_Struct Point3DStruct { get; set; }
            public Point_3D_Struct ReadOnlyPoint3DStruct { get; }

            public Point_2D_With_ExtData Point2DWithExtData { get; set; }
            public Point_2D_With_ExtData ReadOnlyPoint2DWithExtData { get; }

            // Test that objects deserialized with parameterless still work fine as properties
            public SinglePublicParameterizedCtor SinglePublicParameterizedCtor { get; set; }
            public SinglePublicParameterizedCtor ReadOnlySinglePublicParameterizedCtor { get; }

            public Person_Class(
                string emailAddress,
                Guid id,
                int age,
                Point_2D readOnlyPoint2D,
                Point_2D_With_ExtData_Class readOnlyPoint2DWithExtDataClass,
                Point_3D_Struct readOnlyPoint3DStruct,
                Point_2D_With_ExtData readOnlyPoint2DWithExtData,
                SinglePublicParameterizedCtor readOnlySinglePublicParameterizedCtor)
            {
                EmailAddress = emailAddress;
                Id = id;
                Age = age;
                ReadOnlyPoint2D = readOnlyPoint2D;
                ReadOnlyPoint2DWithExtDataClass = readOnlyPoint2DWithExtDataClass;
                ReadOnlyPoint3DStruct = readOnlyPoint3DStruct;
                ReadOnlyPoint2DWithExtData = readOnlyPoint2DWithExtData;
                ReadOnlySinglePublicParameterizedCtor = readOnlySinglePublicParameterizedCtor;
            }
        }

        private struct Person_Struct
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string EmailAddress { get; }
            public Guid Id { get; }
            public int Age { get; }

            public Point_2D Point2D { get; set; }
            public Point_2D ReadOnlyPoint2D { get; }

            public Point_2D_With_ExtData_Class Point2DWithExtDataClass { get; set; }
            public Point_2D_With_ExtData_Class ReadOnlyPoint2DWithExtDataClass { get; }

            public Point_3D_Struct Point3DStruct { get; set; }
            public Point_3D_Struct ReadOnlyPoint3DStruct { get; }

            public Point_2D_With_ExtData Point2DWithExtData { get; set; }
            public Point_2D_With_ExtData ReadOnlyPoint2DWithExtData { get; }

            // Test that objects deserialized with parameterless still work fine as properties
            public SinglePublicParameterizedCtor SinglePublicParameterizedCtor { get; set; }
            public SinglePublicParameterizedCtor ReadOnlySinglePublicParameterizedCtor { get; }

            public Person_Struct(
                string emailAddress,
                Guid id,
                int age,
                Point_2D readOnlyPoint2D,
                Point_2D_With_ExtData_Class readOnlyPoint2DWithExtDataClass,
                Point_3D_Struct readOnlyPoint3DStruct,
                Point_2D_With_ExtData readOnlyPoint2DWithExtData,
                SinglePublicParameterizedCtor readOnlySinglePublicParameterizedCtor)
            {
                // Readonly, setting in ctor.
                EmailAddress = emailAddress;
                Id = id;
                Age = age;
                ReadOnlyPoint2D = readOnlyPoint2D;
                ReadOnlyPoint2DWithExtDataClass = readOnlyPoint2DWithExtDataClass;
                ReadOnlyPoint3DStruct = readOnlyPoint3DStruct;
                ReadOnlyPoint2DWithExtData = readOnlyPoint2DWithExtData;
                ReadOnlySinglePublicParameterizedCtor = readOnlySinglePublicParameterizedCtor;

                // These properties will be set by serializer.
                FirstName = null;
                LastName = null;
                Point2D = null;
                Point2DWithExtDataClass = null;
                Point3DStruct = default;
                Point2DWithExtData = default;
                SinglePublicParameterizedCtor = default;
            }
        }

        private class Point_2D_With_ExtData_Class
        {
            public int X { get; }

            public int Y { get; }

            [JsonConstructor]
            public Point_2D_With_ExtData_Class(int x, int y)
            {
                X = x;
                Y = y;
            }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }

        [Fact]
        public static void ExtraProperties_AreIgnored()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{ ""x"":1,""y"":2,""b"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public static void ExtraProperties_GoInExtensionData_IfPresent()
        {
            Point_2D_With_ExtData point = JsonSerializer.Deserialize<Point_2D_With_ExtData>(@"{""x"":1,""y"":2,""b"":3}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(3, point.ExtensionData["b"].GetInt32());
        }

        [Fact]
        public static void PropertiesNotSet_WhenJSON_MapsToConstructorParameters()
        {
            var obj = JsonSerializer.Deserialize<Point_PropertiesHavePropertyNames>(@"{""A"":1,""b"":2}");
            Assert.Equal(40, obj.X); // Would be 1 if property were set directly after object construction.
            Assert.Equal(60, obj.Y); // Would be 2 if property were set directly after object construction.
            Assert.Equal(@"{""A"":40,""b"":60}", JsonSerializer.Serialize(obj));

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new PointPropertyNamingPolicy()
            };

            var obj2 = JsonSerializer.Deserialize<Point_2D_Mutable>(@"{""A"":1,""b"":2}", options);
            Assert.Equal(40, obj2.X); // Would be 1 if property were set directly after object construction.
            Assert.Equal(60, obj2.Y); // Would be 2 if property were set directly after object construction.
            Assert.Equal(@"{""A"":40,""b"":60}", JsonSerializer.Serialize(obj));
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
            public Point_2D_Mutable(int a, int b)
            {
                X = 40;
                Y = 60;
            }
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

                return name;
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
        public static void IgnoreNullValues_DontSetNull_ToConstructorArguments_ThatCantBeNull()
        {
            // Default is to throw JsonException when null applied to types that can't be null.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NullArgTester>(@"{""Point3DStruct"":null,""Int"":null,""ImmutableArray"":null}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NullArgTester>(@"{""Point3DStruct"":null}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NullArgTester>(@"{""Int"":null}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NullArgTester>(@"{""ImmutableArray"":null}"));

            // Set arguments to default values when IgnoreNullValues is on.
            var options = new JsonSerializerOptions { IgnoreNullValues = true };
            var obj = JsonSerializer.Deserialize<NullArgTester>(@"{""Int"":null,""Point3DStruct"":null,""ImmutableArray"":null}", options);
            Assert.Equal(0, obj.Point3DStruct.X);
            Assert.Equal(0, obj.Point3DStruct.Y);
            Assert.Equal(0, obj.Point3DStruct.Z);
            Assert.True(obj.ImmutableArray.IsDefault);
            Assert.Equal(50, obj.Int);
        }

        public class NullArgTester
        {
            public Point_3D_Struct Point3DStruct { get; }
            public ImmutableArray<int> ImmutableArray { get; }
            public int Int { get; }

            public NullArgTester(Point_3D_Struct point3DStruct, ImmutableArray<int> immutableArray, int @int = 50)
            {
                Point3DStruct = point3DStruct;
                ImmutableArray = immutableArray;
                Int = @int;
            }
        }

        [Fact]
        public static void NumerousSimpleAndComplexParameters()
        {
            //for (int i = 0; i < 1000; i++)
            //{
            //    JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(ClassWithConstructor_SimpleAndComplexParameters.s_json);
            //}

            //for (int i = 0; i < 1000; i++)
            //{
            //    JsonSerializer.Deserialize<Class_SimpleAndComplexParameters>(Class_SimpleAndComplexParameters.s_json);
            //}

            //for (int i = 0; i < 100_000; i++)
            //{
            //    object array = new object[2];
            //}

            var obj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(ClassWithConstructor_SimpleAndComplexParameters.s_json);
            obj.Verify();

            obj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(JsonSerializer.Serialize(obj));
            obj.Verify();
        }

        [Fact]
        public static void UseNamingPolicy_ToMatch()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
            };

            string json = JsonSerializer.Serialize(new NamingPolicyTester(33, 35), options);

            // If we don't use naming policy, then we can't match serialized properties to constructor parameters on deserialization.
            var obj = JsonSerializer.Deserialize<NamingPolicyTester>(json);
            Assert.Equal(0, obj.FirstProperty);
            Assert.Equal(0, obj.SecondProperty);

            obj = JsonSerializer.Deserialize<NamingPolicyTester>(json, options);
            Assert.Equal(33, obj.FirstProperty);
            Assert.Equal(35, obj.SecondProperty);
        }

        private class NamingPolicyTester
        {
            public int FirstProperty { get; }
            public int SecondProperty { get; }

            public NamingPolicyTester(int firstProperty, int secondProperty)
            {
                FirstProperty = firstProperty;
                SecondProperty = secondProperty;
            }
        }

        public sealed class JsonSnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return name;

                // Allocates a string builder with the guessed result length,
                // where 5 is the average word length in English, and
                // max(2, length / 5) is the number of underscores.
                StringBuilder builder = new StringBuilder(name.Length + Math.Max(2, name.Length / 5));
                UnicodeCategory? previousCategory = null;

                for (int currentIndex = 0; currentIndex < name.Length; currentIndex++)
                {
                    char currentChar = name[currentIndex];
                    if (currentChar == '_')
                    {
                        builder.Append('_');
                        previousCategory = null;
                        continue;
                    }

                    UnicodeCategory currentCategory = char.GetUnicodeCategory(currentChar);

                    switch (currentCategory)
                    {
                        case UnicodeCategory.UppercaseLetter:
                        case UnicodeCategory.TitlecaseLetter:
                            if (previousCategory == UnicodeCategory.SpaceSeparator ||
                                previousCategory == UnicodeCategory.LowercaseLetter ||
                                previousCategory != UnicodeCategory.DecimalDigitNumber &&
                                currentIndex > 0 &&
                                currentIndex + 1 < name.Length &&
                                char.IsLower(name[currentIndex + 1]))
                            {
                                builder.Append('_');
                            }

                            currentChar = char.ToLower(currentChar);
                            break;

                        case UnicodeCategory.LowercaseLetter:
                        case UnicodeCategory.DecimalDigitNumber:
                            if (previousCategory == UnicodeCategory.SpaceSeparator)
                            {
                                builder.Append('_');
                            }
                            break;

                        case UnicodeCategory.Surrogate:
                            break;

                        default:
                            if (previousCategory != null)
                            {
                                previousCategory = UnicodeCategory.SpaceSeparator;
                            }
                            continue;
                    }

                    builder.Append(currentChar);
                    previousCategory = currentCategory;
                }

                return builder.ToString();
            }
        }

        [Fact]
        public static void UseNamingPolicy_InvalidPolicyFails()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new NullNamingPolicy()
            };

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<NamingPolicyTester>("{}", options));
        }

        private class Class_SimpleAndComplexParameters
        {
            public byte MyByte { get; set; }
            public sbyte MySByte { get; set; }
            public char MyChar { get; set; }
            public string MyString { get; set; }
            public decimal MyDecimal { get; set; }
            public bool MyBooleanTrue { get; set; }
            public bool MyBooleanFalse { get; set; }
            public float MySingle { get; set; }
            public double MyDouble { get; set; }
            public DateTime MyDateTime { get; set; }
            public DateTimeOffset MyDateTimeOffset { get; set; }
            public Guid MyGuid { get; set; }
            public Uri MyUri { get; set; }
            public SampleEnum MyEnum { get; set; }
            public SampleEnumInt64 MyInt64Enum { get; set; }
            public SampleEnumUInt64 MyUInt64Enum { get; set; }
            public SimpleStruct MySimpleStruct { get; set; }
            public SimpleTestStruct MySimpleTestStruct { get; set; }
            public int[][][] MyInt16ThreeDimensionArray { get; set; }
            public List<List<List<int>>> MyInt16ThreeDimensionList { get; set; }
            public List<string> MyStringList { get; set; }
            public IEnumerable MyStringIEnumerable { get; set; }
            public IList MyStringIList { get; set; }
            public ICollection MyStringICollection { get; set; }
            public IEnumerable<string> MyStringIEnumerableT { get; set; }


            public IReadOnlyList<string> MyStringIReadOnlyListT { get; set; }
            public ISet<string> MyStringISetT { get; set; }
            public KeyValuePair<string, string> MyStringToStringKeyValuePair { get; set; }
            public IDictionary MyStringToStringIDict { get; set; }
            public Dictionary<string, string> MyStringToStringGenericDict { get; set; }
            public IDictionary<string, string> MyStringToStringGenericIDict { get; set; }

            public IImmutableDictionary<string, string> MyStringToStringIImmutableDict { get; set; }
            public ImmutableQueue<string> MyStringImmutablQueueT { get; set; }
            public ImmutableSortedSet<string> MyStringImmutableSortedSetT { get; set; }
            public List<string> MyListOfNullString { get; set; }

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
                @"""MySimpleStruct"" : {""One"" : 11, ""Two"" : 1.9999, ""Three"" : 33}," +
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
        }

        private class ClassWithConstructor_SimpleAndComplexParameters : ITestClass
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
            public List<List<List<int>>> MyInt16ThreeDimensionList { get; }
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

            public ClassWithConstructor_SimpleAndComplexParameters(
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
                @"""MySimpleStruct"" : {""One"" : 11, ""Two"" : 1.9999, ""Three"" : 33}," +
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
                DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
                Assert.Equal("key", entry.Key);
                Assert.Equal("value", ((JsonElement)entry.Value).GetString());

                Assert.Equal("value", MyStringToStringGenericDict["key"]);
                Assert.Equal("value", MyStringToStringGenericIDict["key"]);
                Assert.Equal("value", MyStringToStringIImmutableDict["key"]);

                Assert.Equal("Hello", MyStringImmutablQueueT.First());
                Assert.Equal("Hello", MyStringImmutableSortedSetT.First());

                Assert.Null(MyListOfNullString[0]);
            }
        }

        [Fact]
        public static void TupleDeserializationWorks()
        {
            var tuple = JsonSerializer.Deserialize<Tuple<string, double>>(@"{""Item1"":""New York"",""Item2"":32.68}");
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);

            var tupleWrapper = JsonSerializer.Deserialize<TupleWrapper>(@"{""Tuple"":{""Item1"":""New York"",""Item2"":32.68}}");
            tuple = tupleWrapper.Tuple;
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);

            var tupleList = JsonSerializer.Deserialize<List<Tuple<string, double>>>(@"[{""Item1"":""New York"",""Item2"":32.68}]");
            tuple = tupleList[0];
            Assert.Equal("New York", tuple.Item1);
            Assert.Equal(32.68, tuple.Item2);
        }

        [Fact]
        public static void TupleDeserialization_MoreThanSevenItems()
        {
            // Seven is okay
            string json = JsonSerializer.Serialize(Tuple.Create(1, 2, 3, 4, 5, 6, 7));
            var obj = JsonSerializer.Deserialize<Tuple<int, int, int, int, int, int, int>>(json);
            Assert.Equal(json, JsonSerializer.Serialize(obj));

            // More than seven arguments needs special casing and can be revisted.
            // Newtonsoft.Json fails in the same way.
            json = JsonSerializer.Serialize(Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Tuple<int, int, int, int, int, int, int, int>>(json));

            // Invalid JSON representing a tuple with more than seven items yields an ArgumentException from the constructor.
            // System.ArgumentException : The last element of an eight element tuple must be a Tuple.
            // We pass the number 8, not a new Tuple<int>(8).
            // Fixing this needs special casing. Newtonsoft behaves the same way.
            string invalidJson = @"{""Item1"":1,""Item2"":2,""Item3"":3,""Item4"":4,""Item5"":5,""Item6"":6,""Item7"":7,""Item1"":8}";
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Tuple<int, int, int, int, int, int, int, int>>(invalidJson));
        }

        [Fact]
        public static void TupleDeserialization_DefaultValuesUsed_WhenJsonMissing()
        {
            // Seven items; only three provided.
            string input = @"{""Item2"":""2"",""Item3"":3,""Item6"":6}";
            var obj = JsonSerializer.Deserialize<Tuple<int, string, int, string, string, int, Point_3D_Struct>>(input);

            string serialized = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""Item1"":0", serialized);
            Assert.Contains(@"""Item2"":""2""", serialized);
            Assert.Contains(@"""Item3"":3", serialized);
            Assert.Contains(@"""Item4"":null", serialized);
            Assert.Contains(@"""Item5"":null", serialized);
            Assert.Contains(@"""Item6"":6", serialized);
            Assert.Contains(@"""Item7"":{", serialized);

            serialized = JsonSerializer.Serialize(obj.Item7);
            Assert.Contains(@"""X"":0", serialized);
            Assert.Contains(@"""Y"":0", serialized);
            Assert.Contains(@"""Z"":0", serialized);

            // Although no Json is provided for the 8th item, ArgumentException is still thrown as we use default(int) as the argument/
            // System.ArgumentException : The last element of an eight element tuple must be a Tuple.
            // We pass the number 8, not a new Tuple<int>(default(int)).
            // Fixing this needs special casing. Newtonsoft behaves the same way.
            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Tuple<int, string, int, string, string, int, Point_3D_Struct, int>>(input));
        }

        private class TupleWrapper
        {
            public Tuple<string, double> Tuple { get; set; }
        }

        [Fact]
        public static void TupleDeserializationWorks_ClassWithParameterizedCtor()
        {
            string classJson = ClassWithConstructor_SimpleAndComplexParameters.s_json;

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 6; i++)
            {
                sb.Append(@$"""Item{i + 1}"":{classJson},");
            }
            sb.Append(@$"""Item7"":{classJson}");
            sb.Append("}");

            string complexTupleJson = sb.ToString();

            var complexTuple = JsonSerializer.Deserialize<Tuple<
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters,
                ClassWithConstructor_SimpleAndComplexParameters>>(complexTupleJson);

            complexTuple.Item1.Verify();
            complexTuple.Item2.Verify();
            complexTuple.Item3.Verify();
            complexTuple.Item4.Verify();
            complexTuple.Item5.Verify();
            complexTuple.Item6.Verify();
            complexTuple.Item7.Verify();
        }

        [Fact]
        public static void TupleDeserializationWorks_ClassWithParameterlessCtor()
        {
            string classJson = SimpleTestClass.s_json;

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 6; i++)
            {
                sb.Append(@$"""Item{i + 1}"":{classJson},");
            }
            sb.Append(@$"""Item7"":{classJson}");
            sb.Append("}");

            string complexTupleJson = sb.ToString();

            var complexTuple = JsonSerializer.Deserialize<Tuple<
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass,
                SimpleTestClass>>(complexTupleJson);

            complexTuple.Item1.Verify();
            complexTuple.Item2.Verify();
            complexTuple.Item3.Verify();
            complexTuple.Item4.Verify();
            complexTuple.Item5.Verify();
            complexTuple.Item6.Verify();
            complexTuple.Item7.Verify();
        }

        [Fact]
        public static void NoConstructorHandlingWhenObjectHasConverter()
        {
            // Baseline without converter
            string serialized = JsonSerializer.Serialize(new Point_3D(10, 6));

            Point_3D point = JsonSerializer.Deserialize<Point_3D>(serialized);
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            serialized = JsonSerializer.Serialize(new[] { new Point_3D(10, 6) });

            point = JsonSerializer.Deserialize<Point_3D[]>(serialized)[0];
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            serialized = JsonSerializer.Serialize(new WrapperForPoint_3D { Point_3D = new Point_3D(10, 6) });

            point = JsonSerializer.Deserialize<WrapperForPoint_3D>(serialized).Point_3D;
            Assert.Equal(10, point.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(50, point.Z);

            // Converters for objects with parameterized ctors are honored

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterForPoint3D());

            serialized = JsonSerializer.Serialize(new Point_3D(10, 6));

            point = JsonSerializer.Deserialize<Point_3D>(serialized, options);
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);

            serialized = JsonSerializer.Serialize(new[] { new Point_3D(10, 6) });

            point = JsonSerializer.Deserialize<Point_3D[]>(serialized, options)[0];
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);

            serialized = JsonSerializer.Serialize(new WrapperForPoint_3D { Point_3D = new Point_3D(10, 6) });

            point = JsonSerializer.Deserialize<WrapperForPoint_3D>(serialized, options).Point_3D;
            Assert.Equal(4, point.X);
            Assert.Equal(4, point.Y);
            Assert.Equal(4, point.Z);
        }

        private class ConverterForPoint3D : JsonConverter<Point_3D>
        {
            public override Point_3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                while (reader.TokenType != JsonTokenType.EndObject)
                {
                    reader.Read();
                }

                return new Point_3D(4, 4, 4);
            }

            public override void Write(Utf8JsonWriter writer, Point_3D value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public static void ConstructorHandlingHonorsCustomConverters()
        {
            // Baseline, use internal converters for primitives
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":2,""y"":3}");
            Assert.Equal(2, point.X);
            Assert.Equal(3, point.Y);

            // Honor custom converters
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterForInt32());

            point = JsonSerializer.Deserialize<Point_2D>(@"{""x"":2,""y"":3}", options);
            Assert.Equal(25, point.X);
            Assert.Equal(25, point.X);
        }

        private class ConverterForInt32 : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return 25;
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [Theory]
        [InlineData(typeof(Struct_With_Ctor_With_64_Params))]
        [InlineData(typeof(Class_With_Ctor_With_64_Params))]
        public static void CanDeserialize_ObjectWith_Ctor_With_64_Params(Type type)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 63;  i++)
            {
                sb.Append($@"""Int{i}"":{i},");
            }
            sb.Append($@"""Int63"":63");
            sb.Append("}");

            string input = sb.ToString();

            object obj = JsonSerializer.Deserialize(input, type);
            for (int i = 0; i < 64; i++)
            {
                Assert.Equal(i, (int)type.GetProperty($"Int{i}").GetValue(obj));
            }
        }

        public class Class_With_Ctor_With_64_Params
        {
            public int Int0 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
            public int Int3 { get; }
            public int Int4 { get; }
            public int Int5 { get; }
            public int Int6 { get; }
            public int Int7 { get; }
            public int Int8 { get; }
            public int Int9 { get; }
            public int Int10 { get; }
            public int Int11 { get; }
            public int Int12 { get; }
            public int Int13 { get; }
            public int Int14 { get; }
            public int Int15 { get; }
            public int Int16 { get; }
            public int Int17 { get; }
            public int Int18 { get; }
            public int Int19 { get; }
            public int Int20 { get; }
            public int Int21 { get; }
            public int Int22 { get; }
            public int Int23 { get; }
            public int Int24 { get; }
            public int Int25 { get; }
            public int Int26 { get; }
            public int Int27 { get; }
            public int Int28 { get; }
            public int Int29 { get; }
            public int Int30 { get; }
            public int Int31 { get; }
            public int Int32 { get; }
            public int Int33 { get; }
            public int Int34 { get; }
            public int Int35 { get; }
            public int Int36 { get; }
            public int Int37 { get; }
            public int Int38 { get; }
            public int Int39 { get; }
            public int Int40 { get; }
            public int Int41 { get; }
            public int Int42 { get; }
            public int Int43 { get; }
            public int Int44 { get; }
            public int Int45 { get; }
            public int Int46 { get; }
            public int Int47 { get; }
            public int Int48 { get; }
            public int Int49 { get; }
            public int Int50 { get; }
            public int Int51 { get; }
            public int Int52 { get; }
            public int Int53 { get; }
            public int Int54 { get; }
            public int Int55 { get; }
            public int Int56 { get; }
            public int Int57 { get; }
            public int Int58 { get; }
            public int Int59 { get; }
            public int Int60 { get; }
            public int Int61 { get; }
            public int Int62 { get; }
            public int Int63 { get; }

            public Class_With_Ctor_With_64_Params(int int0, int int1, int int2, int int3, int int4, int int5, int int6, int int7,
                                                 int int8, int int9, int int10, int int11, int int12, int int13, int int14, int int15,
                                                 int int16, int int17, int int18, int int19, int int20, int int21, int int22, int int23,
                                                 int int24, int int25, int int26, int int27, int int28, int int29, int int30, int int31,
                                                 int int32, int int33, int int34, int int35, int int36, int int37, int int38, int int39,
                                                 int int40, int int41, int int42, int int43, int int44, int int45, int int46, int int47,
                                                 int int48, int int49, int int50, int int51, int int52, int int53, int int54, int int55,
                                                 int int56, int int57, int int58, int int59, int int60, int int61, int int62, int int63)
            {
                Int0 = int0; Int1 = int1; Int2 = int2; Int3 = int3; Int4 = int4; Int5 = int5; Int6 = int6; Int7 = int7;
                Int8 = int8; Int9 = int9; Int10 = int10; Int11 = int11; Int12 = int12; Int13 = int13; Int14 = int14; Int15 = int15;
                Int16 = int16; Int17 = int17; Int18 = int18; Int19 = int19; Int20 = int20; Int21 = int21; Int22 = int22; Int23 = int23;
                Int24 = int24; Int25 = int25; Int26 = int26; Int27 = int27; Int28 = int28; Int29 = int29; Int30 = int30; Int31 = int31;
                Int32 = int32; Int33 = int33; Int34 = int34; Int35 = int35; Int36 = int36; Int37 = int37; Int38 = int38; Int39 = int39;
                Int40 = int40; Int41 = int41; Int42 = int42; Int43 = int43; Int44 = int44; Int45 = int45; Int46 = int46; Int47 = int47;
                Int48 = int48; Int49 = int49; Int50 = int50; Int51 = int51; Int52 = int52; Int53 = int53; Int54 = int54; Int55 = int55;
                Int56 = int56; Int57 = int57; Int58 = int58; Int59 = int59; Int60 = int60; Int61 = int61; Int62 = int62; Int63 = int63;
            }
        }

        public struct Struct_With_Ctor_With_64_Params
        {
            public int Int0 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
            public int Int3 { get; }
            public int Int4 { get; }
            public int Int5 { get; }
            public int Int6 { get; }
            public int Int7 { get; }
            public int Int8 { get; }
            public int Int9 { get; }
            public int Int10 { get; }
            public int Int11 { get; }
            public int Int12 { get; }
            public int Int13 { get; }
            public int Int14 { get; }
            public int Int15 { get; }
            public int Int16 { get; }
            public int Int17 { get; }
            public int Int18 { get; }
            public int Int19 { get; }
            public int Int20 { get; }
            public int Int21 { get; }
            public int Int22 { get; }
            public int Int23 { get; }
            public int Int24 { get; }
            public int Int25 { get; }
            public int Int26 { get; }
            public int Int27 { get; }
            public int Int28 { get; }
            public int Int29 { get; }
            public int Int30 { get; }
            public int Int31 { get; }
            public int Int32 { get; }
            public int Int33 { get; }
            public int Int34 { get; }
            public int Int35 { get; }
            public int Int36 { get; }
            public int Int37 { get; }
            public int Int38 { get; }
            public int Int39 { get; }
            public int Int40 { get; }
            public int Int41 { get; }
            public int Int42 { get; }
            public int Int43 { get; }
            public int Int44 { get; }
            public int Int45 { get; }
            public int Int46 { get; }
            public int Int47 { get; }
            public int Int48 { get; }
            public int Int49 { get; }
            public int Int50 { get; }
            public int Int51 { get; }
            public int Int52 { get; }
            public int Int53 { get; }
            public int Int54 { get; }
            public int Int55 { get; }
            public int Int56 { get; }
            public int Int57 { get; }
            public int Int58 { get; }
            public int Int59 { get; }
            public int Int60 { get; }
            public int Int61 { get; }
            public int Int62 { get; }
            public int Int63 { get; }

            public Struct_With_Ctor_With_64_Params(int int0, int int1, int int2, int int3, int int4, int int5, int int6, int int7,
                                                 int int8, int int9, int int10, int int11, int int12, int int13, int int14, int int15,
                                                 int int16, int int17, int int18, int int19, int int20, int int21, int int22, int int23,
                                                 int int24, int int25, int int26, int int27, int int28, int int29, int int30, int int31,
                                                 int int32, int int33, int int34, int int35, int int36, int int37, int int38, int int39,
                                                 int int40, int int41, int int42, int int43, int int44, int int45, int int46, int int47,
                                                 int int48, int int49, int int50, int int51, int int52, int int53, int int54, int int55,
                                                 int int56, int int57, int int58, int int59, int int60, int int61, int int62, int int63)
            {
                Int0 = int0; Int1 = int1; Int2 = int2; Int3 = int3; Int4 = int4; Int5 = int5; Int6 = int6; Int7 = int7;
                Int8 = int8; Int9 = int9; Int10 = int10; Int11 = int11; Int12 = int12; Int13 = int13; Int14 = int14; Int15 = int15;
                Int16 = int16; Int17 = int17; Int18 = int18; Int19 = int19; Int20 = int20; Int21 = int21; Int22 = int22; Int23 = int23;
                Int24 = int24; Int25 = int25; Int26 = int26; Int27 = int27; Int28 = int28; Int29 = int29; Int30 = int30; Int31 = int31;
                Int32 = int32; Int33 = int33; Int34 = int34; Int35 = int35; Int36 = int36; Int37 = int37; Int38 = int38; Int39 = int39;
                Int40 = int40; Int41 = int41; Int42 = int42; Int43 = int43; Int44 = int44; Int45 = int45; Int46 = int46; Int47 = int47;
                Int48 = int48; Int49 = int49; Int50 = int50; Int51 = int51; Int52 = int52; Int53 = int53; Int54 = int54; Int55 = int55;
                Int56 = int56; Int57 = int57; Int58 = int58; Int59 = int59; Int60 = int60; Int61 = int61; Int62 = int62; Int63 = int63;
            }
        }

        [Theory]
        [InlineData(typeof(Class_With_Ctor_With_65_Params))]
        [InlineData(typeof(Struct_With_Ctor_With_65_Params))]
        public static void Cannot_Deserialize_ObjectWith_Ctor_With_65_Params(Type type)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 64; i++)
            {
                sb.Append($@"""Int{i}"":{i},");
            }
            sb.Append($@"""Int64"":64");
            sb.Append("}");

            string input = sb.ToString();

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize(input, type));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize("{}", type));
        }

        public class Class_With_Ctor_With_65_Params
        {
            public int Int0 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
            public int Int3 { get; }
            public int Int4 { get; }
            public int Int5 { get; }
            public int Int6 { get; }
            public int Int7 { get; }
            public int Int8 { get; }
            public int Int9 { get; }
            public int Int10 { get; }
            public int Int11 { get; }
            public int Int12 { get; }
            public int Int13 { get; }
            public int Int14 { get; }
            public int Int15 { get; }
            public int Int16 { get; }
            public int Int17 { get; }
            public int Int18 { get; }
            public int Int19 { get; }
            public int Int20 { get; }
            public int Int21 { get; }
            public int Int22 { get; }
            public int Int23 { get; }
            public int Int24 { get; }
            public int Int25 { get; }
            public int Int26 { get; }
            public int Int27 { get; }
            public int Int28 { get; }
            public int Int29 { get; }
            public int Int30 { get; }
            public int Int31 { get; }
            public int Int32 { get; }
            public int Int33 { get; }
            public int Int34 { get; }
            public int Int35 { get; }
            public int Int36 { get; }
            public int Int37 { get; }
            public int Int38 { get; }
            public int Int39 { get; }
            public int Int40 { get; }
            public int Int41 { get; }
            public int Int42 { get; }
            public int Int43 { get; }
            public int Int44 { get; }
            public int Int45 { get; }
            public int Int46 { get; }
            public int Int47 { get; }
            public int Int48 { get; }
            public int Int49 { get; }
            public int Int50 { get; }
            public int Int51 { get; }
            public int Int52 { get; }
            public int Int53 { get; }
            public int Int54 { get; }
            public int Int55 { get; }
            public int Int56 { get; }
            public int Int57 { get; }
            public int Int58 { get; }
            public int Int59 { get; }
            public int Int60 { get; }
            public int Int61 { get; }
            public int Int62 { get; }
            public int Int63 { get; }
            public int Int64 { get; }

            public Class_With_Ctor_With_65_Params(int int0, int int1, int int2, int int3, int int4, int int5, int int6, int int7,
                                                 int int8, int int9, int int10, int int11, int int12, int int13, int int14, int int15,
                                                 int int16, int int17, int int18, int int19, int int20, int int21, int int22, int int23,
                                                 int int24, int int25, int int26, int int27, int int28, int int29, int int30, int int31,
                                                 int int32, int int33, int int34, int int35, int int36, int int37, int int38, int int39,
                                                 int int40, int int41, int int42, int int43, int int44, int int45, int int46, int int47,
                                                 int int48, int int49, int int50, int int51, int int52, int int53, int int54, int int55,
                                                 int int56, int int57, int int58, int int59, int int60, int int61, int int62, int int63,
                                                 int int64)
            {
                Int0 = int0; Int1 = int1; Int2 = int2; Int3 = int3; Int4 = int4; Int5 = int5; Int6 = int6; Int7 = int7;
                Int8 = int8; Int9 = int9; Int10 = int10; Int11 = int11; Int12 = int12; Int13 = int13; Int14 = int14; Int15 = int15;
                Int16 = int16; Int17 = int17; Int18 = int18; Int19 = int19; Int20 = int20; Int21 = int21; Int22 = int22; Int23 = int23;
                Int24 = int24; Int25 = int25; Int26 = int26; Int27 = int27; Int28 = int28; Int29 = int29; Int30 = int30; Int31 = int31;
                Int32 = int32; Int33 = int33; Int34 = int34; Int35 = int35; Int36 = int36; Int37 = int37; Int38 = int38; Int39 = int39;
                Int40 = int40; Int41 = int41; Int42 = int42; Int43 = int43; Int44 = int44; Int45 = int45; Int46 = int46; Int47 = int47;
                Int48 = int48; Int49 = int49; Int50 = int50; Int51 = int51; Int52 = int52; Int53 = int53; Int54 = int54; Int55 = int55;
                Int56 = int56; Int57 = int57; Int58 = int58; Int59 = int59; Int60 = int60; Int61 = int61; Int62 = int62; Int63 = int63;
                Int64 = int64;
            }
        }

        public struct Struct_With_Ctor_With_65_Params
        {
            public int Int0 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
            public int Int3 { get; }
            public int Int4 { get; }
            public int Int5 { get; }
            public int Int6 { get; }
            public int Int7 { get; }
            public int Int8 { get; }
            public int Int9 { get; }
            public int Int10 { get; }
            public int Int11 { get; }
            public int Int12 { get; }
            public int Int13 { get; }
            public int Int14 { get; }
            public int Int15 { get; }
            public int Int16 { get; }
            public int Int17 { get; }
            public int Int18 { get; }
            public int Int19 { get; }
            public int Int20 { get; }
            public int Int21 { get; }
            public int Int22 { get; }
            public int Int23 { get; }
            public int Int24 { get; }
            public int Int25 { get; }
            public int Int26 { get; }
            public int Int27 { get; }
            public int Int28 { get; }
            public int Int29 { get; }
            public int Int30 { get; }
            public int Int31 { get; }
            public int Int32 { get; }
            public int Int33 { get; }
            public int Int34 { get; }
            public int Int35 { get; }
            public int Int36 { get; }
            public int Int37 { get; }
            public int Int38 { get; }
            public int Int39 { get; }
            public int Int40 { get; }
            public int Int41 { get; }
            public int Int42 { get; }
            public int Int43 { get; }
            public int Int44 { get; }
            public int Int45 { get; }
            public int Int46 { get; }
            public int Int47 { get; }
            public int Int48 { get; }
            public int Int49 { get; }
            public int Int50 { get; }
            public int Int51 { get; }
            public int Int52 { get; }
            public int Int53 { get; }
            public int Int54 { get; }
            public int Int55 { get; }
            public int Int56 { get; }
            public int Int57 { get; }
            public int Int58 { get; }
            public int Int59 { get; }
            public int Int60 { get; }
            public int Int61 { get; }
            public int Int62 { get; }
            public int Int63 { get; }
            public int Int64 { get; }

            public Struct_With_Ctor_With_65_Params(int int0, int int1, int int2, int int3, int int4, int int5, int int6, int int7,
                                                 int int8, int int9, int int10, int int11, int int12, int int13, int int14, int int15,
                                                 int int16, int int17, int int18, int int19, int int20, int int21, int int22, int int23,
                                                 int int24, int int25, int int26, int int27, int int28, int int29, int int30, int int31,
                                                 int int32, int int33, int int34, int int35, int int36, int int37, int int38, int int39,
                                                 int int40, int int41, int int42, int int43, int int44, int int45, int int46, int int47,
                                                 int int48, int int49, int int50, int int51, int int52, int int53, int int54, int int55,
                                                 int int56, int int57, int int58, int int59, int int60, int int61, int int62, int int63,
                                                 int int64)
            {
                Int0 = int0; Int1 = int1; Int2 = int2; Int3 = int3; Int4 = int4; Int5 = int5; Int6 = int6; Int7 = int7;
                Int8 = int8; Int9 = int9; Int10 = int10; Int11 = int11; Int12 = int12; Int13 = int13; Int14 = int14; Int15 = int15;
                Int16 = int16; Int17 = int17; Int18 = int18; Int19 = int19; Int20 = int20; Int21 = int21; Int22 = int22; Int23 = int23;
                Int24 = int24; Int25 = int25; Int26 = int26; Int27 = int27; Int28 = int28; Int29 = int29; Int30 = int30; Int31 = int31;
                Int32 = int32; Int33 = int33; Int34 = int34; Int35 = int35; Int36 = int36; Int37 = int37; Int38 = int38; Int39 = int39;
                Int40 = int40; Int41 = int41; Int42 = int42; Int43 = int43; Int44 = int44; Int45 = int45; Int46 = int46; Int47 = int47;
                Int48 = int48; Int49 = int49; Int50 = int50; Int51 = int51; Int52 = int52; Int53 = int53; Int54 = int54; Int55 = int55;
                Int56 = int56; Int57 = int57; Int58 = int58; Int59 = int59; Int60 = int60; Int61 = int61; Int62 = int62; Int63 = int63;
                Int64 = int64;
            }
        }

        [Fact]
        public static void Deserialize_ObjectWith_Ctor_With_65_Params_IfNull()
        {
            Assert.Null(JsonSerializer.Deserialize<Class_With_Ctor_With_65_Params>("null"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Struct_With_Ctor_With_65_Params>("null"));
        }

        [Fact]
        public static void Escaped_ParameterNames_Work()
        {
            Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""\u0078"":1,""\u0079"":2}");
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        //[Fact]
        //public static void LastParameterWins()
        //{
        //    // u0078 is "x"
        //    Point_2D point = JsonSerializer.Deserialize<Point_2D>(@"{""\u0078"":1,""\u0079"":2,""x"":4}");
        //    Assert.Equal(4, point.X);
        //    Assert.Equal(2, point.Y);
        //}

        [Fact]
        public static void BitVector32_UsesStructDefaultCtor_MultipleParameterizedCtor()
        {
            string serialized = JsonSerializer.Serialize(new BitVector32(1));
            Assert.Equal(0, JsonSerializer.Deserialize<BitVector32>(serialized).Data);
        }

        [Theory]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_GenericIDictionary_JsonElementExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_GenericIDictionary_ObjectExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_Derived_GenericDictionary_JsonElementExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_Derived_GenericDictionary_ObjectExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt))]
        [InlineData(typeof(SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt))]
        public static void HonorExtensionData(Type type)
        {
            var obj1 = JsonSerializer.Deserialize(@"{""key"": ""value""}", type);

            object extensionData = type.GetProperty("ExtensionData").GetValue(obj1);

            JsonElement value;
            if (extensionData is IDictionary<string, JsonElement> typedExtensionData)
            {
                value = typedExtensionData["key"];
            }
            else
            {
                value = (JsonElement)((IDictionary<string, object>)extensionData)["key"];
            }

            Assert.Equal("value", value.GetString());
        }

        private class SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }

            [JsonConstructor]
            public SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt(int x) { }
        }

        private class SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt
        {
            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }

            [JsonConstructor]
            public SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt(int x) { }
        }

        private class SimpleClassWithParameterizedCtor_GenericIDictionary_JsonElementExt
        {
            [JsonExtensionData]
            public IDictionary<string, JsonElement> ExtensionData { get; set; }

            [JsonConstructor]
            public SimpleClassWithParameterizedCtor_GenericIDictionary_JsonElementExt(int x) { }
        }

        private class SimpleClassWithParameterizedCtor_GenericIDictionary_ObjectExt
        {
            [JsonExtensionData]
            public IDictionary<string, object> ExtensionData { get; set; }

            [JsonConstructor]
            public SimpleClassWithParameterizedCtor_GenericIDictionary_ObjectExt(int x) { }
        }

        private class SimpleClassWithParameterizedCtor_Derived_GenericDictionary_JsonElementExt
        {
            [JsonExtensionData]
            public DerivedGenericDictionary_JsonElementExt ExtensionData { get; set; }

            [JsonConstructor]
            public SimpleClassWithParameterizedCtor_Derived_GenericDictionary_JsonElementExt(int x) { }
        }

        private class DerivedGenericDictionary_JsonElementExt : Dictionary<string, JsonElement> { }

        private class SimpleClassWithParameterizedCtor_Derived_GenericDictionary_ObjectExt
        {
            [JsonExtensionData]
            public StringToGenericDictionaryWrapper<object> ExtensionData { get; set; }

            [JsonConstructor]
            public SimpleClassWithParameterizedCtor_Derived_GenericDictionary_ObjectExt(int x) { }
        }

        private class SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt
        {
            [JsonExtensionData]
            public GenericIDictionaryWrapper<string, JsonElement> ExtensionData { get; set; }

            [JsonConstructor]
            public SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt(int x) { }
        }

        private class SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt
        {
            [JsonExtensionData]
            public GenericIDictionaryWrapper<string, object> ExtensionData { get; set; }

            [JsonConstructor]
            public SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt(int x) { }
        }

        public class Parameterless_Point
        {
            public int X { get; set; }

            public int Y { get; set; }
        }
    }
}
