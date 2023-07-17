using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections
{
    /// <summary>
    /// Represents a collection of keys and weakly referenced values. If this collection is accessed concurrently from multiple threads (even in a read-only
    /// manner) then all accesses must be synchronized with a full lock.
    /// </summary>
    /// <remarks>
    /// <para>On .NET Core 3+, internal entries for garbage collected values are removed as they are encountered (i.e. if a key lookup is performed on a garbage
    /// collected value or if keys/values are enumerated over). This is not the case on .NET Standard targets like .NET Framework and earlier versions of .NET
    /// Core. You can perform a full clean by calling the <see cref="Clean"/> method or configure automatic cleaning after a set number of add operations by
    /// setting the <see cref="AutoCleanAddCount"/> property.</para>
    /// </remarks>
    public class WeakValueDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
        where TValue : class
    {
#if NETSTANDARD2_0
        private Dictionary<TKey, WeakReference<TValue>> _entryLookup;
        private int _capacity;
#else
        private readonly Dictionary<TKey, WeakReference<TValue>> _entryLookup;
#endif
        private int _autoCleanAddCount;
        private int _addCountSinceLastClean;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakValueDictionary{TKey, TValue}"/> class.
        /// </summary>
        public WeakValueDictionary() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakValueDictionary{TKey, TValue}"/> class using the specified key equality comparer.
        /// </summary>
        public WeakValueDictionary(IEqualityComparer<TKey>? comparer)
        {
            _entryLookup = new(comparer);
        }

        /// <summary>
        /// Gets or sets the number of add (or indexer set) operations that automatically triggers the <see cref="Clean"/> method to run. Default value is
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
        /// Gets the number of add (or indexer set) operations that have been performed since the last cleaning.
        /// </summary>
        public int AddCountSinceLastClean => _addCountSinceLastClean;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically call <see cref="TrimExcess"/> whenever <see cref="Clean"/> is called. Default value is
        /// <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>Setting this property on the .NET Standard 2.0 version of the library (i.e. on .NET Framework or .NET Core 2.2) has no effect.</para>
        /// </remarks>
        public bool TrimExcessDuringClean { get; set; }

        /// <summary>
        /// Gets the keys in the dictionary.
        /// </summary>
        public IEnumerable<TKey> Keys => this.Select(kvp => kvp.Key);

        /// <summary>
        /// Gets the values in the dictionary.
        /// </summary>
        public IEnumerable<TValue> Values => this.Select(kvp => kvp.Value);

        /// <summary>
        /// Gets the number of entries in the internal data structure. This value will be different than the actual count if any of the values were garbage
        /// collected but still have internal entries in the dictionary that have not been cleaned.
        /// </summary>
        /// <remarks>
        /// <para>This count will not be accurate if values have been collected since the last clean. You can call <see cref="Clean"/> to force a full sweep
        /// before reading the count to get a more accurate value, but keep in mind that a subsequent enumeration may still return fewer values if they happen
        /// to get garbage collected before or during the enumeration. If you require an accurate count together with all the values then you should
        /// temporarily copy the values into a strongly referenced collection (like a <see cref="List{T}"/> or <see cref="Dictionary{TKey, TValue}"/>) so that
        /// they can't be garbage collected and use that to get the count and access the values.</para>
        /// </remarks>
        public int UnsafeCount => _entryLookup.Count;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public TValue this[TKey key]
        {
            get {
                if (!TryGetValue(key, out var value))
                    throw new KeyNotFoundException();

                return value;
            }
            set {
                _entryLookup[key] = new WeakReference<TValue>(value);
                OnAdded();
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The value associated with the specified key, otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the dictionary contains a value with the specified key, otherwise <see langword="false"/>.</returns>
        public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
        {
            if (_entryLookup.TryGetValue(key, out var entry))
            {
                if (entry.TryGetTarget(out value))
                    return true;
#if NETCOREAPP
                else
                    _entryLookup.Remove(key);
#endif
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        public bool TryAdd(TKey key, TValue value)
        {
            if (_entryLookup.TryGetValue(key, out var entry) && entry.TryGetTarget(out _))
                return false;

            _entryLookup[key] = new WeakReference<TValue>(value);
            OnAdded();
            return true;
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <exception cref="ArgumentException">The specified key already exists in the dictionary.</exception>
        public void Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
                throw new ArgumentException("Specified key already exists.", nameof(key));
        }

        /// <summary>
        /// Removes the value with the specified key from the dictionary.
        /// </summary>
        /// <returns><see langword="true"/> if the item was found and removed, otherwise <see langword="false"/>.</returns>
        public bool Remove(TKey key) => TryGetValue(key, out _) && _entryLookup.Remove(key);

        /// <summary>
        /// Removes the entry with the given key and value from the dictionary using the specified equality comparer for the value type.
        /// </summary>
        public bool Remove(TKey key, TValue value, IEqualityComparer<TValue>? comparer = null)
        {
            if (TryGetValue(key, out var current) && (comparer ?? EqualityComparer<TValue>.Default).Equals(value, current))
            {
                _entryLookup.Remove(key);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Indictes whether the dictionary contains the specified key/value pair using the specified value comparer.
        /// </summary>
        public bool Contains(KeyValuePair<TKey, TValue> kvp, IEqualityComparer<TValue>? comparer = null) => Contains(kvp.Key, kvp.Value, comparer);

        /// <summary>
        /// Indictes whether the dictionary contains the key and value using the specified value comparer.
        /// </summary>
        public bool Contains(TKey key, TValue value, IEqualityComparer<TValue>? comparer = null)
        {
            return TryGetValue(key, out var current) && (comparer ?? EqualityComparer<TValue>.Default).Equals(value, current);
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        public bool ContainsKey(TKey key) => TryGetValue(key, out _);

        /// <summary>
        /// Determines whether the dictionary contains the specified value.
        /// </summary>
        public bool ContainsValue(TValue value, IEqualityComparer<TValue>? comparer = null) => Values.Contains(value, comparer);

        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            _entryLookup.Clear();
            _addCountSinceLastClean = 0;
        }

        /// <summary>
        /// Removes internal entries that refer to values that have been garbage collected.
        /// </summary>
        public void Clean()
        {
#if NETSTANDARD
            var staleKvps = _entryLookup.Where(kvp => !kvp.Value.TryGetTarget(out _)).ToList();
#else
            var staleKvps = _entryLookup.Where(kvp => !kvp.Value.TryGetTarget(out _));
#endif

            foreach (var kvp in staleKvps)
                _entryLookup.Remove(kvp.Key);

#if !NETSTANDARD2_0
            if (TrimExcessDuringClean)
                TrimExcess();
#endif

            _addCountSinceLastClean = 0;
        }

        /// <summary>
        /// Reduces the internal capacity of this dictionary to the size needed to hold the current entries.
        /// </summary>
        public void TrimExcess()
        {
#if NETSTANDARD2_0
            if (_capacity > _entryLookup.Count * 2)
            {
                _entryLookup = new Dictionary<TKey, WeakReference<TValue>>(_entryLookup, _entryLookup.Comparer);
                _capacity = _entryLookup.Count;
            }
#else
            _entryLookup.TrimExcess();
#endif
        }

        /// <summary>
        /// Ensures that this dictionary can hold the specified number of elements without growing.
        /// </summary>
        public void EnsureCapacity(int capacity)
        {
#if !NETSTANDARD2_0
            _entryLookup.EnsureCapacity(capacity);
#endif
        }

        /// <summary>
        /// Returns an enumerator that iterates through the key/value pairs in the dictionary.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var kvp in _entryLookup)
            {
                if (kvp.Value.TryGetTarget(out var value))
                    yield return new KeyValuePair<TKey, TValue>(kvp.Key, value);
#if NETCOREAPP
                else
                    _entryLookup.Remove(kvp.Key);
#endif
            }

#if NETCOREAPP
            _addCountSinceLastClean = 0;
#endif
        }

        private void OnAdded()
        {
#if NETSTANDARD2_0
            if (_entryLookup.Count > _capacity)
                _capacity = _entryLookup.Count;
#endif

            _addCountSinceLastClean++;

            if (_autoCleanAddCount != 0 && _addCountSinceLastClean >= _autoCleanAddCount)
                Clean();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the key/value pairs in the dictionary.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}