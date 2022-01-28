using System;
using System.Collections;
using System.Collections.Generic;

namespace Singulink.Collections
{
    /// <content>
    /// Contains the ValueSetCollection implementation for HashSetDictionary.
    /// </content>
    public partial class HashSetDictionary<TKey, TValue>
    {
        /// <summary>
        /// Represents the collection of value sets in a <see cref="HashSetDictionary{TKey, TValue}"/>.
        /// </summary>
        public sealed class ValueSetCollection : ICollection<ValueSet>, IReadOnlyCollection<ValueSet>
        {
            private readonly HashSetDictionary<TKey, TValue> _dictionary;

            internal ValueSetCollection(HashSetDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            /// <inheritdoc/>
            public int Count => _dictionary.Count;

            /// <summary>
            /// Returns an enumerator that iterates through the value sets in a <see cref="ValueSetCollection"/>.
            /// </summary>
            public Enumerator GetEnumerator() => new(_dictionary);

            /// <inheritdoc/>
            bool ICollection<ValueSet>.IsReadOnly => true;

            /// <inheritdoc/>
            bool ICollection<ValueSet>.Contains(ValueSet item)
            {
                foreach (var valueSets in _dictionary._lookup.Values) {
                    if (valueSets == item)
                        return true;
                }

                return false;
            }

            /// <inheritdoc/>
            void ICollection<ValueSet>.CopyTo(ValueSet[] array, int index)
            {
                if ((uint)index > array.Length)
                    throw new IndexOutOfRangeException();

                if (array.Length - index < _dictionary.Count)
                    throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");

                int i = index;

                foreach (var valueSet in _dictionary._lookup.Values)
                    array[i++] = valueSet;
            }

            /// <inheritdoc/>
            IEnumerator<ValueSet> IEnumerable<ValueSet>.GetEnumerator() => GetEnumerator();

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// Not supported.
            /// </summary>
            void ICollection<ValueSet>.Add(ValueSet item) => throw new NotSupportedException();

            /// <summary>
            /// Not supported.
            /// </summary>
            void ICollection<ValueSet>.Clear() => throw new NotSupportedException();

            /// <summary>
            /// Not supported.
            /// </summary>
            bool ICollection<ValueSet>.Remove(ValueSet item) => throw new NotSupportedException();

            /// <summary>
            /// Enumerates the value sets of a <see cref="ValueSetCollection"/>.
            /// </summary>
            public struct Enumerator : IEnumerator<ValueSet>
            {
                private readonly HashSetDictionary<TKey, TValue> _dictionary;
                private readonly Dictionary<TKey, ValueSet>.ValueCollection.Enumerator _valueSetsEnumerator;
                private readonly int _version;

                /// <inheritdoc/>
                public ValueSet Current => _valueSetsEnumerator.Current;

                /// <inheritdoc/>
                object? IEnumerator.Current => Current;

                internal Enumerator(HashSetDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _valueSetsEnumerator = dictionary._lookup.Values.GetEnumerator();
                    _version = _dictionary._version;
                }

                /// <inheritdoc/>
                public void Dispose() => _valueSetsEnumerator.Dispose();

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    _dictionary.CheckVersion(_version);
                    return _valueSetsEnumerator.MoveNext();
                }

                /// <inheritdoc/>
                void IEnumerator.Reset() => ((IEnumerator)_valueSetsEnumerator).Reset();
            }
        }
    }
}