// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
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

        private class PrivateParameterlessCtor
        {
            private PrivateParameterlessCtor() { }
        }

        private class InternalParameterlessCtor
        {
            internal InternalParameterlessCtor() { }
        }

        private class ProtectedParameterlessCtor
        {
            protected ProtectedParameterlessCtor() { }
        }

        private class PrivateParameterlessCtor_InternalParameterizedCtor
        {
            private PrivateParameterlessCtor_InternalParameterizedCtor() { }

            internal PrivateParameterlessCtor_InternalParameterizedCtor(int value) { }
        }

        private class ProtectedParameterlessCtor_PrivateParameterizedCtor
        {
            protected ProtectedParameterlessCtor_PrivateParameterizedCtor() { }

            private ProtectedParameterlessCtor_PrivateParameterizedCtor(int value) { }
        }

        private class PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes
        {
            [JsonConstructor]
            private PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes() { }

            [JsonConstructor]
            internal PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes(int value) { }
        }

        private class ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes
        {
            [JsonConstructor]
            protected ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes() { }

            [JsonConstructor]
            private ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes(int value) { }
        }

        [Fact]
        public static void SingleePublicParameterizedCtors_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj1 = JsonSerializer.Deserialize<SinglePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj1));
        }

        private class SinglePublicParameterizedCtor
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

        [Theory]
        [InlineData(typeof(SingleParameterlessCtor_MultiplePublicParameterizedCtor))]
        [InlineData(typeof(SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct))]
        public static void MultiplePublicParameterizedCtors_SingleParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor(Type type)
        {
            var obj1 = JsonSerializer.Deserialize(@"{""MyInt"":1,""MyString"":""1""}", type);
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj1));
        }

        private class SingleParameterlessCtor_MultiplePublicParameterizedCtor
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

        private struct SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct
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

        [Theory]
        [InlineData(typeof(PublicParameterizedCtor))]
        [InlineData(typeof(Struct_PublicParameterizedConstructor))]
        [InlineData(typeof(PrivateParameterlessConstructor_PublicParameterizedCtor))]
        public static void SinglePublicParameterizedCtor_NoPublicParameterlessCtor_NoAttribute_Supported(Type type)
        {
            var obj1 = JsonSerializer.Deserialize(@"{""MyInt"":1}", type);
            Assert.Equal(@"{""MyInt"":1}", JsonSerializer.Serialize(obj1));
        }

        private class PublicParameterizedCtor
        {
            public int MyInt { get; private set; }

            public PublicParameterizedCtor(int myInt)
            {
                MyInt = myInt;
            }
        }

        private struct Struct_PublicParameterizedConstructor
        {
            public int MyInt { get; }

            public Struct_PublicParameterizedConstructor(int myInt)
            {
                MyInt = myInt;
            }
        }

        private class PrivateParameterlessConstructor_PublicParameterizedCtor
        {
            public int MyInt { get; private set; }

            private PrivateParameterlessConstructor_PublicParameterizedCtor() { }

            public PrivateParameterlessConstructor_PublicParameterizedCtor(int myInt)
            {
                MyInt = myInt;
            }
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

        private class PublicParameterizedCtor_WithAttribute
        {
            public int MyInt { get; private set; }

            [JsonConstructor]
            public PublicParameterizedCtor_WithAttribute(int myInt)
            {
                MyInt = myInt;
            }
        }

        private struct Struct_PublicParameterizedConstructor_WithAttribute
        {
            public int MyInt { get; }

            [JsonConstructor]
            public Struct_PublicParameterizedConstructor_WithAttribute(int myInt)
            {
                MyInt = myInt;
            }
        }

        private class PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute
        {
            public int MyInt { get; private set; }

            private PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute() { }

            [JsonConstructor]
            public PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute(int myInt)
            {
                MyInt = myInt;
            }
        }

        [Fact]
        public static void Class_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<MultiplePublicParameterizedCtor>(@"{""MyInt"":1,""MyString"":""1""}"));
        }

        private class MultiplePublicParameterizedCtor
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

        [Fact]
        public static void Struct_MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_Supported_UseParameterlessCtor()
        {
            var obj = JsonSerializer.Deserialize<MultiplePublicParameterizedCtor_Struct>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":0,""MyString"":null}", JsonSerializer.Serialize(obj));
        }

        private struct MultiplePublicParameterizedCtor_Struct
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

        [Fact]
        public static void NoPublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        {
            var obj1 = JsonSerializer.Deserialize<MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Null(obj1.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":null}", JsonSerializer.Serialize(obj1));

            var obj2 = JsonSerializer.Deserialize<MultiplePublicParameterizedCtor_WithAttribute_Struct>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj2.MyInt);
            Assert.Equal("1", obj2.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":""1""}", JsonSerializer.Serialize(obj2));
        }

        private class MultiplePublicParameterizedCtor_WithAttribute
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

        private struct MultiplePublicParameterizedCtor_WithAttribute_Struct
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

        [Fact]
        public static void PublicParameterlessCtor_MultiplePublicParameterizedCtors_WithAttribute_Supported()
        {
            var obj = JsonSerializer.Deserialize<ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute>(@"{""MyInt"":1,""MyString"":""1""}");
            Assert.Equal(1, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(@"{""MyInt"":1,""MyString"":null}", JsonSerializer.Serialize(obj));
        }

        private class ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute
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

        [Theory]
        [InlineData(typeof(MultiplePublicParameterizedCtor_WithMultipleAttributes))]
        [InlineData(typeof(PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes))]
        public static void MultipleAttribute_NotSupported(Type type)
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize("{}", type));
        }

        private class MultiplePublicParameterizedCtor_WithMultipleAttributes
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

        private class PublicParameterlessConstructor_PublicParameterizedCtor_WithMultipleAttributes
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

        [Theory]
        [InlineData(typeof(StackWrapper))]
        [InlineData(typeof(WrapperForICollection))]
        public static void AttributeIgnoredOnIEnumerable(Type type)
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize("[]", type));
        }

        private class StackWrapper : Stack
        {
            [JsonConstructor]
            public StackWrapper(object[] elements)
            {
                foreach (object element in elements)
                {
                    Push(element);
                }
            }
        }

        private class WrapperForICollection : ICollection
        {
            private List<object> _list = new List<object>();

            public WrapperForICollection(object[] elements)
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
    }
}
