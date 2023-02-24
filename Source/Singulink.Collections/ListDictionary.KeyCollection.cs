using System.Collections;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

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

        /// <summary>
        /// Gets the number of keys in this collection.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <inheritdoc cref="ListDictionary{TKey, TValue}.ContainsKey(TKey)"/>
        public bool Contains(TKey item) => _dictionary.ContainsKey(item);

        /// <summary>
        /// Copies all the keys in the dictionary to an array starting at the specified array index.
        /// </summary>
        public void CopyTo(TKey[] array, int index) => _dictionary._lookup.Keys.CopyTo(array, index);

        /// <summary>
        /// Returns an enumerator that iterates through the keys in this collection.
        /// </summary>
        public Enumerator GetEnumerator() => new(_dictionary);

        #region Explicit Interface Implementations

        /// <summary>
        /// Gets a value indicating whether this collection is read-only. Always returns <see langword="true"/>.
        /// </summary>
        bool ICollection<TKey>.IsReadOnly => true;

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Not Supported

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<TKey>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// Enumerates the values of a <see cref="KeyCollection"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TKey>
        {
            private readonly ListDictionary<TKey, TValue> _dictionary;
            private readonly int _version;

            private Dictionary<TKey, ValueList>.KeyCollection.Enumerator _keysEnumerator;

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public TKey Current => _keysEnumerator.Current;

            /// <inheritdoc cref="Current"/>
            object? IEnumerator.Current => Current;

            internal Enumerator(ListDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = _dictionary._version;

                _keysEnumerator = dictionary._lookup.Keys.GetEnumerator();
            }

            /// <summary>
            /// Releases all the resources used by the enumerator.
            /// </summary>
            public void Dispose() => _keysEnumerator.Dispose();

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            public bool MoveNext()
            {
                Throw.IfEnumeratedCollectionChanged(_version, _dictionary._version);
                return _keysEnumerator.MoveNext();
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            /// <exception cref="NotSupportedException">This operation is not supported.</exception>
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}