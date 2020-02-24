// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter factory for all object-based types (non-enumerable and non-primitive).
    /// </summary>
    internal class JsonObjectConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            // This is the last built-in factory converter, so if the IEnumerableConverterFactory doesn't
            // support it, then it is not IEnumerable.
            Debug.Assert(!typeof(IEnumerable).IsAssignableFrom(typeToConvert));
            return true;
        }

        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonObjectDefaultConverter`1")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonObjectWithParameterizedConstructorConverter`8")]
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            ConstructorInfo? constructor = GetDeserializationConstructor(typeToConvert);

            Type converterType;
            object[]? arguments = null;

            ParameterInfo[]? parameters;

            if (constructor == null || typeToConvert.IsAbstract || (parameters = constructor.GetParameters()).Length == 0)
            {
                converterType = typeof(JsonObjectDefaultConverter<>).MakeGenericType(typeToConvert);
            }
            else
            {
                Type[] typeArguments = new Type[8];

                typeArguments[0] = typeToConvert;

                int parameterCount = parameters.Length;
                Type objectType = typeof(object);

                // Get the first 7 parameters; use dummy parameters if less than 7.
                for (int i = 0; i < 7; i++)
                {
                    if (i < parameterCount)
                    {
                        typeArguments[i + 1] = parameters[i].ParameterType;
                    }
                    else
                    {
                        typeArguments[i + 1] = objectType;
                    }
                }

                converterType = typeof(JsonObjectWithParameterizedConstructorConverter<,,,,,,,>).MakeGenericType(typeArguments);
                arguments = new object[] { constructor, options };
            }

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                converterType,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                arguments,
                culture: null)!;

            return converter;
        }

        private ConstructorInfo? GetDeserializationConstructor(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            if (constructors.Length == 1)
            {
                return constructors[0];
            }

            ConstructorInfo? parameterlessCtor = null;
            ConstructorInfo? ctorWithAttribute = null;

            foreach (ConstructorInfo constructor in constructors)
            {
                if (constructor.GetCustomAttribute<JsonConstructorAttribute>() != null)
                {
                    if (ctorWithAttribute != null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute<JsonConstructorAttribute>(type);
                    }

                    ctorWithAttribute = constructor;
                }
                else if (parameterlessCtor == null && constructor.GetParameters().Length == 0)
                {

                    parameterlessCtor = constructor;
                }
            }

            return ctorWithAttribute ?? parameterlessCtor;
        }
    }
}
