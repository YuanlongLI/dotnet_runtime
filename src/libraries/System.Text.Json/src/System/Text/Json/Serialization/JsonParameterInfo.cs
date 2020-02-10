// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    [DebuggerDisplay("ParameterInfo={ParameterInfo}")]
    internal abstract class JsonParameterInfo
    {
        private Type _runtimePropertyType = null!;

        // The default value of the parameter. This is `DefaultValue` of the `ParameterInfo`, if specified, or the CLR `default` for the `ParameterType`.
        public object? DefaultValue { get; protected set; }

        // The name from a Json value. This is cached for performance on first deserialize.
        public byte[]? JsonPropertyName { get; set; }

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonClassInfo that is cached on JsonSerializerOptions.
        protected JsonSerializerOptions Options { get; set; } = null!; // initialized in Init method

        public ParameterInfo ParameterInfo { get; private set; } = null!;

        // The name of the parameter as UTF-8 bytes.
        public byte[] ParameterName { get; private set; } = null!;

        // The name of the parameter.
        public string NameAsString { get; private set; } = null!;

        // Key for fast property name lookup.
        public ulong ParameterNameKey { get; private set; }

        // The zero-based position of the parameter in the formal parameter list.
        public int Position { get; private set; }

        private JsonClassInfo? _runtimeClassInfo;
        public JsonClassInfo RuntimeClassInfo
        {
            get
            {
                if (_runtimeClassInfo == null)
                {
                    _runtimeClassInfo = Options.GetOrAddClass(_runtimePropertyType);
                }

                return _runtimeClassInfo;
            }
        }

        public virtual void Initialize(
            Type declaredPropertyType,
            Type runtimePropertyType,
            ParameterInfo parameterInfo,
            Type parentClassType,
            JsonConverter converter,
            ClassType classType,
            JsonSerializerOptions options)
        {
            _runtimePropertyType = runtimePropertyType;

            Options = options;

            ParameterInfo = parameterInfo;

            string name = ParameterInfo.Name!;
            // TODO: introduce constructor parameter naming policy
            //if (Options.PropertyNamingPolicy == null)
            //{
            if (name.Length > 0)
                {

#if BUILDING_INBOX_LIBRARY
                    NameAsString = string.Create(name.Length, name, (chars, name) =>
                    {
                        name.AsSpan().CopyTo(chars);
                        chars[0] = char.ToUpperInvariant(chars[0]);
                    });
#else
                    char[] chars = name.ToCharArray();
                    chars[0] = char.ToUpperInvariant(chars[0]);
                    NameAsString = new string(chars);
#endif
            }
            else
                {
                    NameAsString = name;
                }
            //}
            //else
            //{
            //    string name = Options.PropertyNamingPolicy.ConvertName(ParameterInfo.Name!);
            //    if (name == null)
            //    {
            //        ThrowHelper.ThrowInvalidOperationException_SerializerConstructorNameNull(parentClassType, this);
            //    }

            //    NameAsString = name;
            //}

            // `NameAsString` is valid UTF16, so just call the simple UTF16->UTF8 encoder.
            ParameterName = Encoding.UTF8.GetBytes(NameAsString);

            ParameterNameKey = JsonClassInfo.GetKey(ParameterName);

            Position = parameterInfo.Position;
        }

        public abstract bool ReadJson(ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options, out object? argument);
    }
}
