using System.Collections;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <content>
/// Contains the ValueSetCollection implementation for HashSetDictionary.
/// </content>
public partial class HashSetDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents the collection of value sets in a <see cref="HashSetDictionary{TKey, TValue}"/>.
    /// </summary>
    public sealed partial class ValueSetCollection : ICollection<ValueSet>, IReadOnlyCollection<ValueSet>
    {
        private readonly HashSetDictionary<TKey, TValue> _dictionary;

        internal ValueSetCollection(HashSetDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        /// <summary>
        /// Gets the number of value sets in this collection.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Gets the <see cref="HashSetDictionary{TKey, TValue}"/> this collection belongs to.
        /// </summary>
        public HashSetDictionary<TKey, TValue> Dictionary => _dictionary;

        /// <summary>
        /// Returns a value indicating whether this collection contains the given value set.
        /// </summary>
        public bool Contains(ValueSet valueSet) => valueSet.Count > 0 && valueSet.Dictionary == _dictionary;

        /// <summary>
        /// Copies the value sets in this collection to an array starting at the specified index.
        /// </summary>
        public void CopyTo(ValueSet[] array, int arrayIndex) => _dictionary._lookup.Values.CopyTo(array, arrayIndex);

        /// <summary>
        /// Returns an enumerator that iterates through the value sets in this collection.
        /// </summary>
        public Enumerator GetEnumerator() => new(_dictionary);

        #region Explicit Interface Implementations

        /// <summary>
        /// Gets a value indicating whether this collection is read-only. Always returns <see langword="true"/>.
        /// </summary>
        bool ICollection<ValueSet>.IsReadOnly => true;

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator<ValueSet> IEnumerable<ValueSet>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Not Supported

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<ValueSet>.Add(ValueSet item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<ValueSet>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        bool ICollection<ValueSet>.Remove(ValueSet item) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// Enumerates the value sets of a <see cref="ValueSetCollection"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<ValueSet>
        {
            private readonly HashSetDictionary<TKey, TValue> _dictionary;
            private readonly int _version;

            private Dictionary<TKey, ValueSet>.ValueCollection.Enumerator _valueSetsEnumerator;

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public ValueSet Current => _valueSetsEnumerator.Current;

            /// <inheritdoc cref="Current"/>
            object? IEnumerator.Current => Current;

            internal Enumerator(HashSetDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = _dictionary._version;

                _valueSetsEnumerator = dictionary._lookup.Values.GetEnumerator();
            }

            /// <summary>
            /// Releases all the resources used by the enumerator.
            /// </summary>
            public void Dispose() => _valueSetsEnumerator.Dispose();

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            public bool MoveNext()
            {
                Throw.IfEnumeratedCollectionChanged(_version, _dictionary._version);
#if DEBUG
                if (_valueSetsEnumerator.MoveNext())
                {
                    DebugValid(_valueSetsEnumerator.Current);
                    return true;
                }

                return false;
#else
                return _valueSetsEnumerator.MoveNext();
#endif
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            /// <exception cref="NotSupportedException">This operation is not supported.</exception>
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}