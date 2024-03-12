using System.Diagnostics;
using System.Runtime.CompilerServices;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <content>
/// Contains the ValueList implementation for ListDictionary.
/// </content>
public partial class ListDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a dictionary synchronized list of values associated with a key in a <see cref="ListDictionary{TKey, TValue}"/>.
    /// </summary>
    public sealed class ValueList : ReadOnlyValueList, IList<TValue>, IReadOnlyCollectionProvider<TValue>
    {
        private ReadOnlyValueList? _readOnlyValueList;

        internal ValueList(ListDictionary<TKey, TValue> dictionary, TKey key) : base(dictionary, key, []) { }

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        public new TValue this[int index]
        {
            get => base[index];
            set {
                var list = GetList();
                list[index] = value;
                _dictionary._version++;
            }
        }

        /// <summary>
        /// Gets or sets the total number of items the internal data structure can hold without resizing.
        /// </summary>
        public int Capacity
        {
            get => GetList().Capacity;
            set => GetList().Capacity = value;
        }

        /// <summary>
        /// Returns a dictionary synchronized read-only wrapper around this value list.
        /// </summary>
        public ReadOnlyValueList AsReadOnly() => _readOnlyValueList ??= new(_dictionary, _key, _lastList);

        /// <summary>
        /// Adds an item to the end of this list.
        /// </summary>
        public void Add(TValue item)
        {
            var list = GetList();
            list.Add(item);
            FinishAdding(list, 1);
        }

        /// <summary>
        /// Adds the items of the specified collection to the end of this list.
        /// </summary>
        public int AddRange(IEnumerable<TValue> collection)
        {
            var list = GetList();
            int beforeCount = list.Count;
            int added;

            try
            {
                list.AddRange(collection);
            }
            finally
            {
                added = list.Count - beforeCount;
                FinishAdding(list, added);
            }

            return added;
        }

        /// <summary>
        /// Clears all the items in this list and removes the key from the dictionary.
        /// </summary>
        public void Clear()
        {
            var list = GetList();
            int removed = list.Count;

            list.Clear();
            FinishRemoving(list, removed);
        }

#if NET
        /// <summary>
        /// Ensures that the capacity of this list is at least the specified capacity.
        /// </summary>
        /// <returns>The new capacity of this list.</returns>
        public int EnsureCapacity(int capacity)
        {
            // EnsureCapacity causes list version to increment up to net7: https://github.com/dotnet/runtime/issues/82455

            if (!Runtime.IsNet8OrHigher)
                _dictionary._version++;

            return GetList().EnsureCapacity(capacity);
        }
#endif

        /// <summary>
        /// Inserts an item into the list at the specified index.
        /// </summary>
        public void Insert(int index, TValue item)
        {
            var list = GetList();
            list.Insert(index, item);
            FinishAdding(list, 1);
        }

        /// <summary>
        /// Inserts the items of a collection into the list at the specified index.
        /// </summary>
        /// <returns>The number of items that were inserted.</returns>
        public int InsertRange(int index, IEnumerable<TValue> collection)
        {
            var list = GetList();
            int beforeCount = list.Count;
            int added;

            try
            {
                list.InsertRange(index, collection);
            }
            finally
            {
                added = list.Count - beforeCount;
                FinishAdding(list, added);
            }

            return added;
        }

        /// <summary>
        /// Removes the first occurrence of the specified item from the list.
        /// </summary>
        public bool Remove(TValue item)
        {
            var list = GetList();

            if (list.Remove(item))
            {
                FinishRemoving(list, 1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes all the items that match the conditions defined by the specified predicate.
        /// </summary>
        /// <returns>The number of items removed from the list.</returns>
        public int RemoveAll(Predicate<TValue> match)
        {
            var list = GetList();
            int beforeCount = list.Count;
            int removed;

            try
            {
                list.RemoveAll(match);
            }
            finally
            {
                removed = beforeCount - list.Count;
                FinishRemoving(list, removed);
            }

            return removed;
        }

        /// <summary>
        /// Removes the item at the specified index of the list.
        /// </summary>
        public void RemoveAt(int index)
        {
            var list = GetList();
            list.RemoveAt(index);
            FinishRemoving(list, 1);
        }

        /// <summary>
        /// Removes a range of items from the list.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of items to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        public void RemoveRange(int index, int count)
        {
            var list = GetList();
            list.RemoveRange(index, count);
            FinishRemoving(list, count);
        }

        /// <summary>
        /// Reverses the order of the items in the entire list.
        /// </summary>
        public void Reverse()
        {
            _dictionary._version++;
            GetList().Reverse();
        }

        /// <summary>
        /// Reverses the order of the items in the specified range.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to reverse.</param>
        /// <param name="count">The number of items in the range to reverse.</param>
        public void Reverse(int index, int count)
        {
            _dictionary._version++;
            GetList().Reverse(index, count);
        }

        /// <summary>
        /// Clears the items in this list and adds the items from the specified collection.
        /// </summary>
        public void SetRange(IEnumerable<TValue> collection)
        {
            var list = GetList();
            int beforeCount = list.Count;

            list.Clear();

            try
            {
                list.AddRange(collection);
            }
            finally
            {
                if (beforeCount == 0)
                {
                    if (list.Count > 0)
                    {
                        _dictionary._lookup.Add(_key, this);
                        _dictionary._valueCount += list.Count;
                    }
                }
                else
                {
                    if (list.Count == 0)
                    {
                        _dictionary._lookup.Remove(_key);
                        _dictionary._valueCount -= beforeCount;
                    }
                    else
                    {
                        _dictionary._valueCount += list.Count - beforeCount;
                    }
                }

                _dictionary._version++;
                _dictionary.DebugValueCount();
            }
        }

        /// <summary>
        /// Sorts the items in the entire list using the default comparer.
        /// </summary>
        public void Sort()
        {
            _dictionary._version++;
            GetList().Sort();
        }

        /// <summary>
        /// Sorts the items in the entire list using the specified comparison.
        /// </summary>
        public void Sort(Comparison<TValue> comparison)
        {
            _dictionary._version++;
            GetList().Sort(comparison);
        }

        /// <summary>
        /// Sorts the items in the entire list using the specified comparer.
        /// </summary>
        public void Sort(IComparer<TValue>? comparer)
        {
            _dictionary._version++;
            GetList().Sort(comparer);
        }

        /// <summary>
        /// Sorts the items in a range of items in System.Collections.Generic.List`1 using the specified comparer.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count"> The length of the range to sort.</param>
        /// <param name="comparer">The comparer implementation to use when comparing items, or <see langword="null"/> to use the default comparer.</param>
        public void Sort(int index, int count, IComparer<TValue>? comparer)
        {
            _dictionary._version++;
            GetList().Sort(index, count, comparer);
        }

        /// <summary>
        /// Sets the capacity to the actual number of items in the list, if that number is less than a threshold value.
        /// </summary>
        public void TrimExcess() => GetList().TrimExcess();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishRemoving(List<TValue> list, int removed)
        {
            Debug.Assert(_dictionary._lookup[_key]._lastList == list, "removed from non-synced list");

            if (removed > 0)
            {
                if (list.Count == 0)
                    _dictionary._lookup.Remove(_key);

                _dictionary._valueCount -= removed;
                _dictionary._version++;
                _dictionary.DebugValueCount();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishAdding(List<TValue> list, int added)
        {
            if (added > 0)
            {
                if (list.Count == added)
                    _dictionary._lookup.Add(_key, this);

                _dictionary._valueCount += added;
                _dictionary._version++;
                _dictionary.DebugValueCount();
            }
        }

        #region Explicit Interface Implementations

        /// <summary>
        /// Gets a value indicating whether this list is read-only. Always returns <see langword="false"/>.
        /// </summary>
        bool ICollection<TValue>.IsReadOnly => false;

        /// <inheritdoc cref="IReadOnlyCollectionProvider{T}.GetReadOnlyCollection"/>
        IReadOnlyCollection<TValue> IReadOnlyCollectionProvider<TValue>.GetReadOnlyCollection() => AsReadOnly();

        #endregion
    }
}