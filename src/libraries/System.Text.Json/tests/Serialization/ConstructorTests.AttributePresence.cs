//Licensed to the.NET Foundation under one or more agreements.
//The.NET Foundation licenses this file to you under the MIT license.
//See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ConstructorTests
    {
        [Theory]
        [InlineData(typeof(PrivateParameterlessCtor))]
        [InlineData(typeof(InternalParameterlessCtor))]
        [InlineData(typeof(ProtectedParameterlessCtor))]
        [InlineData(typeof(PrivateParameterlessCtor_InternalParameterizedCtor))]
        [InlineData(typeof(ProtectedParameterlessCtor_PrivateParameterizedCtor))]
        [InlineData(typeof(PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes))]
        [InlineData(typeof(ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes))]
        public static void NonPublicCtors_NotSupported(Type type)
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize("{}", type));
        }

        [Fact]
        public static void SingleePublicParameterizedCtors_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj1 = JsonSerializer.Deserialize<SinglePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj1));
        }

        [Theory]
        [InlineData(typeof(SingleParameterlessCtor_MultiplePublicParameterizedCtor))]
        [InlineData(typeof(SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct))]
        public static void MultiplePublicParameterizedCtors_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor(Type type)
        {
            var obj1 = JsonSerializer.Deserialize(@"{""MyInt"":1,""MyString"":""1""}", type);
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj1));
        }

        [Theory]
        [InlineData(typeof(PublicParameterizedCtor))]
        [InlineData(typeof(Struct_PublicParameterizedConstructor))]
        [InlineData(typeof(PrivateParameterlessConstructor_PublicParameterizedCtor))]
        public static void SinglePublicParameterizedCtor_NoPublicParameterlessCtor_NoAttribute_Supported(Type type)
        {
            var obj1 = JsonSerializer.Deserialize(@"{""MyInt"":1}", type);
            Assert.Equal(@"{""MyInt"":1}", JsonSerializer.Serialize(obj1));
        }

        [Theory]
        [InlineData(typeof(PublicParameterizedCtor_WithAttribute))]
        [InlineData(typeof(Struct_PublicParameterizedConstructor_WithAttribute))]
        [InlineData(typeof(PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute))]
        public static void SinglePublicParameterizedCtor_NoPublicParameterlessCtor_WithAttribute_Supported(Type type)
        {
            var obj1 = JsonSerializer.Deserialize(@"{""MyInt"":1}", type);
            Assert.Equal(@"{""MyInt"":1}", JsonSerializer.Serialize(obj1));
        }

        [Fact]
        public static void Class_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<MultiplePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}"));
        }

        [Fact]
        public static void Struct_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj = JsonSerializer.Deserialize<MultiplePublicParameterizedCtor_Struct>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj));
        }

        //[Fact]
        //public static void NoPublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        //{
        //    var obj1 = JsonSerializer.Deserialize<MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
        //    Assert.Equal(1, obj1.MyInt);
        //    Assert.Null(obj1.MyString);
        //    Assert.Equal(@"{""MyInt"":1,""MyString"":null}", JsonSerializer.Serialize(obj1));

        //    //var obj2 = JsonSerializer.Deserialize<MultiplePublicParameterizedCtor_WithAttribute_Struct>(@"{""MyInt"":1,""MyString"":""1""}");
        //    //Assert.Equal(1, obj2.MyInt);
        //    //Assert.Equal("1", obj2.MyString);
        //    //Assert.Equal(@"{""MyInt"":1,""MyString"":""1""}", JsonSerializer.Serialize(obj2));
        //}

        [Fact]
        public static void PublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        {
            var obj = JsonSerializer.Deserialize<ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":null}", JsonSerializer.Serialize(obj));
        }

        [Theory]
        [InlineData(typeof(MultiplePublicParameterizedCtor_WithMultipleAttributes))]
        [InlineData(typeof(PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes))]
        public static void MultipleAttribute_NotSupported(Type type)
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize("{}", type));
        }

        [Theory]
        [InlineData(typeof(Parameterized_StackWrapper))]
        [InlineData(typeof(Parameterized_WrapperForICollection))]
        public static void AttributeIgnoredOnIEnumerable(Type type)
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize("[]", type));
        }
    }
}
