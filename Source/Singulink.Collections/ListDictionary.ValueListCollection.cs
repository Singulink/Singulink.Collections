using System.Collections;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

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

        /// <summary>
        /// Gets the number of value lists in this collection.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Gets the <see cref="ListDictionary{TKey, TValue}"/> this collection belongs to.
        /// </summary>
        public ListDictionary<TKey, TValue> Dictionary => _dictionary;

        /// <summary>
        /// Returns a value indicating whether this collection contains the given value list.
        /// </summary>
        public bool Contains(ValueList valueList) => valueList.Count > 0 && valueList.Dictionary == _dictionary;

        /// <summary>
        /// Copies the value lists in this collection to an array starting at the specified index.
        /// </summary>
        public void CopyTo(ValueList[] array, int index) => _dictionary._lookup.Values.CopyTo(array, index);

        /// <summary>
        /// Returns an enumerator that iterates through the value lists in this collection.
        /// </summary>
        public Enumerator GetEnumerator() => new(_dictionary);

        #region Explicit Interface Implementations

        /// <summary>
        /// Gets a value indicating whether this collection is read-only. Always returns <see langword="true"/>.
        /// </summary>
        bool ICollection<ValueList>.IsReadOnly => true;

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator<ValueList> IEnumerable<ValueList>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Not Supported

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<ValueList>.Add(ValueList item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<ValueList>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        bool ICollection<ValueList>.Remove(ValueList item) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// Enumerates the value lists in a <see cref="ValueListCollection"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<ValueList>
        {
            private readonly ListDictionary<TKey, TValue> _dictionary;
            private readonly int _version;

            private Dictionary<TKey, ValueList>.ValueCollection.Enumerator _valueListsEnumerator;

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public ValueList Current => _valueListsEnumerator.Current;

            /// <inheritdoc cref="Current"/>
            object? IEnumerator.Current => Current;

            internal Enumerator(ListDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _version = _dictionary._version;

                _valueListsEnumerator = dictionary._lookup.Values.GetEnumerator();
            }

            /// <summary>
            /// Releases all the resources used by the enumerator.
            /// </summary>
            public void Dispose() => _valueListsEnumerator.Dispose();

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            public bool MoveNext()
            {
                Throw.IfEnumeratedCollectionChanged(_version, _dictionary._version);
#if DEBUG
                if (_valueListsEnumerator.MoveNext())
                {
                    DebugValid(_valueListsEnumerator.Current);
                    return true;
                }

                return false;
#else
                return _valueListsEnumerator.MoveNext();
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