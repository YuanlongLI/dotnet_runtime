// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal class JsonParameterInfo<TConverter> : JsonParameterInfo
    {
        private JsonConverter<TConverter> _converter = null!;
        private Type _runtimePropertyType = null!;

        public override void Initialize(
            Type declaredPropertyType,
            Type runtimePropertyType,
            ParameterInfo parameterInfo,
            Type parentClassType,
            JsonConverter converter,
            ClassType classType,
            JsonSerializerOptions options)
        {
            base.Initialize(
                declaredPropertyType,
                runtimePropertyType,
                parameterInfo,
                parentClassType,
                converter,
                classType,
                options);

            _converter = (JsonConverter<TConverter>)converter;
            _runtimePropertyType = runtimePropertyType;

            DefaultValue = parameterInfo.HasDefaultValue ? parameterInfo.DefaultValue : default(TConverter)!;
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
                    success = _converter.TryRead(ref reader, _runtimePropertyType, options, ref state, out TConverter typedValue);
                    value = typedValue;
                }
            }

            return success;
        }
    }
}
