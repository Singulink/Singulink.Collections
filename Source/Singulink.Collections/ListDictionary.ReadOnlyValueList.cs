using System.Collections;
using System.Runtime.CompilerServices;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <content>
/// Contains the ReadOnlyValueList implementation for ListDictionary.
/// </content>
public partial class ListDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a synchronized read-only list of values associcated with a key in a <see cref="ListDictionary{TKey, TValue}"/>.
    /// </summary>
    public class ReadOnlyValueList : IList<TValue>, IReadOnlyList<TValue>, IEquatable<ReadOnlyValueList>
    {
#pragma warning disable SA1401 // Fields should be private

        private protected readonly ListDictionary<TKey, TValue> _dictionary;
        private protected readonly TKey _key;
        private protected List<TValue> _lastList;
        private protected ReadOnlyList<TValue>? _transientReadOnlyList;

#pragma warning restore SA1401

        internal ReadOnlyValueList(ListDictionary<TKey, TValue> dictionary, TKey key, List<TValue> lastList)
        {
            _dictionary = dictionary;
            _key = key;
            _lastList = lastList;
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        public TValue this[int index] => GetList()[index];

        /// <summary>
        /// Gets the key this value list is associated with.
        /// </summary>
        public TKey Key => _key;

        /// <summary>
        /// Gets the dictionary this value list is associate with.
        /// </summary>
        public ListDictionary<TKey, TValue> Dictionary => _dictionary;

        /// <summary>
        /// Gets the number of items in this value list. If the count is zero, this value list is detached from <see cref="Dictionary"/>, i.e. calling <see
        /// cref="ContainsKey(TKey)"/> on the dictionary and passing in <see cref="Key"/> as a parameter will return <see langword="false"/>. When items are
        /// added to the value list it is attached to the dictionary.
        /// </summary>
        public int Count => GetList().Count;

        internal List<TValue> LastList => _lastList;

        /// <summary>
        /// Returns a fast read-only wrapper around the underlying <see cref="List{T}"/> that is only guaranteed to be valid until the values associated with
        /// <see cref="Key"/> in <see cref="Dictionary"/> are modified.
        /// </summary>
        /// <remarks>
        /// <para>References to transient lists should only be held for as long as a series of multiple consequtive read-only operations need to be performed on
        /// them. Once the values associated with <see cref="Key"/> are modified, the transient list's behavior is undefined and it may contain stale
        /// values.</para>
        /// <para>It is not beneficial to use a transient list for a single read operation, but when multipe read operations need to be done consequtively,
        /// slight performance gains may be seen from performing them on a transient read-only list instead of directly on a <see cref="ValueList"/> or <see
        /// cref="ReadOnlyValueList"/>, since the transient list does not check if it needs to synchronize with a newly attached value list in the
        /// dictionary.</para>
        /// </remarks>
        public ReadOnlyList<TValue> AsTransient()
        {
            var list = GetList();

            if (_transientReadOnlyList == null)
                _transientReadOnlyList = new(list);
            else
                _transientReadOnlyList.WrappedList = list;

            return _transientReadOnlyList;
        }

        /// <summary>a
        /// Determines whether an element is in the list.
        /// </summary>
        public bool Contains(TValue item) => GetList().Contains(item);

        /// <summary>
        /// Copies the list to an array.
        /// </summary>
        public void CopyTo(TValue[] array) => GetList().CopyTo(array);

        /// <summary>
        /// Copies the list to an array starting at the specified array index.
        /// </summary>
        public void CopyTo(TValue[] array, int arrayIndex) => GetList().CopyTo(array, arrayIndex);

        /// <summary>
        /// Copies a range of elements from the list to an array starting at the specified array index.
        /// </summary>
        public void CopyTo(int index, TValue[] array, int arrayIndex, int count) => GetList().CopyTo(index, array, arrayIndex, count);

        /// <summary>
        /// Determines whether the list contains an element that matches the condition specified by the specified predicate.
        /// </summary>
        public bool Exists(Predicate<TValue> match) => GetList().Exists(match);

        /// <summary>
        /// Searches the list for an element that matches the conditions in the specified predicate and returns the first matching item.
        /// </summary>
        public TValue? Find(Predicate<TValue> match) => GetList().Find(match);

        /// <summary>
        /// Returns all the elements in the list that match the condition in the specified predicate.
        /// </summary>
        public List<TValue> FindAll(Predicate<TValue> match) => GetList().FindAll(match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the index of the first occurrence within the range
        /// of elements in the list that starts at the specified  index and contains the specified number of elements.
        /// </summary>
        public int FindIndex(int startIndex, int count, Predicate<TValue> match) => GetList().FindIndex(startIndex, count, match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the index of the first occurrence within
        /// the range of elements in the list that extends from the specified index to the last element.
        /// </summary>
        public int FindIndex(int startIndex, Predicate<TValue> match) => GetList().FindIndex(startIndex, match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the index of the first occurrence within the entire
        /// list.
        /// </summary>
        public int FindIndex(Predicate<TValue> match) => GetList().FindIndex(match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the last occurrence within the entire list.
        /// </summary>
        public TValue? FindLast(Predicate<TValue> match) => GetList().FindLast(match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the index of the last occurrence within the range of
        /// elements in the list that contains the specified number of elements and ends at the specified index.
        /// </summary>
        public int FindLastIndex(int startIndex, int count, Predicate<TValue> match) => GetList().FindLastIndex(startIndex, count, match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the index of the last occurrence within the range of
        /// elements in the list that extends from the first element to the specified index.
        /// </summary>
        public int FindLastIndex(int startIndex, Predicate<TValue> match) => GetList().FindLastIndex(startIndex, match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the index of the last occurrence within the entire
        /// list.
        /// </summary>
        public int FindLastIndex(Predicate<TValue> match) => GetList().FindLastIndex(match);

        /// <summary>
        /// Performs the specified action on each element of the list.
        /// </summary>
        public void ForEach(Action<TValue> action) => GetList().ForEach(action);

        /// <summary>
        /// Creates a shallow copy of a range of elements in the source list.
        /// </summary>
        public List<TValue> GetRange(int index, int count) => GetList().GetRange(index, count);

        /// <summary>
        /// Searches for the specified object and returns the index of the first occurrence within the entire list.
        /// </summary>
        public int IndexOf(TValue item) => GetList().IndexOf(item);

        /// <summary>
        /// Searches for the specified object and returns the index of the first occurrence within the range of elements in the list that extends from the specified
        /// index to the last element.
        /// </summary>
        public int IndexOf(TValue item, int index) => GetList().IndexOf(item, index);

        /// <summary>
        /// Searches for the specified object and returns the index of the first occurrence within the range of elements in the list that starts at the specified
        /// index and contains the specified number of elements.
        /// </summary>
        public int IndexOf(TValue item, int index, int count) => GetList().IndexOf(item, index, count);

        /// <summary>
        /// Searches for the specified object and returns the index of the last occurrence within the entire list.
        /// </summary>
        public int LastIndexOf(TValue item) => GetList().LastIndexOf(item);

        /// <summary>
        /// Searches for the specified object and returns the index of the last occurrence within the range of elements in the list that extends from the first
        /// element to the specified index.
        /// </summary>
        public int LastIndexOf(TValue item, int index) => GetList().LastIndexOf(item, index);

        /// <summary>
        /// Searches for the specified object and returns the index of the last occurrence within the range of elements in the list that contains the specified
        /// number of elements and ends at the specified index.
        /// </summary>
        public int LastIndexOf(TValue item, int index, int count) => GetList().LastIndexOf(item, index, count);

        /// <summary>
        /// Copies the elements of the list to a new array.
        /// </summary>
        public TValue[] ToArray() => GetList().ToArray();

        /// <summary>
        /// Determines whether every element in the list matches the conditions defined by the specified predicate.
        /// </summary>
        public bool TrueForAll(Predicate<TValue> match) => GetList().TrueForAll(match);

        /// <summary>
        /// Returns an enumerator that iterates through the underlying <see cref="List{T}"/>.
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected List<TValue> GetList()
        {
            return _lastList.Count > 0 ? _lastList : UpdateAndGetValues();

            [MethodImpl(MethodImplOptions.NoInlining)]
            List<TValue> UpdateAndGetValues() => _dictionary.TryGetValues(_key, out var valueList) ? _lastList = valueList._lastList : _lastList;
        }

        #region Equality and Hash Code

        /// <summary>
        /// Determines whether two value lists are equal. Value lists are considered equal if they point to the same <see cref="Dictionary"/> with the same <see
        /// cref="Key"/>.
        /// </summary>
        public static bool operator ==(ReadOnlyValueList? x, ReadOnlyValueList? y)
        {
            if (x is null)
                return y is null;

            return x.Equals(y);
        }

        /// <summary>
        /// Determines whether two value lists are not equal. Value lists are considered equal if they point to the same <see cref="Dictionary"/> with the same
        /// <see cref="Key"/>.
        /// </summary>
        public static bool operator !=(ReadOnlyValueList? x, ReadOnlyValueList? y) => !(x == y);

        /// <summary>
        /// Determines whether this value list is equal to another value list. Value lists are considered equal if they point to the same <see
        /// cref="Dictionary"/> with the same <see cref="Key"/>.
        /// </summary>
        public bool Equals(ReadOnlyValueList? other)
        {
            return other != null && GetType() == other.GetType() && _dictionary == other._dictionary && _dictionary.KeyComparer.Equals(_key, other._key);
        }

        /// <summary>
        /// Determines whether this value list is equal to another object. Value lists are considered equal if they point to the same <see
        /// cref="Dictionary"/> and <see cref="Key"/>.
        /// </summary>
        public override bool Equals(object? obj) => Equals(obj as ReadOnlyValueList);

        /// <summary>
        /// Returns a hash code for this value list.
        /// </summary>
        public override int GetHashCode() => (_dictionary, _dictionary.KeyComparer.GetHashCode(_key)).GetHashCode();

        #endregion

        #region Explicit Interface Implementations

        /// <summary>
        /// Gets a value indicating whether this list is read-only. Always returns <see langword="true"/>.
        /// </summary>
        bool ICollection<TValue>.IsReadOnly => true;

        /// <inheritdoc cref="this[int]"/>
        TValue IList<TValue>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

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
        void IList<TValue>.Insert(int index, TValue item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void IList<TValue>.RemoveAt(int index) => throw new NotSupportedException();

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
        /// Enumerates the elements of a <see cref="ReadOnlyValueList"/> or <see cref="ValueList"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly ReadOnlyValueList _valueList;
            private readonly List<TValue> _list;
            private List<TValue>.Enumerator _listEnumerator;

            internal Enumerator(ReadOnlyValueList valueList)
            {
                _valueList = valueList;
                _list = valueList.GetList();
                _listEnumerator = _list.GetEnumerator();
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public TValue Current => _listEnumerator.Current;

            /// <inheritdoc cref="Current"/>
            object? IEnumerator.Current => Current;

            /// <summary>
            /// Releases all resources used by this enumerator.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            public bool MoveNext()
            {
                if (_list.Count == 0 && _list != _valueList.GetList())
                    Throw.EnumerationCollectionChanged();

                return _listEnumerator.MoveNext();
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            /// <exception cref="NotSupportedException">This operation is not supported.</exception>
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}