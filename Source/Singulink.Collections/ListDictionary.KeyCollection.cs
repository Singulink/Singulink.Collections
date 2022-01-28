using System;
using System.Collections;
using System.Collections.Generic;

namespace Singulink.Collections
{
    /// <content>
    /// Contains the KeyCollection implementation for ListDictionary.
    /// </content>
    public partial class ListDictionary<TKey, TValue>
    {
        /// <summary>
        /// Represents the collection of keys in a <see cref="ListDictionary{TKey, TValue}"/>.
        /// </summary>
        public sealed class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
        {
            private readonly ListDictionary<TKey, TValue> _dictionary;

            internal KeyCollection(ListDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            /// <inheritdoc/>
            public int Count => _dictionary.Count;

            /// <inheritdoc/>
            bool ICollection<TKey>.IsReadOnly => true;

            /// <inheritdoc/>
            public bool Contains(TKey item) => _dictionary.ContainsKey(item);

            /// <inheritdoc/>
            public void CopyTo(TKey[] array, int index) => _dictionary._lookup.Keys.CopyTo(array, index);

            /// <summary>
            /// Returns an enumerator that iterates through the <see cref="KeyCollection"/>.
            /// </summary>
            public Enumerator GetEnumerator() => new(_dictionary);

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
                private readonly ListDictionary<TKey, TValue> _dictionary;
                private readonly Dictionary<TKey, ValueList>.KeyCollection.Enumerator _keyEnumerator;
                private readonly int _version;

                /// <inheritdoc/>
                public TKey Current => _keyEnumerator.Current;

                /// <inheritdoc/>
                object? IEnumerator.Current => Current;

                internal Enumerator(ListDictionary<TKey, TValue> dictionary)
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