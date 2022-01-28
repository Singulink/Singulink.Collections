using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Singulink.Collections
{
    /// <summary>
    /// Represents a collection of keys and hash sets of unique values (per key).
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// <para>Please note that the <see cref="IDictionary{TKey, TValue}"/> and <see cref="ICollection{T}"/> implementation is read-only because many of the
    /// operations that modify the dictionary have substantially different behavior than is typically expected from these interfaces.</para>
    /// </remarks>
    public partial class HashSetDictionary<TKey, TValue> : IDictionary<TKey, HashSetDictionary<TKey, TValue>.ValueSet>, IReadOnlyDictionary<TKey, HashSetDictionary<TKey, TValue>.ValueSet>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, ValueSet> _lookup;
        private readonly IEqualityComparer<TValue>? _valueComparer;

        private KeyCollection? _keys;
        private ValueSetCollection? _valueSets;
        private ValueCollection? _values;
        private int _valueCount = 0;
        private int _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="HashSetDictionary{TKey, TValue}"/> class.
        /// </summary>
        public HashSetDictionary() : this(0, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HashSetDictionary{TKey, TValue}"/> class with the specified initial capacity for key/value set pairs.
        /// </summary>
        public HashSetDictionary(int capacity) : this(capacity, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HashSetDictionary{TKey, TValue}"/> class that uses the specified key and value comparers.
        /// </summary>
        public HashSetDictionary(IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null) : this(0, keyComparer, valueComparer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HashSetDictionary{TKey, TValue}"/> class with the specified initial capacity for key/value set pairs,
        /// and uses the specified key and value comparers.
        /// </summary>
        public HashSetDictionary(int capacity, IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null)
        {
            _lookup = new(capacity, keyComparer);
            _valueComparer = valueComparer;
        }

        /// <summary>
        /// Gets the value set associated with the specified key or <see langword="null"/> if the key was not found.
        /// </summary>
        /// <exception cref="ArgumentNullException">The specified key was <see langword="null"/>.</exception>
        public ValueSet? this[TKey key] => _lookup.TryGetValue(key, out var valueSet) ? valueSet : null;

        /// <summary>
        /// Gets the number of keys and associated value sets in the dictionary.
        /// </summary>
        public int Count => _lookup.Count;

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public KeyCollection Keys => _keys ??= new KeyCollection(this);

        /// <summary>
        /// Gets a collection containing the value sets in the dictionary.
        /// </summary>
        public ValueSetCollection ValueSets => _valueSets ??= new ValueSetCollection(this);

        /// <summary>
        /// Gets a collection containing all the values in the dictionary across all the keys.
        /// </summary>
        public ValueCollection Values => _values ??= new ValueCollection(this);

#pragma warning disable CA1721 // Property names should not match get methods

        /// <summary>
        /// Gets the number of values in the dictionary across all keys.
        /// </summary>
        public int ValueCount => _valueCount;

#pragma warning restore CA1721 // Property names should not match get methods

        /// <summary>
        /// Gets the number of values in the dictionary associated with the specified key or 0 if the key is not present.
        /// </summary>
        public int GetValueCount(TKey key) => _lookup.TryGetValue(key, out var valueSet) ? valueSet.Count : 0;

        /// <summary>
        /// Adds a value to the value set for the specified key.
        /// </summary>
        /// <returns>True if the key and value were added, or <see langword="false"/> if they were already present in the dictionary.</returns>
        public bool Add(TKey key, TValue value) => Add(key, value, out _);

        /// <summary>
        /// Adds a value to the value set for the specified key.
        /// </summary>
        /// <returns>True if the value was added, or <see langword="false"/> if the value set for the specified key already contained the value.</returns>
        public bool Add(TKey key, TValue value, out ValueSet resultingValueSet)
        {
            HashSet<TValue> set;

            if (!_lookup.TryGetValue(key, out var valueSet)) {
                set = new(_valueComparer);
                valueSet = new(this, key, set);
                _lookup.Add(key, valueSet);
            }
            else {
                set = valueSet.WrappedSet;
            }

            resultingValueSet = valueSet;

            if (set.Add(value)) {
                _valueCount++;
                _version++;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds the elements of a collection into the value set associated with the specified key.
        /// </summary>
        /// <returns>The number of values added to the value set.</returns>
        public int AddRange(TKey key, IEnumerable<TValue> collection) => AddRange(key, collection, out _);

        /// <summary>
        /// Adds the elements of a collection into the value set associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value set that should have values added.</param>
        /// <param name="collection">The collection of elements to add to the value set.</param>
        /// <param name="resultingValueSet">The resulting value set associated with the specified key, or <see langword="null"/> if the collection provided was
        /// empty and the key does not exist in the dictionary.</param>
        /// <returns>The number of values added to the value set.</returns>
        public int AddRange(TKey key, IEnumerable<TValue> collection, out ValueSet? resultingValueSet)
        {
            HashSet<TValue> set;
            int added = 0;

            if (_lookup.TryGetValue(key, out var valueSet)) {
                if (collection is ICollection<TValue> c && c.Count == 0) {
                    resultingValueSet = null;
                    return 0;
                }

                if (valueSet == collection) {
                    resultingValueSet = valueSet;
                    return 0;
                }

                set = valueSet.WrappedSet;

                foreach (var value in collection) {
                    if (set.Add(value))
                        added++;
                }
            }
            else {
                if ((collection is ICollection<TValue> c && c.Count == 0) || (set = collection.ToHashSet(_valueComparer)).Count == 0) {
                    resultingValueSet = null;
                    return 0;
                }

                valueSet = new(this, key, set);
                _lookup.Add(key, valueSet);
                added = set.Count;
            }

            _valueCount += added;
            _version++;

            resultingValueSet = valueSet;
            return added;
        }

        /// <summary>
        /// Clears all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            if (_valueCount > 0) {
                foreach (var valueSet in _lookup.Values)
                    valueSet.Detach();

                _lookup.Clear();

                _valueCount = 0;
                _version++;
            }
        }

        /// <summary>
        /// Returns a value indicating whether the specified key and associated value are present in the dictionary.
        /// </summary>
        public bool Contains(TKey key, TValue value)
        {
            if (_lookup.TryGetValue(key, out var valueSet))
                return valueSet.WrappedSet.Contains(value);

            return false;
        }

        /// <summary>
        /// Returns a value indicating whether the value set associated with the specified key contains any element in the collection provided. Always returns
        /// <see langword="true"/> if the collection provided is empty. Returns <see langword="false"/> if the collection is not empty but the key is not found.
        /// </summary>
        public bool ContainsAll(TKey key, IEnumerable<TValue> collection)
        {
            var c = collection as ICollection<TValue>;

            if (c?.Count == 0)
                return true;

            if (_lookup.TryGetValue(key, out var valueSet))
                return valueSet.IsSupersetOf(collection);

            return c == null && !collection.Any();
        }

        /// <summary>
        /// Returns a value indicating whether the value set associated with the specified key contains all the elements in the collection provided. Always
        /// returns <see langword="false"/> if the collection provided is empty or the key is not found.
        /// </summary>
        public bool ContainsAny(TKey key, IEnumerable<TValue> collection)
        {
            if (_lookup.TryGetValue(key, out var valueSet))
                return valueSet.Overlaps(collection);

            return false;
        }

        /// <summary>
        /// Returns a value indicating whether the dictionary contains the specified key.
        /// </summary>
        public bool ContainsKey(TKey key) => _lookup.ContainsKey(key);

        /// <summary>
        /// Returns a value indicating whether any of the value sets in the dictionary contain the specified value.
        /// </summary>
        public bool ContainsValue(TValue value)
        {
            foreach (var valueSet in _lookup.Values) {
                if (valueSet.Contains(value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Ensures that the dictionary can hold up to a specified number of key/value set pairs without any further expansion of its backing storage.
        /// </summary>
        /// <param name="capacity">The number of key/value set pairs.</param>
        /// <returns>The currect capacity of the dictionary.</returns>
        public int EnsureCapacity(int capacity) => _lookup.EnsureCapacity(capacity);

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="HashSetDictionary{TKey, TValue}"/>.
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// Removes all the values associated with the specified key.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>The value set that was removed or <see langword="null"/> if the key was not found.</returns>
        public ValueSet? Remove(TKey key)
        {
            Remove(key, out var valueSet);
            return valueSet;
        }

        /// <summary>
        /// Removes a key and all its associated values from the dictionary.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="removedValueSet">The value set that was removed or <see langword="null"/> if the key was not found.</param>
        /// <returns>A value indicating whether the key was found and removed.</returns>
        public bool Remove(TKey key, [NotNullWhen(true)] out ValueSet? removedValueSet)
        {
            if (_lookup.Remove(key, out var valueSet)) {
                _valueCount -= valueSet.Count;
                _version++;

                removedValueSet = valueSet;
                return true;
            }

            removedValueSet = null;
            return false;
        }

        /// <summary>
        /// Removes a value from the set associated with the specified key. If the value is the last value in the set then the key is also removed.
        /// </summary>
        /// <param name="key">The key of the value set fom which to remove the value.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>A value indicating whether the value was found and removed.</returns>
        public bool Remove(TKey key, TValue value) => Remove(key, value, out _);

        /// <summary>
        /// Removes a value from the set associated with the specified key. If the value is the last value in the set then the key is also removed.
        /// </summary>
        /// <param name="key">The key of the value set fom which to remove the value.</param>
        /// <param name="value">The value to remove.</param>
        /// <param name="remainingValueSet">The remaining value set associated with the specified key, or <see langword="null"/> if there are no values
        /// remaining.</param>
        /// <returns>A value indicating whether the key was found and the value was found and removed.</returns>
        public bool Remove(TKey key, TValue value, out ValueSet? remainingValueSet)
        {
            if (_lookup.TryGetValue(key, out var valueSet)) {
                var set = valueSet.WrappedSet;

                if (set.Remove(value)) {
                    if (set.Count == 0) {
                        _lookup.Remove(key);
                        remainingValueSet = null;
                    }
                    else {
                        remainingValueSet = valueSet;
                    }

                    _valueCount--;
                    _version++;

                    return true;
                }

                remainingValueSet = valueSet;
            }
            else {
                remainingValueSet = null;
            }

            return false;
        }

        /// <summary>
        /// Removes values from the value set associated with the specified key. If all the values are removed from the value set then the key is also removed.
        /// </summary>
        /// <param name="key">The key of the value set to remove the values from.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The number of values removed.</returns>
        public int Remove(TKey key, IEnumerable<TValue> values) => Remove(key, values, out _);

        /// <summary>
        /// Removes values from the value set associated with the specified key. If all the values are removed from the value set then the key is also removed.
        /// </summary>
        /// <param name="key">The key of the value set to remove the values from.</param>
        /// <param name="values">The values to remove.</param>
        /// <param name="remainingValueSet">The remaining set of values on the specified key, or <see langword="null"/> if there are no values remaining.</param>
        /// <returns>The number of values removed.</returns>
        public int Remove(TKey key, IEnumerable<TValue> values, out ValueSet? remainingValueSet)
        {
            if (!_lookup.TryGetValue(key, out var valueSet)) {
                remainingValueSet = null;
                return 0;
            }

            var set = valueSet.WrappedSet;

            if (valueSet == values) {
                _lookup.Remove(key);

                _valueCount -= set.Count;
                _version++;

                remainingValueSet = null;
                return set.Count;
            }

            int removed = 0;

            foreach (var value in values) {
                if (set.Remove(value)) {
                    removed++;

                    if (set.Count == 0) {
                        _lookup.Remove(key);

                        _valueCount -= removed;
                        _version++;

                        remainingValueSet = null;
                        return removed;
                    }
                }
            }

            if (removed > 0) {
                _valueCount -= removed;
                _version++;
            }

            remainingValueSet = valueSet;
            return removed;
        }

        /// <summary>
        /// Sets the values associated with the specified key. If the collection provided is empty then the key is removed from the dictionary.
        /// </summary>
        /// <returns>The value set associated with the specified key or <see langword="null"/> if the values collection provided was empty.</returns>
        public ValueSet? Set(TKey key, IEnumerable<TValue> collection)
        {
            HashSet<TValue> set;

            if (_lookup.TryGetValue(key, out var valueSet)) {
                if (collection == valueSet)
                    return valueSet;

                set = valueSet.WrappedSet;
                _valueCount -= set.Count;
                _version++;
                set.Clear();

                if (collection is not ICollection<TValue> c || c.Count > 0) {
                    foreach (var value in collection)
                        set.Add(value);
                }

                if (set.Count == 0) {
                    _lookup.Remove(key);
                    return null;
                }

                _valueCount += set.Count;
            }
            else {
                if ((collection is ICollection<TValue> c && c.Count == 0) || (set = collection.ToHashSet(_valueComparer)).Count == 0)
                    return null;

                valueSet = new(this, key, set);
                _lookup.Add(key, valueSet);

                _valueCount += set.Count;
                _version++;
            }

            return valueSet;
        }

        /// <summary>
        /// Gets the values for the specified key or <see langword="null"/> if the key was not found.
        /// </summary>
        /// <returns>A value indicating whether the key was found.</returns>
        public bool TryGetValues(TKey key, [NotNullWhen(true)] out ValueSet? valueSet) => _lookup.TryGetValue(key, out valueSet);

        /// <summary>
        /// Sets the key/value set pair capacity of this dictionary to what it would be if it had been originally initialized with all its entries, and
        /// optionally trims all the value sets in the dictionary as well.
        /// </summary>
        /// <param name="trimValueSets"><see langword="true"/> to trim all the value sets as well, or <see langword="false"/> to only trim the
        /// dictionary.</param>
        public void TrimExcess(bool trimValueSets = true)
        {
            _lookup.TrimExcess();

            if (trimValueSets) {
                foreach (var valueSet in _lookup.Values)
                    valueSet.TrimExcess();
            }
        }

        private void CheckVersion(int enumeratorVersion)
        {
            if (enumeratorVersion != _version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
        }

        #region Explicit Interface Implementations

        /// <inheritdoc/>
        ICollection<TKey> IDictionary<TKey, ValueSet>.Keys => Keys;

        /// <inheritdoc/>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, ValueSet>.Keys => Keys;

        /// <inheritdoc/>
        ICollection<ValueSet> IDictionary<TKey, ValueSet>.Values => ValueSets;

        /// <inheritdoc/>
        IEnumerable<ValueSet> IReadOnlyDictionary<TKey, ValueSet>.Values => ValueSets;

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, ValueSet>>.IsReadOnly => true;

        /// <summary>
        /// <inheritdoc/>
        /// Setter is not supported.
        /// </summary>
        /// <inheritdoc/>
        ValueSet IDictionary<TKey, ValueSet>.this[TKey key] {
            get => this[key] ?? throw new KeyNotFoundException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        ValueSet IReadOnlyDictionary<TKey, ValueSet>.this[TKey key] => this[key] ?? throw new KeyNotFoundException();

        /// <inheritdoc/>
        bool IDictionary<TKey, ValueSet>.TryGetValue(TKey key, [MaybeNullWhen(false)] out ValueSet value)
        {
            return TryGetValues(key, out value);
        }

        /// <inheritdoc/>
        bool IReadOnlyDictionary<TKey, ValueSet>.TryGetValue(TKey key, [MaybeNullWhen(false)] out ValueSet value)
        {
            return TryGetValues(key, out value);
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, ValueSet>>.Contains(KeyValuePair<TKey, ValueSet> item)
        {
            return _lookup.TryGetValue(item.Key, out var valueSet) && valueSet == item.Value;
        }

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, ValueSet>>.CopyTo(KeyValuePair<TKey, ValueSet>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, ValueSet>>)_lookup).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        IEnumerator<KeyValuePair<TKey, ValueSet>> IEnumerable<KeyValuePair<TKey, ValueSet>>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Not Supported

        /// <inheritdoc/>
        void IDictionary<TKey, ValueSet>.Add(TKey key, ValueSet value) => throw new NotSupportedException();

        /// <inheritdoc/>
        bool IDictionary<TKey, ValueSet>.Remove(TKey key) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, ValueSet>>.Add(KeyValuePair<TKey, ValueSet> item) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, ValueSet>>.Clear() => throw new NotSupportedException();

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, ValueSet>>.Remove(KeyValuePair<TKey, ValueSet> item) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// Enumerates the elements of a <see cref="HashSetDictionary{TKey, TValue}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, ValueSet>>
        {
            private readonly HashSetDictionary<TKey, TValue> _dictionary;
            private readonly Dictionary<TKey, ValueSet>.Enumerator _enumerator;
            private readonly int _version;

            /// <inheritdoc/>
            public KeyValuePair<TKey, ValueSet> Current => _enumerator.Current;

            /// <inheritdoc/>
            object IEnumerator.Current => Current;

            internal Enumerator(HashSetDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _enumerator = dictionary._lookup.GetEnumerator();
                _version = _dictionary._version;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                _dictionary.CheckVersion(_version);
                return _enumerator.MoveNext();
            }

            /// <inheritdoc/>
            void IEnumerator.Reset() => ((IEnumerator)_enumerator).Reset();

            /// <inheritdoc/>
            public void Dispose() => _enumerator.Dispose();
        }
    }
}