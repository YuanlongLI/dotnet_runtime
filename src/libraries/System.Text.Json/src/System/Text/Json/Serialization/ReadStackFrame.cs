// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    [DebuggerDisplay("ClassType.{JsonClassInfo.ClassType}, {JsonClassInfo.Type.Name}")]
    internal struct ReadStackFrame
    {
        public StackFramePropertyState ProcessedPropertyState;

        // Object state.
        public bool ProcessedStartToken;
        public bool ProcessedEndToken;

        public bool UseExtensionProperty;

        // The object (POCO or IEnumerable) that is being populated
        public object? ReturnValue;
        public JsonClassInfo JsonClassInfo;

        // Support Dictionary keys.
        public string? KeyName;

        // Support JSON Path on exceptions.
        public byte[]? JsonPropertyName; // This is Utf8 since we don't want to convert to string until an exception is thown.
        public string? JsonPropertyNameAsString; // This is used for re-entry cases.

        // Current property values.
        public JsonPropertyInfo JsonPropertyInfo;

        // Validation state.
        public int OriginalDepth;
        public int OriginalPropertyDepth;
        public long OriginalPropertyBytesConsumed;
        public JsonTokenType OriginalTokenType;
        public JsonTokenType OriginalPropertyTokenType;

        // For performance, we order the properties by the first deserialize and PropertyIndex helps find the right slot quicker.
        public int PropertyIndex;
        public List<PropertyRef>? PropertyRefCache;

        public void InitializeReEntry(Type type, JsonSerializerOptions options, string? propertyName)
        {
            JsonClassInfo jsonClassInfo = options.GetOrAddClass(type);
            if (jsonClassInfo.ClassType == ClassType.Invalid)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupportedCollection(type);
            }

            // The initial JsonPropertyInfo will be used to obtain the converter.
            JsonPropertyInfo = jsonClassInfo.PolicyProperty!;

            // Set for exception handling calculation of JsonPath.
            JsonPropertyNameAsString = propertyName;
        }

        public void InitializeRoot(Type type, JsonSerializerOptions options)
        {
            JsonClassInfo = options.GetOrAddClass(type);
            if (JsonClassInfo.ClassType == ClassType.Invalid)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupportedCollection(type);
            }

            // The initial JsonPropertyInfo will be used to obtain the converter.
            JsonPropertyInfo = JsonClassInfo.PolicyProperty!;
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
            JsonClassInfo = null!;
            PropertyRefCache = null;
            ReturnValue = null;
            OriginalDepth = 0;
            OriginalTokenType = JsonTokenType.None;
            PropertyIndex = 0;
            ProcessedStartToken = false;
            ProcessedEndToken = false;

            EndProperty();
        }

        public void EndProperty()
        {
            ProcessedPropertyState = StackFramePropertyState.None;

            // No need to clear these since they are overwritten each time:
            //JsonPropertyInfo = null;
            //UseExtensionProperty = false;
            //OriginalPropertyDepth = 0;
            //OriginalPropertyBytesConsumed = 0;
            //OriginalPropertyTokenType = JsonTokenType.None;

            // Don't clear JsonPropertyName or JsonPropertyNameAsString since they are used for JsonPath in exception cases.
        }

        public void EndElement()
        {
            OriginalPropertyDepth = 0;
            OriginalPropertyBytesConsumed = 0;
            OriginalPropertyTokenType = JsonTokenType.None;
            ProcessedPropertyState = StackFramePropertyState.None;

            // Don't clear KeyName as it is used in Path for exception cases.
        }
    }
}
