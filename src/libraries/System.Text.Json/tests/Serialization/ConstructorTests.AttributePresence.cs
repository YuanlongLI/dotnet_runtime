// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ConstrutorTests
    {
        [Theory]
        [InlineData(typeof(PrivateParameterlessCtor))]
        [InlineData(typeof(InternalParameterlessCtor))]
        [InlineData(typeof(ProtectedParameterlessCtor))]
        [InlineData(typeof(PrivateParameterlessCtor_InternalParameterizedCtor))]
        [InlineData(typeof(ProtectedParameterlessCtor_PrivateParameterizedCtor))]
        public static void NonPublicCtors_NotSupported(Type type)
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize("{}", type));
        }

        private class PrivateParameterlessCtor
        {
            private PrivateParameterlessCtor() {}
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

        [Theory]
        [InlineData(typeof(Struct_DefaultConstructor))]
        [InlineData(typeof(Struct_PublicParameterizedConstructor))]
        [InlineData(typeof(Class_DefaultConstructor))]
        [InlineData(typeof(Class_PublicParameterlessCtor_PublicParameterizedCtor))]
        public static void Object_NoAttribute_Supported_WithDefaultConstructor(Type type)
        {
            object obj = JsonSerializer.Deserialize("{}", type);
            Assert.NotNull(obj);
            Assert.Equal(@"{""MyInt"":0}", JsonSerializer.Serialize(obj));
        }

        private struct Struct_DefaultConstructor
        {
            public int MyInt { get; set; }
        }

        private struct Struct_PublicParameterizedConstructor
        {
            public int MyInt { get; set; }

            public Struct_PublicParameterizedConstructor(int myInt)
            {
                MyInt = myInt;
            }
        }

        private class Class_DefaultConstructor
        {
            public int MyInt { get; set; }
        }

        private class Class_PublicParameterlessCtor_PublicParameterizedCtor
        {
            public int MyInt { get; set; }

            public Class_PublicParameterlessCtor_PublicParameterizedCtor() { }

            public Class_PublicParameterlessCtor_PublicParameterizedCtor(int myInt)
            {
                MyInt = myInt;
            }
        }

        [Fact]
        public static void SinglePublicParameterizedCtor_NoPublicParameterlessCtor_NoAttribute_Supported()
        {
            var obj1 = JsonSerializer.Deserialize<PublicParameterizedCtor>(@"{""myInt"":1}");
            Assert.Equal(1, obj1.MyInt);

            var obj2 = JsonSerializer.Deserialize<PrivateParameterlessConstructor_PublicParameterizedCtor>(@"{""myInt"":1}");
            Assert.Equal(1, obj2.MyInt);
        }

        private class PublicParameterizedCtor
        {
            public int MyInt { get; private set; }

            public PublicParameterizedCtor(int myInt)
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

        [Fact]
        public static void SinglePublicParameterizedCtor_NoPublicParameterlessCtor_WithAttribute_Supported()
        {
            var obj1 = JsonSerializer.Deserialize<PublicParameterizedCtor_WithAttribute>(@"{""myInt"":1}");
            Assert.Equal(1, obj1.MyInt);

            var obj2 = JsonSerializer.Deserialize<PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute>(@"{""myInt"":1}");
            Assert.Equal(1, obj2.MyInt);
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
        public static void MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_NoAttribute_NotSupported()
        {
            var obj1 = JsonSerializer.Deserialize<MultiplePublicParameterizedCtor>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(1, obj1.MyInt);

            var obj2 = JsonSerializer.Deserialize<ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(1, obj2.MyInt);
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

        private class ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor
        {
            public int MyInt { get; private set; }
            public string MyString { get; private set; }

            protected ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor() { }

            public ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor(int myInt)
            {
                MyInt = myInt;
            }

            public ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor(int myInt, string myString)
            {
                MyInt = myInt;
                MyString = myString;
            }
        }

        [Fact]
        public static void MultiplePublicParameterizedCtors_NoPublicParameterlessCtor_WithAttribute_Supported()
        {
            var obj1 = JsonSerializer.Deserialize<MultiplePublicParameterizedCtor_WithAttribute>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Null(obj1.MyString);

            var obj2 = JsonSerializer.Deserialize<ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithAttribute>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(1, obj2.MyInt);
            Assert.Equal("1", obj2.MyString);
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

        private class ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithAttribute
        {
            public int MyInt { get; private set; }
            public string MyString { get; private set; }

            protected ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithAttribute() { }

            public ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithAttribute(int myInt)
            {
                MyInt = myInt;
            }

            [JsonConstructor]
            public ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithAttribute(int myInt, string myString)
            {
                MyInt = myInt;
                MyString = myString;
            }
        }

        [Fact]
        public static void SinglePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute_Supported()
        {
            var obj1 = JsonSerializer.Deserialize<SinglePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(1, obj1.MyInt);
            Assert.Null(obj1.MyString);

            var obj2 = JsonSerializer.Deserialize<SinglePublicParameterizedConstructor_WithAttribute>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(1, obj2.MyInt);
            Assert.Equal("1", obj2.MyString);
        }

        private class SinglePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute
        {
            public int MyInt { get; private set; }
            public string MyString { get; private set; }

            
            public SinglePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute() { }

            [JsonConstructor]
            public SinglePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute(int myInt)
            {
                MyInt = myInt;
            }
        }

        private struct SinglePublicParameterizedConstructor_WithAttribute
        {
            public int MyInt { get; set; }
            public string MyString { get; set; }

            [JsonConstructor]
            public SinglePublicParameterizedConstructor_WithAttribute(int myInt, string myString)
            {
                MyInt = myInt;
                MyString = myString;
            }
        }

        [Fact]
        public static void MultiplePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute_Supported()
        {
            var obj1 = JsonSerializer.Deserialize<MultiplePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(0, obj1.MyInt);
            Assert.Null(obj1.MyString);

            var obj2 = JsonSerializer.Deserialize<ProtectedParameterlessConstructor_WithPublicParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(1, obj2.MyInt);
            Assert.Null(obj2.MyString);

            var obj3 = JsonSerializer.Deserialize<MultiplePublicParameterizedConstructor_WithAttribute>(@"{""myInt"":1,""myString"":""1""}");
            Assert.Equal(1, obj3.MyInt);
            Assert.Equal("1", obj3.MyString);
        }

        private class MultiplePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute
        {
            public int MyInt { get; private set; }
            public string MyString { get; private set; }

            [JsonConstructor]
            public MultiplePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute() { }

            public MultiplePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute(int myInt)
            {
                MyInt = myInt;
            }

            public MultiplePublicParameterizedCtors_WithPublicParameterlessCtor_WithAttribute(int myInt, string myString)
            {
                MyInt = myInt;
                MyString = myString;
            }
        }

        private class ProtectedParameterlessConstructor_WithPublicParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute
        {
            public int MyInt { get; private set; }
            public string MyString { get; private set; }

            protected ProtectedParameterlessConstructor_WithPublicParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute() { }

            [JsonConstructor]
            public ProtectedParameterlessConstructor_WithPublicParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute(int myInt)
            {
                MyInt = myInt;
            }

            public ProtectedParameterlessConstructor_WithPublicParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute(int myInt, string myString)
            {
                MyInt = myInt;
                MyString = myString;
            }
        }

        private struct MultiplePublicParameterizedConstructor_WithAttribute
        {
            public int MyInt { get; set; }
            public string MyString { get; set; }

            public MultiplePublicParameterizedConstructor_WithAttribute(int myInt)
            {
                MyInt = myInt;
                MyString = null;
            }

            [JsonConstructor]
            public MultiplePublicParameterizedConstructor_WithAttribute(int myInt, string myString)
            {
                MyInt = myInt;
                MyString = myString;
            }
        }

        [Theory]
        [InlineData(typeof(PrivateParameterlessCtor_InternalParameterizedCtor_WithMultipleAttributes))]
        [InlineData(typeof(ProtectedParameterlessCtor_PrivateParameterizedCtor_WithMultipleAttributes))]
        [InlineData(typeof(MultiplePublicParameterizedCtor_WithMultipleAttributes))]
        [InlineData(typeof(ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithMultipleAttributes))]
        public static void MultipleAttribute_NotSupported(Type type)
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize("{}", type));
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

        private class ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithMultipleAttributes
        {
            public int MyInt { get; private set; }
            public string MyString { get; private set; }

            protected ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithMultipleAttributes() { }

            [JsonConstructor]
            public ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithMultipleAttributes(int myInt)
            {
                MyInt = myInt;
            }

            [JsonConstructor]
            public ProtectedParameterlessConstructor_MultiplePublicParameterizedCtor_WithMultipleAttributes(int myInt, string myString)
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

        // Highlight that we still support serialization where possible.
    }
}
