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
        public JsonPropertyInfo? JsonPropertyInfo;
        public StackFramePropertyState PropertyState;
        public bool UseExtensionProperty;

        // Support JSON Path on exceptions.
        public byte[]? JsonPropertyName; // This is Utf8 since we don't want to convert to string until an exception is thown.
        public string? JsonPropertyNameAsString; // This is used for dictionary keys and re-entry cases that specify a property name.

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

        public JsonParameterInfo? JsonConstructorParameterInfo;

        public int ConstructorParameterIndex;
        public List<ParameterRef>? ParameterRefCache;

        // Cache for parsed constructor arguments.
        public object? ConstructorArguments;

        // Data extension for objects with parameterized ctors and an extension data property.
        public object? DataExtension;

        // When deserializing objects with parameterized ctors, the properties we found on the first pass.
        public ValueTuple<JsonPropertyInfo, JsonReaderState, long, byte[]?>[]? FoundProperties;

        // When deserializing objects with parameterized ctors asynchronously, the properties we found on the first pass.
        public ValueTuple<JsonPropertyInfo, JsonReaderState, byte[], byte[]?>[]? FoundPropertiesAsync;

        public int FoundPropertyCount;

        public JsonReaderState ReaderState;

        public void EndConstructorParameter()
        {
            JsonConstructorParameterInfo = null;
            JsonPropertyName = null;
            PropertyState = StackFramePropertyState.None;
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

            // The initial JsonPropertyInfo will be used to obtain the converter.
            JsonPropertyInfo = jsonClassInfo.PolicyProperty;

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
            ConstructorArguments = null;
            DataExtension = null;
            FoundProperties = null;
            FoundPropertiesAsync = null;
            FoundPropertyCount = 0;
            JsonClassInfo = null!;
            ObjectState = StackFrameObjectState.None;
            OriginalDepth = 0;
            OriginalTokenType = JsonTokenType.None;
            ParameterRefCache = null;
            PropertyIndex = 0;
            PropertyRefCache = null;
            ReturnValue = null;

            EndConstructorParameter();
            EndProperty();
        }
    }
}
