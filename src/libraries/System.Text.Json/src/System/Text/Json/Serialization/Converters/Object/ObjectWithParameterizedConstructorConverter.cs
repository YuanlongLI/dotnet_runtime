// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal abstract partial class ObjectWithParameterizedConstructorConverter<TypeToConvert> : JsonObjectConverter<TypeToConvert>
    {
        // All of the serializable properties on a POCO (except the optional extension property) keyed on property name.
        protected volatile Dictionary<string, JsonPropertyInfo> PropertyCache = null!;

        // All of the serializable properties on a POCO including the optional extension property.
        // Used for performance during serialization instead of 'PropertyCache' above.
        private volatile JsonPropertyInfo[]? _propertyCacheArray;

        protected JsonPropertyInfo? DataExtensionProperty;

        protected volatile Dictionary<string, JsonParameterInfo> ParameterCache = null!;

        protected int ParameterCount { get; private set; }

        internal override void Initialize(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            // The parameter cache must be before the property caches.
            CreateParameterCache(constructor, options);

            CreatePropertyCaches(options);
        }

        private void CreateParameterCache(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            ParameterInfo[] parameters = constructor.GetParameters();

            ParameterCount = parameters.Length;

            // TODO: honor options.ParameterCaseInsensitvity
            var cache = new Dictionary<string, JsonParameterInfo>(ParameterCount);
            foreach (ParameterInfo parameter in parameters)
            {
                JsonParameterInfo jsonParameterInfo = AddConstructorParameterProperty(parameter.ParameterType, parameter, base.TypeToConvert, options);
                if (!JsonHelpers.TryAdd(cache, jsonParameterInfo.NameAsString, jsonParameterInfo))
                {
                    // TODO: Add test for this; use throw helper. This could be many-to-one naming policy.
                    throw new InvalidOperationException();
                }
            }

            // Set field when finished to avoid concurrency issues.
            ParameterCache = cache;
        }

        private JsonParameterInfo AddConstructorParameterProperty(Type parameterType, ParameterInfo parameterInfo, Type parentType, JsonSerializerOptions options)
        {
            ClassType classType = JsonClassInfo.GetClassType(
                parameterType,
                parentType,
                propertyInfo: null,
                out Type? runtimeType,
                out Type? _,
                out JsonConverter? converter,
                options);

            if (converter == null)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(parameterType, parentType, parameterInfo);
            }

            return CreateConstructorParameter(
                declaredParameterType: parameterType,
                runtimeParameterType: runtimeType!,
                parameterInfo,
                parentType,
                converter,
                classType,
                options);
        }

        private static JsonParameterInfo CreateConstructorParameter(
            Type declaredParameterType,
            Type runtimeParameterType,
            ParameterInfo parameterInfo,
            Type parentType,
            JsonConverter converter,
            ClassType classType,
            JsonSerializerOptions options)
        {
            // Create the JsonParameterInfo instance.
            JsonParameterInfo jsonParameterInfo = converter.CreateJsonParameterInfo();

            jsonParameterInfo.Initialize(
                declaredParameterType,
                runtimeParameterType,
                parameterInfo,
                parentClassType: parentType,
                converter,
                classType,
                options);

            return jsonParameterInfo;
        }

        public Dictionary<string, JsonPropertyInfo> CreatePropertyCache(int capacity, JsonSerializerOptions options)
        {
            if (options.PropertyNameCaseInsensitive)
            {
                return new Dictionary<string, JsonPropertyInfo>(capacity, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                return new Dictionary<string, JsonPropertyInfo>(capacity);
            }
        }

        private void CreatePropertyCaches(JsonSerializerOptions options)
        {
            PropertyInfo[] properties = base.TypeToConvert.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            Dictionary<string, JsonPropertyInfo> propertyCache = CreatePropertyCache(properties.Length, options);
            Dictionary<string, JsonPropertyInfo> parameterMatches = new Dictionary<string, JsonPropertyInfo>(ParameterCount);
            Dictionary<string, JsonPropertyInfo> cacheToPopulate;

            foreach (PropertyInfo propertyInfo in properties)
            {
                // Ignore indexers
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                // For now we only support public getters\setters
                if (propertyInfo.GetMethod?.IsPublic == true ||
                    propertyInfo.SetMethod?.IsPublic == true)
                {
                    JsonPropertyInfo jsonPropertyInfo = JsonClassInfo.AddProperty(propertyInfo.PropertyType, propertyInfo, base.TypeToConvert, options);
                    Debug.Assert(jsonPropertyInfo != null && jsonPropertyInfo.NameAsString != null);

                    // If there is a case-insensitive match, this means that the property will probably be assigned through the constructor.
                    // We want to put this property at the front of the queue to be serialized, so we can get a quicker match on deserialization.
                    cacheToPopulate = ParameterCache.ContainsKey(jsonPropertyInfo.NameAsString) ? parameterMatches : propertyCache;

                    // If the JsonPropertyNameAttribute or naming policy results in collisions, throw an exception.
                    if (!JsonHelpers.TryAdd(cacheToPopulate, jsonPropertyInfo.NameAsString, jsonPropertyInfo))
                    {
                        JsonPropertyInfo other = cacheToPopulate[jsonPropertyInfo.NameAsString];

                        if (other.ShouldDeserialize == false && other.ShouldSerialize == false)
                        {
                            // Overwrite the one just added since it has [JsonIgnore].
                            cacheToPopulate[jsonPropertyInfo.NameAsString] = jsonPropertyInfo;
                        }
                        else if (jsonPropertyInfo.ShouldDeserialize == true || jsonPropertyInfo.ShouldSerialize == true)
                        {
                            ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(base.TypeToConvert, jsonPropertyInfo);
                        }
                        // else ignore jsonPropertyInfo since it has [JsonIgnore].
                    }
                }
            }

            JsonPropertyInfo[] cacheArray;

            JsonPropertyInfo? dataExtensionProperty;
            if (JsonClassInfo.TryDetermineExtensionDataProperty(base.TypeToConvert, propertyCache, options, out dataExtensionProperty))
            {
                Debug.Assert(dataExtensionProperty != null);
                DataExtensionProperty = dataExtensionProperty;

                // Remove from propertyCache since it is handled independently.
                propertyCache.Remove(DataExtensionProperty.NameAsString!);

                int propertyCount = propertyCache.Count + parameterMatches.Count;
                cacheArray = new JsonPropertyInfo[propertyCount + 1];

                // Set the last element to the extension property.
                cacheArray[propertyCount] = DataExtensionProperty;
            }
            else if (JsonClassInfo.TryDetermineExtensionDataProperty(base.TypeToConvert, parameterMatches, options, out dataExtensionProperty))
            {
                Debug.Assert(dataExtensionProperty != null);
                DataExtensionProperty = dataExtensionProperty;

                // Remove from parameterCache since it is handled independently.
                ParameterCache.Remove(DataExtensionProperty.NameAsString!);

                int propertyCount = propertyCache.Count + parameterMatches.Count;
                cacheArray = new JsonPropertyInfo[propertyCount + 1];

                // Set the last element to the extension property.
                cacheArray[propertyCount] = DataExtensionProperty;
            }
            else
            {
                cacheArray = new JsonPropertyInfo[propertyCache.Count + parameterMatches.Count];
            }

            parameterMatches.Values.CopyTo(cacheArray, 0);
            propertyCache.Values.CopyTo(cacheArray, parameterMatches.Count);

            foreach (KeyValuePair<string, JsonPropertyInfo> pair in parameterMatches)
            {
                propertyCache.Add(pair.Key, pair.Value);
            }

            PropertyCache = propertyCache;
            _propertyCacheArray = cacheArray;
        }

        internal override bool ConstructorIsParameterized => true;
    }
}
