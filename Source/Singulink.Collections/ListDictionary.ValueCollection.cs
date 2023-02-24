using System.Collections;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <content>
/// Contains the ValueCollection implementation for ListDictionary.
/// </content>
public partial class ListDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents the collection of values in a <see cref="ListDictionary{TKey, TValue}"/>.
    /// </summary>
    public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        private readonly ListDictionary<TKey, TValue> _dictionary;

        internal ValueCollection(ListDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        /// <inheritdoc cref="ListDictionary{TKey, TValue}.ValueCount"/>
        public int Count => _dictionary._valueCount;

        /// <inheritdoc cref="ListDictionary{TKey, TValue}.ContainsValue(TValue)"/>
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
        /// Returns an enumerator that iterates through the <see cref="ValueCollection"/>.
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
            private readonly ListDictionary<TKey, TValue> _dictionary;
            private readonly int _version;

            private Dictionary<TKey, ValueList>.ValueCollection.Enumerator _valueListsEnumerator;
            private List<TValue>.Enumerator _currentListEnumerator;
            private bool _started;

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public TValue Current => _currentListEnumerator.Current;

            /// <inheritdoc cref="Current"/>
            object? IEnumerator.Current => Current;

            internal Enumerator(ListDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = _dictionary._version;

                _valueListsEnumerator = dictionary._lookup.Values.GetEnumerator();
                _currentListEnumerator = default;
                _started = false;
            }

            /// <summary>
            /// Releases all the resources used by the enumerator.
            /// </summary>
            public void Dispose()
            {
                _valueListsEnumerator.Dispose();
                _currentListEnumerator.Dispose();
            }

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            public bool MoveNext()
            {
                Throw.IfEnumeratedCollectionChanged(_version, _dictionary._version);

                if (!_started)
                {
                    if (!_valueListsEnumerator.MoveNext())
                        return false;

                    DebugValid(_valueListsEnumerator.Current);

                    _currentListEnumerator = _valueListsEnumerator.Current.LastList.GetEnumerator();
                    _currentListEnumerator.MoveNext();
                    _started = true;

                    return true;
                }

                if (_currentListEnumerator.MoveNext())
                    return true;

                if (!_valueListsEnumerator.MoveNext())
                    return false;

                DebugValid(_valueListsEnumerator.Current);

                _currentListEnumerator = _valueListsEnumerator.Current.LastList.GetEnumerator();
                _currentListEnumerator.MoveNext();

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