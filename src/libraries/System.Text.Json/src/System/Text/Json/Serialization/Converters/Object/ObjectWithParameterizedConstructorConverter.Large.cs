// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
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

        protected override void ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options)
        {
            Debug.Assert(state.Current.ConstructorArguments == null);

            jsonParameterInfo.ReadJson(ref state, ref reader, options, out object? arg0);

            Debug.Assert(state.Current.ConstructorArgumentsArray != null);
            state.Current.ConstructorArgumentsArray[jsonParameterInfo.Position] = arg0!;
        }

        protected override object CreateObject(ref ReadStack state)
        {
            if (_createObject == null)
            {
                // This means this constructor has more than 64 parameters.
                throw new NotSupportedException();
            }

            object obj = _createObject(state.Current.ConstructorArgumentsArray!)!;

            ArrayPool<object>.Shared.Return(state.Current.ConstructorArgumentsArray!, clearArray: true);
            ArrayPool<bool>.Shared.Return(state.Current.ConstructorArgumentState!, clearArray: true);
            return obj;
        }

        protected override void InitializeConstructorArgumentCache(ref ReadStackFrame frame, JsonSerializerOptions options)
        {
            frame.ConstructorArgumentsArray = ArrayPool<object>.Shared.Rent(ParameterCount);

            foreach (JsonParameterInfo parameterInfo in ParameterCache.Values)
            {
                frame.ConstructorArgumentsArray[parameterInfo.Position] = parameterInfo.DefaultValue!;
            }

            frame.ConstructorArgumentState = ArrayPool<bool>.Shared.Rent(ParameterCount);
            frame.JsonPropertyKindIndicator = ArrayPool<bool>.Shared.Rent(PropertyNameCountCacheThreshold);
        }
    }
}
