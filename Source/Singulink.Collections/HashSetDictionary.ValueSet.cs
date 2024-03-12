using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Singulink.Collections;

/// <content>
/// Contains the ValueSet implementation for HashSetDictionary.
/// </content>
public partial class HashSetDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a dictionary synchronized set of values associated with a key in a <see cref="HashSetDictionary{TKey, TValue}"/>.
    /// </summary>
    public sealed class ValueSet : ReadOnlyValueSet, ISet<TValue>, IReadOnlyCollectionProvider<TValue>
    {
        private ReadOnlyValueSet? _readOnlyValueSet;

        internal ValueSet(HashSetDictionary<TKey, TValue> dictionary, TKey key) : base(dictionary, key, new(dictionary._valueComparer)) { }

        /// <summary>
        /// Returns a dictionary synchronized read-only wrapper around this value set.
        /// </summary>
        public ReadOnlyValueSet AsReadOnly() => _readOnlyValueSet ??= new(_dictionary, _key, _lastSet);

        /// <summary>
        /// Adds an item to this set.
        /// </summary>
        public bool Add(TValue item)
        {
            var set = GetSet();

            if (set.Add(item))
            {
                FinishAdding(set, 1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds the items of the specified collection to this set.
        /// </summary>
        public int AddRange(IEnumerable<TValue> collection)
        {
            var set = GetSet();
            int beforeCount = set.Count;
            int added;

            try
            {
                foreach (var item in collection)
                    set.Add(item);
            }
            finally
            {
                added = set.Count - beforeCount;
                FinishAdding(set, added);
            }

            return added;
        }

        /// <summary>
        /// Clears all the values in this set and removes the key from the dictionary.
        /// </summary>
        public void Clear()
        {
            var set = GetSet();
            int removed = set.Count;

            set.Clear();
            FinishRemoving(set, removed);
        }

#if !NETSTANDARD2_0

        /// <summary>
        /// Ensures that the value set can hold the specified number of items without growing.
        /// </summary>
        public int EnsureCapacity(int capacity)
        {
            _dictionary._version++;
            return GetSet().EnsureCapacity(capacity);
        }

#endif

        /// <summary>
        /// Removes all items in the specified collection from the current set.
        /// </summary>
        public void ExceptWith(IEnumerable<TValue> other)
        {
            var set = GetSet();
            int beforeCount = set.Count;

            set.ExceptWith(other);
            FinishRemoving(set, beforeCount - set.Count);
        }

        /// <summary>
        /// Modifies the current set to contain only items that are also present in the specified collection.
        /// </summary>
        public void IntersectWith(IEnumerable<TValue> other)
        {
            var set = GetSet();
            int beforeCount = set.Count;

            set.IntersectWith(other);
            FinishRemoving(set, beforeCount - set.Count);
        }

        /// <summary>
        /// Removes the specified item from the set.
        /// </summary>
        public bool Remove(TValue item)
        {
            var set = GetSet();

            if (set.Remove(item))
            {
                FinishRemoving(set, 1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears the items in this set and adds the items from the specified collection.
        /// </summary>
        public void SetRange(IEnumerable<TValue> collection)
        {
            var set = GetSet();
            int beforeCount = set.Count;

            set.Clear();

            try
            {
                foreach (var item in collection)
                    set.Add(item);
            }
            finally
            {
                if (beforeCount == 0)
                {
                    if (set.Count > 0)
                    {
                        _dictionary._lookup.Add(_key, this);
                        _dictionary._valueCount += set.Count;
                    }
                }
                else
                {
                    if (set.Count == 0)
                    {
                        _dictionary._lookup.Remove(_key);
                        _dictionary._valueCount -= beforeCount;
                    }
                    else
                    {
                        _dictionary._valueCount += set.Count - beforeCount;
                    }
                }

                _dictionary._version++;
                _dictionary.DebugValueCount();
            }
        }

        /// <summary>
        /// Modifies the current set to contain only items that are present either in the set or in the specified collection, but not both.
        /// </summary>
        public void SymmetricExceptWith(IEnumerable<TValue> other)
        {
            var set = GetSet();
            int beforeCount = set.Count;

            set.SymmetricExceptWith(other);
            FinishRemoving(set, beforeCount - set.Count);
        }

        /// <summary>
        /// Sets the capacity of the set to the actual number of items it contains, rounded up to a nearby, implementation-specific value.
        /// </summary>
        public void TrimExcess() => GetSet().TrimExcess();

        /// <summary>
        /// Modifies the current set to contain all items that are present in the set, the specified collection, or both.
        /// </summary>
        public void UnionWith(IEnumerable<TValue> other)
        {
            var set = GetSet();
            int beforeCount = set.Count;

            set.ExceptWith(other);
            FinishAdding(set, beforeCount - set.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishRemoving(HashSet<TValue> set, int removed)
        {
            Debug.Assert(_dictionary._lookup[_key]._lastSet == set, "removed from non-synced set");

            if (removed > 0)
            {
                if (set.Count == 0)
                    _dictionary._lookup.Remove(_key);

                _dictionary._valueCount -= removed;
                _dictionary._version++;
                _dictionary.DebugValueCount();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinishAdding(HashSet<TValue> set, int added)
        {
            if (added > 0)
            {
                if (set.Count == added)
                    _dictionary._lookup.Add(_key, this);

                _dictionary._valueCount += added;
                _dictionary._version++;
                _dictionary.DebugValueCount();
            }
        }

        #region Explicit Interface Implementations

        /// <summary>
        /// Gets a value indicating whether the set is read-only. Always returns <see langword="false"/>.
        /// </summary>
        bool ICollection<TValue>.IsReadOnly => false;

        /// <inheritdoc cref="Add(TValue)"/>
        void ICollection<TValue>.Add(TValue item) => Add(item);

        /// <inheritdoc cref="IReadOnlyCollectionProvider{T}.GetReadOnlyCollection"/>
        IReadOnlyCollection<TValue> IReadOnlyCollectionProvider<TValue>.GetReadOnlyCollection() => AsReadOnly();

        #endregion
    }
}