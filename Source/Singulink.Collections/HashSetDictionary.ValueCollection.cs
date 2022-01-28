using System;
using System.Collections;
using System.Collections.Generic;

namespace Singulink.Collections
{
    /// <content>
    /// Contains the ValueCollection implementation for HashSetDictionary.
    /// </content>
    public partial class HashSetDictionary<TKey, TValue>
    {
        /// <summary>
        /// Represents the collection of values in a <see cref="HashSetDictionary{TKey, TValue}"/>.
        /// </summary>
        public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
        {
            private readonly HashSetDictionary<TKey, TValue> _dictionary;

            internal ValueCollection(HashSetDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            /// <inheritdoc/>
            public int Count => _dictionary.ValueCount;

            /// <summary>
            /// Returns an enumerator that iterates through the <see cref="ValueCollection"/>.
            /// </summary>
            public Enumerator GetEnumerator() => new(_dictionary);

            /// <inheritdoc/>
            bool ICollection<TValue>.IsReadOnly => true;

            /// <inheritdoc/>
            bool ICollection<TValue>.Contains(TValue item) => _dictionary.ContainsValue(item);

            /// <inheritdoc/>
            void ICollection<TValue>.CopyTo(TValue[] array, int index)
            {
                if ((uint)index > array.Length)
                    throw new IndexOutOfRangeException();

                if (array.Length - index < _dictionary.ValueCount)
                    throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");

                int i = index;

                foreach (var valueSet in _dictionary._lookup.Values) {
                    var set = valueSet.WrappedSet;
                    set.CopyTo(array, i);
                    i += set.Count;
                }
            }

            /// <inheritdoc/>
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// Not supported.
            /// </summary>
            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

            /// <summary>
            /// Not supported.
            /// </summary>
            void ICollection<TValue>.Clear() => throw new NotSupportedException();

            /// <summary>
            /// Not supported.
            /// </summary>
            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

            /// <summary>
            /// Enumerates the values of a <see cref="ValueCollection"/>.
            /// </summary>
            public struct Enumerator : IEnumerator<TValue>
            {
                private readonly HashSetDictionary<TKey, TValue> _dictionary;
                private readonly Dictionary<TKey, ValueSet>.ValueCollection.Enumerator _valueSetsEnumerator;
                private HashSet<TValue>.Enumerator _valuesEnumerator;
                private readonly int _version;
                private bool _started;

                /// <inheritdoc/>
                public TValue Current => _valuesEnumerator.Current;

                /// <inheritdoc/>
                object? IEnumerator.Current => Current;

                internal Enumerator(HashSetDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _valueSetsEnumerator = dictionary._lookup.Values.GetEnumerator();
                    _valuesEnumerator = default;
                    _version = _dictionary._version;
                    _started = false;
                }

                /// <inheritdoc/>
                public void Dispose()
                {
                    _valueSetsEnumerator.Dispose();
                    _valuesEnumerator.Dispose();
                }

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    _dictionary.CheckVersion(_version);

                    if (!_started) {
                        if (!_valueSetsEnumerator.MoveNext())
                            return false;

                        _valuesEnumerator = _valueSetsEnumerator.Current.GetEnumerator();
                        _valuesEnumerator.MoveNext();
                        _started = true;
                        return true;
                    }

                    if (_valuesEnumerator.MoveNext())
                        return true;

                    if (!_valueSetsEnumerator.MoveNext())
                        return false;

                    _valuesEnumerator = _valueSetsEnumerator.Current.GetEnumerator();
                    _valuesEnumerator.MoveNext();
                    return true;
                }

                /// <inheritdoc/>
                void IEnumerator.Reset()
                {
                    ((IEnumerator)_valueSetsEnumerator).Reset();
                    _valuesEnumerator = default;
                    _started = false;
                }
            }
        }
    }
}