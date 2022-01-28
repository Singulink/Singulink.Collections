using System;
using System.Collections;
using System.Collections.Generic;

namespace Singulink.Collections
{
    /// <content>
    /// Contains the KeyCollection implementation for HashSetDictionary.
    /// </content>
    public partial class HashSetDictionary<TKey, TValue>
    {
        /// <summary>
        /// Represents the collection of keys in a <see cref="HashSetDictionary{TKey, TValue}"/>.
        /// </summary>
        public sealed class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
        {
            private readonly HashSetDictionary<TKey, TValue> _dictionary;

            internal KeyCollection(HashSetDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            /// <inheritdoc/>
            public int Count => _dictionary.Count;

            /// <summary>
            /// Returns an enumerator that iterates through the <see cref="KeyCollection"/>.
            /// </summary>
            public Enumerator GetEnumerator() => new(_dictionary);

            /// <inheritdoc/>
            bool ICollection<TKey>.IsReadOnly => true;

            /// <inheritdoc/>
            bool ICollection<TKey>.Contains(TKey item) => _dictionary.ContainsKey(item);

            /// <inheritdoc/>
            void ICollection<TKey>.CopyTo(TKey[] array, int index) => _dictionary._lookup.Keys.CopyTo(array, index);

            /// <inheritdoc/>
            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => GetEnumerator();

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// Not supported.
            /// </summary>
            void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

            /// <summary>
            /// Not supported.
            /// </summary>
            void ICollection<TKey>.Clear() => throw new NotSupportedException();

            /// <summary>
            /// Not supported.
            /// </summary>
            bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

            /// <summary>
            /// Enumerates the values of a <see cref="KeyCollection"/>.
            /// </summary>
            public struct Enumerator : IEnumerator<TKey>
            {
                private readonly HashSetDictionary<TKey, TValue> _dictionary;
                private readonly Dictionary<TKey, ValueSet>.KeyCollection.Enumerator _keyEnumerator;
                private readonly int _version;

                /// <inheritdoc/>
                public TKey Current => _keyEnumerator.Current;

                /// <inheritdoc/>
                object? IEnumerator.Current => Current;

                internal Enumerator(HashSetDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _keyEnumerator = dictionary._lookup.Keys.GetEnumerator();
                    _version = _dictionary._version;
                }

                /// <inheritdoc/>
                public void Dispose() => _keyEnumerator.Dispose();

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    _dictionary.CheckVersion(_version);
                    return _keyEnumerator.MoveNext();
                }

                /// <inheritdoc/>
                void IEnumerator.Reset() => ((IEnumerator)_keyEnumerator).Reset();
            }
        }
    }
}