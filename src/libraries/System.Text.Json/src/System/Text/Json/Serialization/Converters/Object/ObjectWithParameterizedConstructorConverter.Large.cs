﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed class LargeObjectWithParameterizedConstructorConverter<TypeToConvert> :
        ObjectWithParameterizedConstructorConverter<TypeToConvert>
    {
        private JsonClassInfo.ParameterizedConstructorDelegate<TypeToConvert>? _createObject;

        internal override void Initialize(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            base.Initialize(constructor, options);

            _createObject = options.MemberAccessorStrategy.CreateParameterizedConstructor<TypeToConvert>(constructor)!;
        }

        protected override bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options)
        {
            bool success = jsonParameterInfo.ReadJson(ref state, ref reader, options, out object? arg0);

            if (success)
            {
                ((object[])state.Current.ConstructorArguments!)[jsonParameterInfo.Position] = arg0!;
            }

            return success;
        }

        protected override object CreateObject(ref ReadStack state)
        {
            if (_createObject == null)
            {
                // This means this constructor has more than 64 parameters.
                throw new NotSupportedException();
            }

            object[] arguments = (object[])state.Current.ConstructorArguments!;

            object obj = _createObject(arguments)!;

            ArrayPool<object>.Shared.Return(arguments, clearArray: true);
            return obj;
        }

        protected override void InitializeConstructorArgumentCaches(ref ReadStackFrame frame, JsonSerializerOptions options)
        {
            object[] arguments = ArrayPool<object>.Shared.Rent(ParameterCount);
            foreach (JsonParameterInfo parameterInfo in ParameterCache.Values)
            {
                arguments[parameterInfo.Position] = parameterInfo.DefaultValue!;
            }

            frame.ConstructorArguments = arguments;
        }
    }
}
