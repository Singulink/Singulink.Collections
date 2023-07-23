using System.Collections;

namespace Singulink.Collections
{
    /// <summary>
    /// Represents a collection of weakly referenced values that maintains relative insertion order. If this collection is accessed concurrently from multiple
    /// threads in a read-only manner then no locking is necessary, otherwise a full lock or reader/writer lock must be obtained around all accesses.
    /// </summary>
    /// <remarks>
    /// <para>Internal entries for garbage collected values are not removed automatically by default. You can perform a full clean by calling the <see
    /// cref="Clean"/> method or configure automatic cleaning after a set number of <see cref="Add(T)"/> operations by setting the <see
    /// cref="AutoCleanAddCount"/> property.</para>
    /// </remarks>
    public sealed class WeakList<T> : IEnumerable<T> where T : class
    {
        private readonly List<WeakReference<T>> _entries = new List<WeakReference<T>>();

        private int _autoCleanAddCount;
        private int _extraTrimCapacity;
        private int _addCountSinceLastClean;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakList{T}"/> class.
        /// </summary>
        public WeakList()
        {
        }

        /// <summary>
        /// Gets or sets the number of <see cref="Add(T)"/> operations that automatically triggers the <see cref="Clean"/> method to run. Default value is
        /// <see langword="null"/> which indicates that automatic cleaning is not performed.
        /// </summary>
        public int? AutoCleanAddCount
        {
            get => _autoCleanAddCount == 0 ? null : _autoCleanAddCount;
            set {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _autoCleanAddCount = value.GetValueOrDefault();
            }
        }

        /// <summary>
        /// Gets the number of add operations that have been performed since the last cleaning.
        /// </summary>
        public int AddCountSinceLastClean => _addCountSinceLastClean;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically call <see cref="TrimExcess"/> whenever <see cref="Clean"/> is called. Default value is
        /// <see langword="false"/>.
        /// </summary>
        public bool TrimExcessDuringClean { get; set; }

