﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    /// <summary>
    /// Represents a strongly-typed parameter to prevent boxing where have less than 4 parameters.
    /// Holds relevant state like the default value of the parameter, and the position in the method's parameter list.
    /// </summary>
    internal class JsonParameterInfo<T> : JsonParameterInfo
    {
        private JsonConverter<T> _converter = null!;
        private Type _runtimePropertyType = null!;

        public override JsonConverter ConverterBase => _converter;

        public T TypedDefaultValue { get; private set; } = default!;

        public override void Initialize(
            string matchingPropertyName,
            Type declaredPropertyType,
            Type runtimePropertyType,
            ParameterInfo parameterInfo,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            base.Initialize(
                matchingPropertyName,
                declaredPropertyType,
                runtimePropertyType,
                parameterInfo,
                converter,
                options);

            _converter = (JsonConverter<T>)converter;
            _runtimePropertyType = runtimePropertyType;

            if (parameterInfo.HasDefaultValue)
            {
                DefaultValue = parameterInfo.DefaultValue;
                TypedDefaultValue = (T)parameterInfo.DefaultValue!;
            }
            else
            {
                DefaultValue = TypedDefaultValue;
            }
        }

        public override bool ReadJson(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options, out object? value)
        {
            bool success;
            bool isNullToken = reader.TokenType == JsonTokenType.Null;

            if (isNullToken &&
                ((!_converter.HandleNullValue && !state.IsContinuation) || options.IgnoreNullValues))
            {
                // Don't have to check for IgnoreNullValue option here because we set the default value (likely null) regardless
                value = DefaultValue;
                return true;
            }
            else
            {
                // Optimize for internal converters by avoiding the extra call to TryRead.
                if (_converter.CanUseDirectReadOrWrite)
                {
                    value = _converter.Read(ref reader, _runtimePropertyType, options);
                    return true;
                }
                else
                {
                    success = _converter.TryRead(ref reader, _runtimePropertyType, options, ref state, out T typedValue);
                    value = typedValue;
                }
            }

            return success;
        }

        public bool ReadJsonTyped(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options, out T value)
        {
            bool success;
            bool isNullToken = reader.TokenType == JsonTokenType.Null;

            if (isNullToken &&
                ((!_converter.HandleNullValue && !state.IsContinuation) || options.IgnoreNullValues))
            {
                // Don't have to check for IgnoreNullValue option here because we set the default value (likely null) regardless
                value = TypedDefaultValue;
                return true;
            }
            else
            {
                // Optimize for internal converters by avoiding the extra call to TryRead.
                if (_converter.CanUseDirectReadOrWrite)
                {
                    value = _converter.Read(ref reader, _runtimePropertyType, options);
                    return true;
                }
                else
                {
                    success = _converter.TryRead(ref reader, _runtimePropertyType, options, ref state, out value);
                }
            }

            return success;
        }
    }
}
