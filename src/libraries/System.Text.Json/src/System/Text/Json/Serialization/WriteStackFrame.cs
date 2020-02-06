﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    [DebuggerDisplay("ClassType.{JsonClassInfo.ClassType}, {JsonClassInfo.Type.Name}")]
    internal struct WriteStackFrame
    {
        /// <summary>
        /// The enumerator for resumable collections.
        /// </summary>
        public IEnumerator? CollectionEnumerator;

        /// <summary>
        /// The original JsonPropertyInfo that is not changed. It contains all properties.
        /// </summary>
        /// <remarks>
        /// For objects, it is either the policy property for the class or the current property.
        /// For collections, it is either the policy property for the class or the policy property for the current element.
        /// </remarks>
        public JsonPropertyInfo DeclaredJsonPropertyInfo;

        /// <summary>
        /// Used when processing dictionaries.
        /// </summary>
        public bool IgnoreDictionaryKeyPolicy;

        /// <summary>
        /// The class (POCO or IEnumerable) that is being populated.
        /// </summary>
        public JsonClassInfo JsonClassInfo;

        /// <summary>
        /// The key name for a dictionary value.
        /// </summary>
        public string KeyName;

        /// <summary>
        /// Validation state for a class.
        /// </summary>
        public int OriginalDepth;

        // Class-level state for collections.
        public bool ProcessedStartToken;
        public bool ProcessedEndToken;

        /// <summary>
        /// Property or Element state.
        /// </summary>
        public StackFramePropertyState PropertyState;

        /// <summary>
        /// The enumerator index for resumable collections.
        /// </summary>
        public int EnumeratorIndex;

        // This is used for re-entry cases for exception handling.
        public string? JsonPropertyNameAsString;

        // Preserve Reference
        public MetadataPropertyName MetadataPropertyName;

        /// <summary>
        /// The run-time JsonPropertyInfo that contains the ClassInfo and ConverterBase for polymorphic scenarios.
        /// </summary>
        /// <remarks>
        /// For objects, it is either the policy property for the class or the policy property for the current property.
        /// For collections, it is either the policy property for the class or the policy property for the current element.
        /// </remarks>
        public JsonPropertyInfo? PolymorphicJsonPropertyInfo;

        public void EndDictionaryElement()
        {
            PropertyState = StackFramePropertyState.None;
        }

        public void EndProperty()
        {
            DeclaredJsonPropertyInfo = null!;
            JsonPropertyNameAsString = null;
            KeyName = null!;
            PolymorphicJsonPropertyInfo = null;
            PropertyState = StackFramePropertyState.None;
        }

        /// <summary>
        /// Return the property that contains the correct polymorphic properties including
        /// the ClassType and ConverterBase.
        /// </summary>
        /// <returns></returns>
        public JsonPropertyInfo GetPolymorphicJsonPropertyInfo()
        {
            return PolymorphicJsonPropertyInfo != null ? PolymorphicJsonPropertyInfo : DeclaredJsonPropertyInfo;
        }

        /// <summary>
        /// Initializes the state for polymorphic or re-entry cases.
        /// </summary>
        public JsonConverter InitializeReEntry(Type type, JsonSerializerOptions options, string? propertyName = null)
        {
            JsonClassInfo newClassInfo = options.GetOrAddClass(type);
            if (newClassInfo.ClassType == ClassType.Invalid)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupportedCollection(type);
            }

            // todo: check if type==newtype and skip below?

            // Set for exception handling calculation of JsonPath.
            JsonPropertyNameAsString = propertyName;

            PolymorphicJsonPropertyInfo = newClassInfo.PolicyProperty!;
            return PolymorphicJsonPropertyInfo.ConverterBase;
        }

        public void Reset()
        {
            CollectionEnumerator = null;
            EnumeratorIndex = 0;
            IgnoreDictionaryKeyPolicy = false;
            JsonClassInfo = null!;
            OriginalDepth = 0;
            ProcessedStartToken = false;
            ProcessedEndToken = false;

            EndProperty();
        }
    }
}
