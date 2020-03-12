﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed class LargeObjectWithParameterizedConstructorConverter<T> : ObjectWithParameterizedConstructorConverter<T> where T : notnull
    {
        private JsonClassInfo.ParameterizedConstructorDelegate<T>? _createObject;

        internal override void CreateConstructorDelegate(JsonSerializerOptions options)
        {
            _createObject = options.MemberAccessorStrategy.CreateParameterizedConstructor<T>(ConstructorInfo)!;
        }

        protected override bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options)
        {
            bool success = jsonParameterInfo.ReadJson(ref state, ref reader, options, out object? arg0);

            if (success)
            {
                ((object[])state.Current.CtorArgumentState.Arguments!)[jsonParameterInfo.Position] = arg0!;
            }

            return success;
        }

        protected override object CreateObject(ref ReadStack state)
        {
            if (_createObject == null)
            {
                // This means this constructor has more than 64 parameters.
                ThrowHelper.ThrowNotSupportedException_ConstructorMaxOf64Parameters(ConstructorInfo, TypeToConvert);
            }

            object[] arguments = (object[])state.Current.CtorArgumentState.Arguments!;

            object obj = _createObject(arguments)!;

            ArrayPool<object>.Shared.Return(arguments, clearArray: true);
            return obj;
        }

        protected override void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options)
        {
            // Clear state from previous deserialization.
            state.Current.CtorArgumentState.Reset();

            int paramCount = state.Current.JsonClassInfo.ParameterCount;
            Dictionary<string, JsonParameterInfo>.ValueCollection parameterCacheValues = state.Current.JsonClassInfo.ParameterCache!.Values;

            if (paramCount != parameterCacheValues.Count)
            {
                ThrowHelper.ThrowInvalidOperationException_ConstructorParameterIncompleteBinding(ConstructorInfo, TypeToConvert);
            }

            object[] arguments = ArrayPool<object>.Shared.Rent(paramCount);
            foreach (JsonParameterInfo jsonParameterInfo in parameterCacheValues)
            {
                if (jsonParameterInfo.ShouldDeserialize)
                {
                    arguments[jsonParameterInfo.Position] = jsonParameterInfo.DefaultValue!;
                }
            }

            state.Current.CtorArgumentState.Arguments = arguments;
        }
    }
}
