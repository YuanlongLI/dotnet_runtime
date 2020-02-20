// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    [DebuggerDisplay("ClassType.{JsonClassInfo.ClassType}, {JsonClassInfo.Type.Name}")]
    internal struct ReadStackFrame
    {
        // Current property values.
        public JsonPropertyInfo JsonPropertyInfo;
        public StackFramePropertyState PropertyState;
        public bool UseExtensionProperty;

        // Support JSON Path on exceptions.
        public byte[]? JsonPropertyName; // This is Utf8 since we don't want to convert to string until an exception is thown.
        public string? JsonPropertyNameAsString; // This is used for dictionary keys, re-entry cases and edge cases with reference handling.

        // Validation state.
        public int OriginalDepth;
        public JsonTokenType OriginalTokenType;

        // Current object (POCO or IEnumerable).
        public object? ReturnValue; // The current return value used for re-entry.
        public JsonClassInfo JsonClassInfo;
        public StackFrameObjectState ObjectState; // State tracking the current object.

        // Preserve reference.
        public string? MetadataId;

        // For performance, we order the properties by the first deserialize and PropertyIndex helps find the right slot quicker.
        public int PropertyIndex;
        public List<PropertyRef>? PropertyRefCache;

        // Add method delegate for Non-generic Stack and Queue; and types that derive from them.
        public object? AddMethodDelegate;

        // Cached values for objects created with parameterized constructors
        public JsonParameterInfo? JsonConstructorParameterInfo;

        public int ConstructorParameterIndex;
        public List<ParameterRef>? ParameterRefCache;

        public bool[]? ConstructorArgumentState;
        public object[]? ConstructorArguments;
        public Dictionary<JsonPropertyInfo, object?>? PropertyValues;

        public bool ExtensionDataIsObject;
        public IDictionary<string, object>? ObjectExtensionData;
        public IDictionary<string, JsonElement>? JsonElementExtensionData;

        public void InitializeObjectWithParameterizedConstructor(
            Dictionary<string, JsonParameterInfo> parameterCache,
            JsonPropertyInfo? dataExtensionProperty)
        {
            // Initialize temporary property value cache.
            PropertyValues = new Dictionary<JsonPropertyInfo, object?>();

            // Initialize temporary extension data cache.
            InitializeExtensionDataCache(dataExtensionProperty);

            // Initialize temporary constructor argument cache.
            InitializeConstructorArgumentCache(parameterCache);

            ConstructorArgumentState = ArrayPool<bool>.Shared.Rent(parameterCache.Count);
        }

        private void InitializeExtensionDataCache(JsonPropertyInfo? dataExtensionProperty)
        {
            if (dataExtensionProperty != null)
            {
                Type underlyingIDictionaryType = dataExtensionProperty.DeclaredPropertyType.GetCompatibleGenericInterface(typeof(IDictionary<,>))!;
                Debug.Assert(underlyingIDictionaryType.IsGenericType);
                Debug.Assert(underlyingIDictionaryType.GetGenericArguments().Length == 2);
                Debug.Assert(underlyingIDictionaryType.GetGenericArguments()[0].UnderlyingSystemType == typeof(string));
                Debug.Assert(
                    underlyingIDictionaryType.GetGenericArguments()[1].UnderlyingSystemType == typeof(object) ||
                    underlyingIDictionaryType.GetGenericArguments()[1].UnderlyingSystemType == typeof(JsonElement));

                JsonClassInfo.ConstructorDelegate createObject = dataExtensionProperty.RuntimeClassInfo.CreateObject!;

                if (underlyingIDictionaryType.GetGenericArguments()[1] == typeof(object))
                {
                    ExtensionDataIsObject = true;
                    ObjectExtensionData = (IDictionary<string, object>)createObject()!;
                }
                else
                {
                    JsonElementExtensionData = (IDictionary<string, JsonElement>)createObject()!;
                }
            }
        }

        private void InitializeConstructorArgumentCache(Dictionary<string, JsonParameterInfo> parameterCache)
        {
            //ConstructorArguments = new object[parameterCache.Count];
            // We know that this will be a maximum of 64 items.
            ConstructorArguments = ArrayPool<object>.Shared.Rent(parameterCache.Count);

            foreach (JsonParameterInfo parameterInfo in parameterCache.Values)
            {
                ConstructorArguments[parameterInfo.Position] = parameterInfo.DefaultValue!;
            }
        }

        public void EndConstructorParameter()
        {
            JsonConstructorParameterInfo = null;
        }

        public void EndProperty()
        {
            JsonPropertyInfo = null!;
            JsonPropertyName = null;
            JsonPropertyNameAsString = null;
            PropertyState = StackFramePropertyState.None;
            MetadataId = null;

            // No need to clear these since they are overwritten each time:
            //  UseExtensionProperty
        }

        public void EndElement()
        {
            JsonPropertyNameAsString = null;
            PropertyState = StackFramePropertyState.None;
        }

        public void InitializeReEntry(Type type, JsonSerializerOptions options, string? propertyName)
        {
            JsonClassInfo jsonClassInfo = options.GetOrAddClass(type);
            Debug.Assert(jsonClassInfo.ClassType != ClassType.Invalid);

            // The initial JsonPropertyInfo will be used to obtain the converter.
            JsonPropertyInfo = jsonClassInfo.PolicyProperty!;

            // Set for exception handling calculation of JsonPath.
            JsonPropertyNameAsString = propertyName;
        }

        /// <summary>
        /// Is the current object a Dictionary.
        /// </summary>
        public bool IsProcessingDictionary()
        {
            return (JsonClassInfo.ClassType & ClassType.Dictionary) != 0;
        }

        /// <summary>
        /// Is the current object an Enumerable.
        /// </summary>
        public bool IsProcessingEnumerable()
        {
            return (JsonClassInfo.ClassType & ClassType.Enumerable) != 0;
        }

        public void Reset()
        {
            AddMethodDelegate = null;
            ConstructorParameterIndex = 0;
            JsonClassInfo = null!;
            ObjectState = StackFrameObjectState.None;
            OriginalDepth = 0;
            OriginalTokenType = JsonTokenType.None;
            PropertyIndex = 0;
            PropertyRefCache = null;
            ReturnValue = null;

            EndProperty();
        }
    }
}
