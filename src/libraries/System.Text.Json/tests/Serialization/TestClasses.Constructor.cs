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
    public class PrivateParameterlessCtor
    {
        private PrivateParameterlessCtor() { }
    }

    public class InternalParameterlessCtor
    {
        internal InternalParameterlessCtor() { }
    }

    public class ProtectedParameterlessCtor
    {
        protected ProtectedParameterlessCtor() { }
    }

    public class PrivateParameterlessCtor_InternalParameterizedCtor
    {
        private PrivateParameterlessCtor_InternalParameterizedCtor() { }

        internal PrivateParameterlessCtor_InternalParameterizedCtor(int value) { }
    }

    public class ProtectedParameterlessCtor_PrivateParameterizedCtor
    {
        protected ProtectedParameterlessCtor_PrivateParameterizedCtor() { }

        private ProtectedParameterlessCtor_PrivateParameterizedCtor(int value) { }
    }

    public class PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes
    {
        [JsonConstructor]
        private PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes() { }

        [JsonConstructor]
        internal PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes(int value) { }
    }

    public class ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes
    {
        [JsonConstructor]
        protected ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes() { }

        [JsonConstructor]
        private ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes(int value) { }
    }

    public class SinglePublicParameterizedCtor
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public SinglePublicParameterizedCtor() { }

        public SinglePublicParameterizedCtor(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class SingleParameterlessCtor_MultiplePublicParameterizedCtor
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor() { }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor(int myInt)
        {
            MyInt = myInt;
        }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public struct SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct(int myInt)
        {
            MyInt = myInt;
            MyString = null;
        }

        public SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class PublicParameterizedCtor
    {
        public int MyInt { get; private set; }

        public PublicParameterizedCtor(int myInt)
        {
            MyInt = myInt;
        }
    }

    public struct Struct_PublicParameterizedConstructor
    {
        public int MyInt { get; }

        public Struct_PublicParameterizedConstructor(int myInt)
        {
            MyInt = myInt;
        }
    }

    public class PrivateParameterlessConstructor_PublicParameterizedCtor
    {
        public int MyInt { get; private set; }

        private PrivateParameterlessConstructor_PublicParameterizedCtor() { }

        public PrivateParameterlessConstructor_PublicParameterizedCtor(int myInt)
        {
            MyInt = myInt;
        }
    }

    public class PublicParameterizedCtor_WithAttribute
    {
        public int MyInt { get; private set; }

        [JsonConstructor]
        public PublicParameterizedCtor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }
    }

    public struct Struct_PublicParameterizedConstructor_WithAttribute
    {
        public int MyInt { get; }

        [JsonConstructor]
        public Struct_PublicParameterizedConstructor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }
    }

    public class PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute
    {
        public int MyInt { get; private set; }

        private PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute() { }

        [JsonConstructor]
        public PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }
    }

    public class MultiplePublicParameterizedCtor
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public MultiplePublicParameterizedCtor(int myInt)
        {
            MyInt = myInt;
        }

        public MultiplePublicParameterizedCtor(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public struct MultiplePublicParameterizedCtor_Struct
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public MultiplePublicParameterizedCtor_Struct(int myInt)
        {
            MyInt = myInt;
            MyString = null;
        }

        public MultiplePublicParameterizedCtor_Struct(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class MultiplePublicParameterizedCtor_WithAttribute
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        [JsonConstructor]
        public MultiplePublicParameterizedCtor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }

        public MultiplePublicParameterizedCtor_WithAttribute(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public struct MultiplePublicParameterizedCtor_WithAttribute_Struct
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public MultiplePublicParameterizedCtor_WithAttribute_Struct(int myInt)
        {
            MyInt = myInt;
            MyString = null;
        }

        [JsonConstructor]
        public MultiplePublicParameterizedCtor_WithAttribute_Struct(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        public ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute() { }

        [JsonConstructor]
        public ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute(int myInt)
        {
            MyInt = myInt;
        }

        public ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class MultiplePublicParameterizedCtor_WithMultipleAttributes
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        [JsonConstructor]
        public MultiplePublicParameterizedCtor_WithMultipleAttributes(int myInt)
        {
            MyInt = myInt;
        }

        [JsonConstructor]
        public MultiplePublicParameterizedCtor_WithMultipleAttributes(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes
    {
        public int MyInt { get; private set; }
        public string MyString { get; private set; }

        [JsonConstructor]
        public PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes() { }

        [JsonConstructor]
        public PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes(int myInt, string myString)
        {
            MyInt = myInt;
            MyString = myString;
        }
    }

    public class Parameterized_StackWrapper : Stack
    {
        [JsonConstructor]
        public Parameterized_StackWrapper(object[] elements)
        {
            foreach (object element in elements)
            {
                Push(element);
            }
        }
    }

    public class Parameterized_WrapperForICollection : ICollection
    {
        private List<object> _list = new List<object>();

        public Parameterized_WrapperForICollection(object[] elements)
        {
            _list.AddRange(elements);
        }

        public int Count => ((ICollection)_list).Count;

        public bool IsSynchronized => ((ICollection)_list).IsSynchronized;

        public object SyncRoot => ((ICollection)_list).SyncRoot;

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_list).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return ((ICollection)_list).GetEnumerator();
        }
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

    public struct Point_2D_With_ExtData
    {
        public int X { get; }

        public int Y { get; }

        [JsonConstructor]
        public Point_2D_With_ExtData(int x, int y)
        {
            X = x;
            Y = y;
            ExtensionData = new Dictionary<string, JsonElement>();
        }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }

    public struct WrapperForPoint_3D
    {
        public Point_3D Point_3D { get; set; }
    }

    public class ClassWrapperForPoint_3D
    {
        public Point_3D Point3D { get; }

        public ClassWrapperForPoint_3D(Point_3D point3D)
        {
            Point3D = point3D;
        }
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

    public class ClassWrapper_For_Int_Point_3D_String
    {
        public int MyInt { get; }

        public Point_3D_Struct MyPoint3DStruct { get; }

        public string MyString { get; }

        public ClassWrapper_For_Int_Point_3D_String(Point_3D_Struct myPoint3DStruct)
        {
            MyInt = 0;
            MyPoint3DStruct = myPoint3DStruct;
            MyString = null;
        }

        [JsonConstructor]
        public ClassWrapper_For_Int_Point_3D_String(int myInt, Point_3D_Struct myPoint3DStruct, string myString)
        {
            MyInt = myInt;
            MyPoint3DStruct = myPoint3DStruct;
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

    public class Person_Class
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

    public struct Person_Struct
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

    public class Point_2D_With_ExtData_Class
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

    public class Point_PropertiesHavePropertyNames
    {
        [JsonPropertyName("A")]
        public int X { get; set; }

        [JsonPropertyName("B")]
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

    public class ClassWithConstructor_SimpleAndComplexParameters : ITestClass
    {
        public byte MyByte { get; }
        public sbyte MySByte { get; set; }
        public char MyChar { get; }
        public string MyString { get; }
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

        public static ClassWithConstructor_SimpleAndComplexParameters GetInstance() =>
            JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(s_json);

        public static readonly string s_json = $"{{{s_partialJson1},{s_partialJson2}}}";
        public static readonly string s_json_flipped = $"{{{s_partialJson2},{s_partialJson1}}}";

        private const string s_partialJson1 =
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
            @"""MyInt16ThreeDimensionArray"" : [[[11, 12],[13, 14]],[[21,22],[23,24]]]";

        private const string s_partialJson2 =
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
            @"""MyListOfNullString"" : [null]";

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
    public class Parameterless_ClassWithPrimitives
    {
        public int FirstInt { get; set; }
        public int SecondInt { get; set; }

        public string FirstString { get; set; }
        public string SecondString { get; set; }

        public DateTime FirstDateTime { get; set; }
        public DateTime SecondDateTime { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public int ThirdInt { get; set; }
        public int FourthInt { get; set; }

        public string ThirdString { get; set; }
        public string FourthString { get; set; }

        public DateTime ThirdDateTime { get; set; }
        public DateTime FourthDateTime { get; set; }
    }

    public class Parameterized_ClassWithPrimitives_3Args
    {
        public int FirstInt { get; set; }
        public int SecondInt { get; set; }

        public string FirstString { get; set; }
        public string SecondString { get; set; }

        public DateTime FirstDateTime { get; set; }
        public DateTime SecondDateTime { get; set; }

        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public int ThirdInt { get; set; }
        public int FourthInt { get; set; }

        public string ThirdString { get; set; }
        public string FourthString { get; set; }

        public DateTime ThirdDateTime { get; set; }
        public DateTime FourthDateTime { get; set; }


        public Parameterized_ClassWithPrimitives_3Args(int x, int y, int z) => (X, Y, Z) = (x, y, z);
    }

    public class TupleWrapper
    {
        public Tuple<string, double> Tuple { get; set; }
    }

    public class ConverterForPoint3D : JsonConverter<Point_3D>
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
    public class ConverterForInt32 : JsonConverter<int>
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

    public class Parameterized_Person
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public Guid Id { get; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }

        public Parameterized_Person(Guid id) => Id = id;
    }

    public class SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt(int x) { }
    }

    public class SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt
    {
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt(int x) { }
    }

    public class SimpleClassWithParameterizedCtor_GenericIDictionary_JsonElementExt
    {
        [JsonExtensionData]
        public IDictionary<string, JsonElement> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_GenericIDictionary_JsonElementExt(int x) { }
    }

    public class SimpleClassWithParameterizedCtor_GenericIDictionary_ObjectExt
    {
        [JsonExtensionData]
        public IDictionary<string, object> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_GenericIDictionary_ObjectExt(int x) { }
    }

    public class SimpleClassWithParameterizedCtor_Derived_GenericDictionary_JsonElementExt
    {
        [JsonExtensionData]
        public DerivedGenericDictionary_JsonElementExt ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_Derived_GenericDictionary_JsonElementExt(int x) { }
    }

    public class DerivedGenericDictionary_JsonElementExt : Dictionary<string, JsonElement> { }

    public class SimpleClassWithParameterizedCtor_Derived_GenericDictionary_ObjectExt
    {
        [JsonExtensionData]
        public StringToGenericDictionaryWrapper<object> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_Derived_GenericDictionary_ObjectExt(int x) { }
    }

    public class SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt
    {
        [JsonExtensionData]
        public GenericIDictionaryWrapper<string, JsonElement> ExtensionData { get; set; }

        [JsonConstructor]
        public SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt(int x) { }
    }

    public class SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt
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
