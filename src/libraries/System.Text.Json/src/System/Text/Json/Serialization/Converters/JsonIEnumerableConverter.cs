// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonIEnumerableConverter<TCollection> : JsonIEnumerableDefaultConverter<TCollection, object> where TCollection:IEnumerable
    {
        protected override void CreateCollection(ref ReadStack state)
        {
            Type collectionType = state.Current.JsonClassInfo.Type;

            // Perform quick check for IEnumerable and then slower check for compatibility with List<object>.
            if (collectionType != typeof(IEnumerable) && !collectionType.IsAssignableFrom(RuntimeType))
            {
                // A collection was specified that just implements IEnumerable; there's not a way to populate that.
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupportedCollection(collectionType);
            }

            state.Current.ReturnValue = new List<object>();
        }

        // Consider overriding ConvertCollection to convert the list to an array since a List is mutable.
        //  However, converting from the temporary list to an array is expensive.

        protected override void Add(object value, ref ReadStack state)
        {
            ((List<object>)state.Current.ReturnValue!).Add(value);
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

            JsonConverter<object> converter = GetElementConverter(ref state);
            do
            {
                if (!converter.TryWrite(writer, enumerator.Current!, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                state.Current.EndElement();
            } while (enumerator.MoveNext());

            return true;
        }

        internal override Type RuntimeType => typeof(List<object>);
    }
}
