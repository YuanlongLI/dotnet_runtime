﻿// Licensed to the .NET Foundation under one or more agreements.
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
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.LargeObjectWithParameterizedConstructorConverter`1")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.SmallObjectWithParameterizedConstructorConverter`5")]
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            JsonConverter converter;
            Type converterType;

            ConstructorInfo? constructor = GetDeserializationConstructor(typeToConvert);
            ParameterInfo[]? parameters = constructor?.GetParameters();

            if (constructor == null || typeToConvert.IsAbstract || parameters!.Length == 0)
            {
                converterType = typeof(JsonObjectDefaultConverter<>).MakeGenericType(typeToConvert);
            }
            else
            {
                int parameterCount = parameters.Length;

                if ((parameterCount <= JsonConstants.UnboxedParameterCountThreshold)
                    && (GetNumberOfValueTypeProperties(typeToConvert, constructor) < JsonConstants.ValueTypePropertyCountThreshold))
                {
                    Type placeHolderType = typeof(object);
                    Type[] typeArguments = new Type[JsonConstants.UnboxedParameterCountThreshold + 1];

                    typeArguments[0] = typeToConvert;
                    for (int i = 0; i < JsonConstants.UnboxedParameterCountThreshold; i++)
                    {
                        if (i < parameterCount)
                        {
                            typeArguments[i + 1] = parameters[i].ParameterType;
                        }
                        else
                        {
                            // Use placeholder arguments if there are less args than the threshold.
                            typeArguments[i + 1] = placeHolderType;
                        }
                    }

                    converterType = typeof(SmallObjectWithParameterizedConstructorConverter<,,,,>).MakeGenericType(typeArguments);
                }
                else
                {
                    converterType = typeof(LargeObjectWithParameterizedConstructorConverter<>).MakeGenericType(typeToConvert);
                }
            }

            converter = (JsonConverter)Activator.CreateInstance(
                    converterType,
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: null,
                    culture: null)!;

            // This body for this method implemented by ObjectDefaultConverter<> is empty.
            converter.Initialize(constructor!, options);

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

        // Gives a naive estimate of the number of value type properties we'll deserialize directly into.
        // We'll box them if we do deserialization in one pass and temporarily cache properties as we
        // parse them while searching for
        // We only care about this for objects that have parameterized constructors.
        private int GetNumberOfValueTypeProperties(Type type, ConstructorInfo constructor)
        {
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            ParameterInfo[] parameters = constructor.GetParameters();

            int numValueTypes = 0;

            foreach (PropertyInfo property in properties)
            {
                if (property.GetIndexParameters().Length == 0 && property.PropertyType.IsValueType)
                {
                    numValueTypes++;
                }
            }

            foreach (ParameterInfo parameter in parameters)
            {
                if (parameter.ParameterType.IsValueType)
                {
                    numValueTypes--;
                }
            }

            // A negative return value is fine here.
            return numValueTypes;
        }
    }
}
