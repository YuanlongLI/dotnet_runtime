// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    /// Converter for <cref>System.Collections.IList</cref>.
    internal sealed class JsonIListConverter<TCollection> : JsonIEnumerableDefaultConverter<TCollection, object>
        where TCollection : IList
    {
        protected override void Add(object value, ref ReadStack state)
        {
            ((IList)state.Current.ReturnValue!).Add(value);
        }

        protected override void CreateCollection(ref ReadStack state)
        {
            JsonClassInfo classInfo = state.Current.JsonClassInfo;
            Type type = state.Current.JsonClassInfo.Type;
            if (type.IsAbstract || type.IsInterface)
            {
                state.Current.ReturnValue = new List<object>();
            }
            else
            {
                state.Current.ReturnValue = classInfo.CreateObject!();
            }
        }

        protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            IEnumerator enumerator;
            if (state.Current.CollectionEnumerator == null)
            {
                enumerator = value.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    return true;
                }
            }
            else
            {
                enumerator = state.Current.CollectionEnumerator;
            }

            JsonConverter<object> converter = JsonSerializerOptions.GetObjectConverter();
            do
            {
                object? element = enumerator.Current;

                if (!converter.TryWrite(writer, element!, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                state.Current.EndElement();
            } while (enumerator.MoveNext());

            return true;
        }

        internal override Type RuntimeType
        {
            get
            {
                if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
                {
                    return typeof(List<object>);
                }

                return TypeToConvert;
            }
        }
    }
}
