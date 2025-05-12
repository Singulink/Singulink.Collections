using System.Collections;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <content>
/// Contains the ValueCollection implementation for HashSetDictionary.
/// </content>
public partial class HashSetDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents the collection of values in a <see cref="HashSetDictionary{TKey, TValue}"/>.
    /// </summary>
    public sealed partial class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        private readonly HashSetDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(HashSetDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        /// <inheritdoc cref="HashSetDictionary{TKey, TValue}.ValueCount"/>
        public int Count => _dictionary._valueCount;

        /// <inheritdoc cref="HashSetDictionary{TKey, TValue}.ContainsValue(TValue)"/>
        public bool Contains(TValue item) => _dictionary.ContainsValue(item);

        /// <summary>
        /// Copies all the values in the dictionary to an array starting at the specified array index.
        /// </summary>
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            CollectionCopy.CheckParams(Count, array, arrayIndex);

            foreach (var set in _dictionary._lookup.Values)
            {
                set.CopyTo(array, arrayIndex);
                arrayIndex += set.Count;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the values in this collection.
        /// </summary>
        public Enumerator GetEnumerator() => new(_dictionary);

        #region Explicit Interface Implementations

        /// <summary>
        /// Gets a value indicating whether this collection is read-only. Always returns <see langword="true"/>.
        /// </summary>
        bool ICollection<TValue>.IsReadOnly => true;

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Not Supported

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<TValue>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// Enumerates the values of a <see cref="ValueCollection"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly HashSetDictionary<TKey, TValue> _dictionary;
            private readonly int _version;

            private Dictionary<TKey, ValueSet>.ValueCollection.Enumerator _valueSetsEnumerator;
            private HashSet<TValue>.Enumerator _currentSetEnumerator;
            private bool _started;

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public TValue Current => _currentSetEnumerator.Current;

            /// <inheritdoc cref="Current"/>
            object? IEnumerator.Current => Current;

            internal Enumerator(HashSetDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = _dictionary._version;

                _valueSetsEnumerator = dictionary._lookup.Values.GetEnumerator();
                _currentSetEnumerator = default;
                _started = false;
            }

            /// <summary>
            /// Releases all the resources used by the enumerator.
            /// </summary>
            public void Dispose()
            {
                _valueSetsEnumerator.Dispose();
                _currentSetEnumerator.Dispose();
            }

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            public bool MoveNext()
            {
                Throw.IfEnumeratedCollectionChanged(_version, _dictionary._version);

                if (!_started)
                {
                    if (!_valueSetsEnumerator.MoveNext())
                        return false;

                    DebugValid(_valueSetsEnumerator.Current);

                    _currentSetEnumerator = _valueSetsEnumerator.Current.LastSet.GetEnumerator();
                    _currentSetEnumerator.MoveNext();
                    _started = true;
                    return true;
                }

                if (_currentSetEnumerator.MoveNext())
                    return true;

                if (!_valueSetsEnumerator.MoveNext())
                    return false;

                DebugValid(_valueSetsEnumerator.Current);

                _currentSetEnumerator = _valueSetsEnumerator.Current.LastSet.GetEnumerator();
                _currentSetEnumerator.MoveNext();
                return true;
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            /// <exception cref="NotSupportedException">This operation is not supported.</exception>
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}