// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace System.Text.Json
{
    internal sealed class ReflectionMemberAccessor : MemberAccessor
    {
        public override JsonClassInfo.ConstructorDelegate? CreateConstructor(Type type)
        {
            Debug.Assert(type != null);
            ConstructorInfo? realMethod = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null);

            if (type.IsAbstract)
            {
                return null;
            }

            if (realMethod == null && !type.IsValueType)
            {
                return null;
            }

            return () => Activator.CreateInstance(type, nonPublic: false);
        }

        public override JsonClassInfo.ParameterizedConstructorDelegate<TTypeToConvert, TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>?
            CreateParameterizedConstructor<TTypeToConvert, TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(ConstructorInfo constructor)
        {
            Type type = typeof(TTypeToConvert);
            Debug.Assert(!type.IsAbstract);
            Debug.Assert(type.GetConstructors().Contains(constructor));

            int parameterCount = constructor.GetParameters().Length;

            // This call will only invoke the desired public constructor, as the arguments below will match the method signature exactly.
            // We verfied the constructor exists and is public during the selection of the calling converter.
            return (arg0, arg1, arg2, arg3, arg4, arg5, arg6, restOfArgs) =>
            {
                object[] arguments = new object[parameterCount];

                for (int i = 0; i < parameterCount; i++)
                {
                    switch (i)
                    {
                        case 0:
                            arguments[0] = arg0!;
                            break;
                        case 1:
                            arguments[1] = arg1!;
                            break;
                        case 2:
                            arguments[2] = arg2!;
                            break;
                        case 3:
                            arguments[3] = arg3!;
                            break;
                        case 4:
                            arguments[4] = arg4!;
                            break;
                        case 5:
                            arguments[5] = arg5!;
                            break;
                        case 6:
                            arguments[6] = arg6!;
                            break;
                        default:
                            arguments[i] = restOfArgs[i - 7];
                            break;
                    }
                }

                return (TTypeToConvert)Activator.CreateInstance(type, arguments)!;
            };
        }

        public override Action<TCollection, object> CreateAddMethodDelegate<TCollection>()
        {
            Type collectionType = typeof(TCollection);
            Type elementType = typeof(object);

            // We verified this won't be null when we created the converter for the collection type.
            MethodInfo addMethod = (collectionType.GetMethod("Push") ?? collectionType.GetMethod("Enqueue"))!;

            return delegate (TCollection collection, object element)
            {
                addMethod.Invoke(collection, new object[] { element! });
            };
        }

        public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TElement, TCollection>()
        {
            MethodInfo createRange = typeof(TCollection).GetImmutableEnumerableCreateRangeMethod(typeof(TElement));
            return (Func<IEnumerable<TElement>, TCollection>)createRange.CreateDelegate(
                typeof(Func<IEnumerable<TElement>, TCollection>));
        }

        public override Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TElement, TCollection>()
        {
            MethodInfo createRange = typeof(TCollection).GetImmutableDictionaryCreateRangeMethod(typeof(TElement));
            return (Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection>)createRange.CreateDelegate(
                typeof(Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection>));
        }

        public override Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetGetMethod()!;

            return delegate (object obj)
            {
                return (TProperty)getMethodInfo.Invoke(obj, null)!;
            };
        }

        public override Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.GetSetMethod()!;

            return delegate (object obj, TProperty value)
            {
                setMethodInfo.Invoke(obj, new object[] { value! });
            };
        }
    }
}