        /// <summary>
        /// Gets or sets the extra capacity to leave when <see cref="TrimExcess"/> is called. Default value is <c>0</c>.
        /// </summary>
        public int ExtraTrimCapacity
        {
            get => _extraTrimCapacity;
            set {
                if (_extraTrimCapacity < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _extraTrimCapacity = value;
            }
        }

        /// <summary>
        /// Gets the number of entries in the internal data structure. This value will be different than the actual count if any of the values were garbage
        /// collected but still have internal entries in the list that have not been cleaned.
        /// </summary>
        /// <remarks>
        /// <para>This count will not be accurate if values have been collected since the last clean. You can call <see cref="Clean"/> to force a full sweep
        /// before reading the count to get a more accurate value, but keep in mind that a subsequent enumeration may still return fewer values if they happen
        /// to get garbage collected before or during the enumeration. If you require an accurate count together with all the values then you should
        /// temporarily copy the values into a strongly referenced collection (like a <see cref="List{T}"/>) so that they can't be garbage collected and use
        /// that to get the count and access the values.</para>
        /// </remarks>
        public int UnsafeCount => _entries.Count;

        /// <summary>
        /// Gets or sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        public int Capacity
        {
            get => _entries.Capacity;
            set => _entries.Capacity = value;
        }

        /// <summary>
        /// Adds an item to the end of the collection.
        /// </summary>
        public void Add(T item)
        {
            _entries.Add(new WeakReference<T>(item));
            OnAdded();
        }

        /// <summary>
        /// Inserts an item to the beginning of the collection.
        /// </summary>
        public void InsertFirst(T item)
        {
            _entries.Insert(0, new WeakReference<T>(item));
            OnAdded();
        }

        /// <summary>
        /// Inserts an item before another item.
        /// </summary>
        /// <param name="findItem">The item to find to determine the insertion point.</param>
        /// <param name="item">The item to insert.</param>
        /// <param name="comparer">The comparer to use to determine item equality.</param>
        /// <exception cref="ArgumentException">The item to insert before was not found.</exception>
        public void InsertBefore(T findItem, T item, IEqualityComparer<T>? comparer = null)
        {
            if (!TryInsertBefore(findItem, item, comparer))
                throw new ArgumentException("The specified item was not found.", nameof(findItem));
        }

        /// <summary>
        /// Inserts an item before another item.
        /// </summary>
        /// <param name="findItem">The item to find to determine the insertion point.</param>
        /// <param name="item">The item to insert.</param>
        /// <param name="comparer">The comparer to use to determine item equality.</param>
        /// <returns><see langword="true"/> if the item to insert after was found and the item was inserted, otherwise <see langword="false"/>.</returns>
        public bool TryInsertBefore(T findItem, T item, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].TryGetTarget(out var currentItem) && comparer.Equals(currentItem, findItem))
                {
                    _entries.Insert(i, new WeakReference<T>(item));
                    OnAdded();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Inserts an item after another item.
        /// </summary>
        /// <param name="findItem">The item to find to determine the insertion point.</param>
        /// <param name="item">The item to insert.</param>
        /// <param name="comparer">The comparer to use to determine item equality.</param>
        /// <exception cref="ArgumentException">The item to insert after was not found.</exception>
        public void InsertAfter(T findItem, T item, IEqualityComparer<T>? comparer = null)
        {
            if (!TryInsertAfter(findItem, item, comparer))
                throw new ArgumentException("The specified item was not found.", nameof(findItem));
        }

        /// <summary>
        /// Inserts an item after another item.
        /// </summary>
        /// <param name="findItem">The item to find to determine the insertion point.</param>
        /// <param name="item">The item to insert.</param>
        /// <param name="comparer">The comparer to use to determine item equality.</param>
        /// <returns><see langword="true"/> if the item to insert after was found and the item was inserted, otherwise <see langword="false"/>.</returns>
        public bool TryInsertAfter(T findItem, T item, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].TryGetTarget(out var currentItem) && comparer.Equals(currentItem, findItem))
                {
                    _entries.Insert(i + 1, new WeakReference<T>(item));
                    OnAdded();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes an item from the collection using the specified equality comparer.
        /// </summary>
        /// <returns><see langword="true"/> if the item was removed, otherwise <see langword="false"/>.</returns>
        public bool Remove(T item, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].TryGetTarget(out var currentItem) && comparer.Equals(currentItem, item))
                {
                    _entries.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the collection contains the given item using the specified equality comparer.
        /// </summary>
        public bool Contains(T item, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            foreach (var entry in _entries)
            {
                if (entry.TryGetTarget(out var currentItem) && comparer.Equals(currentItem, item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Removes all the elements from the collection.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _addCountSinceLastClean = 0;
        }

        /// <summary>
        /// Removes internal entries for values that have been garbage collected and trims the excess if <see cref="TrimExcessDuringClean"/> is set.
        /// </summary>
        public void Clean()
        {
            _entries.RemoveAll(entry => !entry.TryGetTarget(out _));

            if (TrimExcessDuringClean)
                TrimExcess();

            _addCountSinceLastClean = 0;
        }

        /// <summary>
        /// Reduces the internal capacity to the number of entries in the collection plus <see cref="ExtraTrimCapacity"/>.
        /// </summary>
        public void TrimExcess()
        {
            int trimmedCapacity = _entries.Count + ExtraTrimCapacity;

            if (trimmedCapacity < _entries.Capacity)
                _entries.Capacity = trimmedCapacity;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var entry in _entries)
            {
                if (entry.TryGetTarget(out var item))
                    yield return item;
            }
        }

        private void OnAdded()
        {
            _addCountSinceLastClean++;

            if (_autoCleanAddCount != 0 && _addCountSinceLastClean >= _autoCleanAddCount)
                Clean();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}