using System;
using System.Collections;
using System.Collections.Generic;

namespace Singulink.Collections
{
    /// <content>
    /// Contains the ValueListCollection implementation for ListDictionary.
    /// </content>
    public partial class ListDictionary<TKey, TValue>
    {
        /// <summary>
        /// Represents the collection of value lists in a <see cref="ListDictionary{TKey, TValue}"/>.
        /// </summary>
        public sealed class ValueListCollection : ICollection<ValueList>, IReadOnlyCollection<ValueList>
        {
            private readonly ListDictionary<TKey, TValue> _dictionary;

            internal ValueListCollection(ListDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            /// <inheritdoc/>
            public int Count => _dictionary.Count;

            /// <inheritdoc/>
            bool ICollection<ValueList>.IsReadOnly => true;

            /// <inheritdoc/>
            public bool Contains(ValueList item)
            {
                foreach (var valueList in _dictionary._lookup.Values) {
                    if (valueList == item)
                        return true;
                }

                return false;
            }

            /// <inheritdoc/>
            public void CopyTo(ValueList[] array, int index)
            {
                if ((uint)index > array.Length)
                    throw new IndexOutOfRangeException();

                if (array.Length - index < _dictionary.Count)
                    throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");

                int i = index;

                foreach (var valueList in _dictionary._lookup.Values)
                    array[i++] = valueList;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the value lists in a <see cref="ValueListCollection"/>.
            /// </summary>
            public Enumerator GetEnumerator() => new(_dictionary);

            /// <inheritdoc/>
            IEnumerator<ValueList> IEnumerable<ValueList>.GetEnumerator() => GetEnumerator();

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// Not supported.
            /// </summary>
            void ICollection<ValueList>.Add(ValueList item) => throw new NotSupportedException();

            /// <summary>
            /// Not supported.
            /// </summary>
            void ICollection<ValueList>.Clear() => throw new NotSupportedException();

            /// <summary>
            /// Not supported.
            /// </summary>
            bool ICollection<ValueList>.Remove(ValueList item) => throw new NotSupportedException();

            /// <summary>
            /// Enumerates the value lists of a <see cref="ValueListCollection"/>.
            /// </summary>
            public struct Enumerator : IEnumerator<ValueList>
            {
                private readonly ListDictionary<TKey, TValue> _dictionary;
                private readonly Dictionary<TKey, ValueList>.ValueCollection.Enumerator _valueListEnumerator;
                private readonly int _version;

                /// <inheritdoc/>
                public ValueList Current => _valueListEnumerator.Current;

                /// <inheritdoc/>
                object? IEnumerator.Current => Current;

                internal Enumerator(ListDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _valueListEnumerator = dictionary._lookup.Values.GetEnumerator();
                    _version = _dictionary._version;
                }

                /// <inheritdoc/>
                public void Dispose() => _valueListEnumerator.Dispose();

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    if (_version != _dictionary._version)
                        throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

                    return _valueListEnumerator.MoveNext();
                }

                /// <inheritdoc/>
                void IEnumerator.Reset() => ((IEnumerator)_valueListEnumerator).Reset();
            }
        }
    }
}