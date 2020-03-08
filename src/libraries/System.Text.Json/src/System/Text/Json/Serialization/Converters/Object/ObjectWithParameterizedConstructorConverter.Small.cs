// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed class SmallObjectWithParameterizedConstructorConverter<TypeToConvert, TArg0, TArg1, TArg2, TArg3> :
        ObjectWithParameterizedConstructorConverter<TypeToConvert>
    {
        private JsonClassInfo.ParameterizedConstructorDelegate<TypeToConvert, TArg0, TArg1, TArg2, TArg3>? _createObject;

        internal override void Initialize(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            base.Initialize(constructor, options);

            _createObject = options.MemberAccessorStrategy.CreateParameterizedConstructor<TypeToConvert, TArg0, TArg1, TArg2, TArg3>(constructor)!;
        }

        protected override object CreateObject(ref ReadStack state)
        {
            var argCache = (ArgumentCache<TArg0, TArg1, TArg2, TArg3>)state.Current.ConstructorArguments!;
            return _createObject!(argCache.Arg0, argCache.Arg1, argCache.Arg2, argCache.Arg3)!;
        }

        protected override void ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options)
        {
            Debug.Assert(state.Current.ConstructorArguments != null);

            var argCache = (ArgumentCache<TArg0, TArg1, TArg2, TArg3>)state.Current.ConstructorArguments;

            switch (jsonParameterInfo.Position)
            {
                case 0:
                    ((JsonParameterInfo<TArg0>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg0 arg0);
                    argCache.Arg0 = arg0;
                    break;
                case 1:
                    ((JsonParameterInfo<TArg1>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg1 arg1);
                    argCache.Arg1 = arg1;
                    break;
                case 2:
                    ((JsonParameterInfo<TArg2>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg2 arg2);
                    argCache.Arg2 = arg2;
                    break;
                case 3:
                    ((JsonParameterInfo<TArg3>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg3 arg3);
                    argCache.Arg3 = arg3;
                    break;
                default:
                    Debug.Fail("This should never happen.");
                    break;
            }
        }

        protected override void InitializeConstructorArgumentCaches(ref ReadStackFrame frame, JsonSerializerOptions options)
        {
            var argCache = new ArgumentCache<TArg0, TArg1, TArg2, TArg3>();

            foreach (JsonParameterInfo parameterInfo in ParameterCache.Values)
            {
                int position = parameterInfo.Position;

                switch (position)
                {
                    case 0:
                        argCache.Arg0 = ((JsonParameterInfo<TArg0>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 1:
                        argCache.Arg1 = ((JsonParameterInfo<TArg1>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 2:
                        argCache.Arg2 = ((JsonParameterInfo<TArg2>)parameterInfo).TypedDefaultValue!;
                        break;
                    case 3:
                        argCache.Arg3 = ((JsonParameterInfo<TArg3>)parameterInfo).TypedDefaultValue!;
                        break;
                    default:
                        Debug.Fail("We should never get here.");
                        break;
                }
            }

            frame.ConstructorArguments = argCache;
        }
    }
}
