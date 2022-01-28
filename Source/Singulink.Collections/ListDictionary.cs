using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Singulink.Collections
{
    /// <summary>
    /// Represents a collection of keys and lists of values.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// <para>Please note that the <see cref="IDictionary{TKey, TValue}"/> and <see cref="ICollection{T}"/> implementation is read-only because many of the
    /// operations that modify the dictionary have substantially different behavior than is typically expected from these interfaces.</para>
    /// </remarks>
    public partial class ListDictionary<TKey, TValue> : IDictionary<TKey, ListDictionary<TKey, TValue>.ValueList>, IReadOnlyDictionary<TKey, ListDictionary<TKey, TValue>.ValueList>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, ValueList> _lookup;

        private KeyCollection? _keys;
        private ValueListCollection? _valueLists;
        private ValueCollection? _values;
        private int _valueCount = 0;
        private int _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListDictionary{TKey, TValue}"/> class.
        /// </summary>
        public ListDictionary() : this(0, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListDictionary{TKey, TValue}"/> class with the specified initial capacity for key/value list pairs.
        /// </summary>
        public ListDictionary(int capacity) : this(capacity, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListDictionary{TKey, TValue}"/> class that uses the specified key comparer.
        /// </summary>
        public ListDictionary(IEqualityComparer<TKey>? keyComparer) : this(0, keyComparer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListDictionary{TKey, TValue}"/> class with the specified initial capacity for key/value list pairs,
        /// and uses the specified key comparer.
        /// </summary>
        public ListDictionary(int capacity, IEqualityComparer<TKey>? keyComparer)
        {
            _lookup = new(capacity, keyComparer);
        }

        /// <summary>
        /// Gets the value list associated with the specified key or <see langword="null"/> if the key was not found.
        /// </summary>
        /// <exception cref="ArgumentNullException">The specified key was <see langword="null"/>.</exception>
        public ValueList? this[TKey key] => _lookup.TryGetValue(key, out var valueList) ? valueList : null;

        /// <summary>
        /// Gets the number of keys and associated value lists in the dictionary.
        /// </summary>
        public int Count => _lookup.Count;

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public KeyCollection Keys => _keys ??= new KeyCollection(this);

        /// <summary>
        /// Gets a collection containing the value lists in the dictionary.
        /// </summary>
        public ValueListCollection ValueLists => _valueLists ??= new ValueListCollection(this);

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
        /// Adds a value to the value list for the specified key.
        /// </summary>
        public void Add(TKey key, TValue value) => Add(key, value, out _);

        /// <summary>
        /// Adds a value to the value list for the specified key.
        /// </summary>
        public void Add(TKey key, TValue value, out ValueList resultingValueList)
        {
            List<TValue> list;

            if (_lookup.TryGetValue(key, out var valueList)) {
                list = valueList.WrappedList;
            }
            else {
                list = new();
                valueList = new(this, key, list);
                _lookup.Add(key, valueList);
            }

            list.Add(value);
            _valueCount++;
            _version++;

            resultingValueList = valueList;
        }

        /// <inheritdoc cref="AddRange(TKey, IEnumerable{TValue}, out ValueList?)"/>
        public void AddRange(TKey key, IEnumerable<TValue> collection) => AddRange(key, collection, out _);

        /// <summary>
        /// Adds the elements of a collection into the value list associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value list that should have values added.</param>
        /// <param name="collection">The collection of elements to add to the value list.</param>
        /// <param name="resultingValueList">The resulting value list associated with the specified key, or <see langword="null"/> if the collection provided
        /// was empty and the key does not exist in the dictionary.</param>
        public void AddRange(TKey key, IEnumerable<TValue> collection, out ValueList? resultingValueList)
        {
            List<TValue> list;

            if (_lookup.TryGetValue(key, out var valueList)) {
                list = valueList.WrappedList;
                int oldCount = list.Count;
                list.AddRange(collection);
                int added = list.Count - oldCount;

                if (added > 0) {
                    _valueCount += added;
                    _version++;
                }
            }
            else {
                if ((collection is ICollection<TValue> c && c.Count == 0) || (list = collection.ToList()).Count == 0) {
                    resultingValueList = null;
                    return;
                }

                valueList = new ValueList(this, key, list);
                _lookup.Add(key, valueList);
                _valueCount += list.Count;
                _version++;
            }

            resultingValueList = valueList;
        }

        /// <summary>
        /// Clears all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            if (_valueCount > 0) {
                foreach (var valueList in _lookup.Values)
                    valueList.Detach();

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
            if (_lookup.TryGetValue(key, out var valueList))
                return valueList.Contains(value);

            return false;
        }

        /// <summary>
        /// Returns a value indicating whether the specified key and associated value are present in the dictionary.
        /// </summary>
        public bool Contains(TKey key, TValue value, IEqualityComparer<TValue>? valueComparer = null)
        {
            if (_lookup.TryGetValue(key, out var valueList))
                return valueComparer == null ? valueList.Contains(value) : valueList.Contains(value, valueComparer);

            return false;
        }

        /// <summary>
        /// Returns a value indicating whether the dictionary contains the specified key.
        /// </summary>
        public bool ContainsKey(TKey key) => _lookup.ContainsKey(key);

        /// <summary>
        /// Returns a value indicating whether any of the value lists in the dictionary contain the specified value.
        /// </summary>
        public bool ContainsValue(TValue value)
        {
            foreach (var valueList in _lookup.Values) {
                if (valueList.Contains(value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a value indicating whether any of the value lists in the dictionary contain the specified value.
        /// </summary>
        public bool ContainsValue(TValue value, IEqualityComparer<TValue>? valueComparer = null)
        {
            if (valueComparer == null) {
                foreach (var valueList in _lookup.Values) {
                    if (valueList.Contains(value))
                        return true;
                }
            }
            else {
                foreach (var valueList in _lookup.Values) {
                    if (valueList.Contains(value, valueComparer))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Ensures that the dictionary can hold up to a specified number of key/value list pairs without any further expansion of its backing storage.
        /// </summary>
        /// <param name="capacity">The number of key/value list pairs.</param>
        /// <returns>The currect capacity of the dictionary.</returns>
        public int EnsureCapacity(int capacity) => _lookup.EnsureCapacity(capacity);

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="HashSetDictionary{TKey, TValue}"/>.
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// Gets the number of values in the dictionary associated with the specified key or 0 if the key is not present.
        /// </summary>
        public int GetValueCount(TKey key) => _lookup.TryGetValue(key, out var valueList) ? valueList.Count : 0;

        /// <inheritdoc cref="Insert(TKey, int, TValue, out ValueList)"/>
        public void Insert(TKey key, int index, TValue value) => Insert(key, index, value, out _);

        /// <summary>
        /// Inserts a value to the value list for the specified key at the specified index.
        /// </summary>
        public void Insert(TKey key, int index, TValue value, out ValueList resultingValueList)
        {
            List<TValue> list;

            if (_lookup.TryGetValue(key, out var valueList)) {
                list = valueList.WrappedList;
            }
            else {
                if (index != 0)
                    throw GetIndexOutOfRangeException();

                list = new();
                valueList = new(this, key, list);
                _lookup.Add(key, valueList);
            }

            list.Insert(index, value);
            _valueCount++;
            _version++;

            resultingValueList = valueList;
        }

        /// <inheritdoc cref="InsertRange(TKey, int, IEnumerable{TValue}, out ValueList?)"/>
        public void InsertRange(TKey key, int index, IEnumerable<TValue> collection) => InsertRange(key, index, collection, out _);

        /// <summary>
        /// Inserts the elements of a collection into the value list associated with the specified key at the specified index.
        /// </summary>
        /// <param name="key">The key of the value list that should have values inserted.</param>
        /// <param name="index">The zero-based index to insert the elements at.</param>
        /// <param name="collection">The collection of elements to insert into the value list.</param>
        /// <param name="resultingValueList">The resulting value list associated with the specified key, or <see langword="null"/> if the collection provided
        /// was empty and the key does not exist in the dictionary.</param>
        public void InsertRange(TKey key, int index, IEnumerable<TValue> collection, out ValueList? resultingValueList)
        {
            List<TValue> list;

            if (_lookup.TryGetValue(key, out var valueList)) {
                list = valueList.WrappedList;
                int oldCount = list.Count;
                list.InsertRange(index, collection);
                int added = list.Count - oldCount;

                if (added > 0) {
                    _valueCount += added;
                    _version++;
                }
            }
            else {
                if (index != 0)
                    throw GetIndexOutOfRangeException();

                if ((collection is ICollection<TValue> c && c.Count == 0) || (list = collection.ToList()).Count == 0) {
                    resultingValueList = null;
                    return;
                }

                valueList = new ValueList(this, key, list);
                _lookup.Add(key, valueList);
                _valueCount += list.Count;
                _version++;
            }

            resultingValueList = valueList;
        }

        /// <summary>
        /// Removes all the values associated with the specified key.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>The value list that was removed or <see langword="null"/> if the key was not found.</returns>
        public ValueList? Remove(TKey key)
        {
            Remove(key, out var valueList);
            return valueList;
        }

        /// <summary>
        /// Removes a key and all its associated values from the dictionary.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="removedValueList">The value list that was removed or <see langword="null"/> if the key was not found.</param>
        /// <returns>A value indicating whether the key was found and removed.</returns>
        public bool Remove(TKey key, [NotNullWhen(true)] out ValueList? removedValueList)
        {
            if (!_lookup.Remove(key, out var valueList)) {
                removedValueList = null;
                return false;
            }

            valueList.Detach();
            _valueCount -= valueList.Count;
            _version++;

            removedValueList = valueList;
            return true;
        }

        /// <summary>
        /// Removes a value from the list associated with the specified key. If the value is the last value in the list then the key is also removed.
        /// </summary>
        /// <param name="key">The key of the value list fom which to remove the value.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>A value indicating whether the value was found and removed.</returns>
        public bool Remove(TKey key, TValue value) => Remove(key, value, out _);

        /// <summary>
        /// Removes a value from the list associated with the specified key. If the value is the last value in the list then the key is also removed.
        /// </summary>
        /// <param name="key">The key of the value list fom which to remove the value.</param>
        /// <param name="value">The value to remove.</param>
        /// <param name="remainingValueList">The remaining value list associated with the specified key, or <see langword="null"/> if there are no values
        /// remaining.</param>
        /// <returns>A value indicating whether the key was found and the value was found and removed.</returns>
        public bool Remove(TKey key, TValue value, out ValueList? remainingValueList)
        {
            if (!_lookup.TryGetValue(key, out var valueList)) {
                remainingValueList = null;
                return false;
            }

            var list = valueList.WrappedList;

            if (list.Remove(value)) {
                if (list.Count == 0) {
                    _lookup.Remove(key);
                    valueList.Detach();

                    remainingValueList = null;
                }
                else {
                    remainingValueList = valueList;
                }

                _valueCount--;
                _version++;

                return true;
            }

            remainingValueList = valueList;
            return false;
        }

        /// <summary>
        /// Removes all the values that match the conditions defined by the specified predicate. If all the values in a value list are removed, its associated
        /// key is also removed from the dictionary.
        /// </summary>
        /// <param name="match">The delegate that defines the conditions of the elements to remove.</param>
        /// <returns>The number of values removed.</returns>
        public int RemoveAll(Predicate<TValue> match)
        {
            int removed = 0;

            foreach (var valueList in _lookup.Values) {
                var list = valueList.WrappedList;
                removed += list.RemoveAll(match);

                if (list.Count == 0) {
                    _lookup.Remove(valueList.Key);
                    valueList.Detach();
                }
            }

            if (removed > 0) {
                _valueCount -= removed;
                _version++;
            }

            return removed;
        }

        /// <inheritdoc cref="RemoveAll(TKey, Predicate{TValue}, out ValueList?)"/>
        public int RemoveAll(TKey key, Predicate<TValue> match) => RemoveAll(key, match, out _);

        /// <summary>
        /// Removes all the values associated with the specified key that match the conditions defined by the specified predicate. If all the values in the list
        /// are removed, the key is also removed from the dictionary.
        /// </summary>
        /// <param name="key">The key of the value list that should have values inserted.</param>
        /// <param name="match">The delegate that defines the conditions of the elements to remove.</param>
        /// <param name="remainingValueList">The remaining list of values associated with the specified key, or <see langword="null"/> if there are no values
        /// remaining.</param>
        /// <returns>The number of values removed.</returns>
        public int RemoveAll(TKey key, Predicate<TValue> match, out ValueList? remainingValueList)
        {
            if (!_lookup.TryGetValue(key, out var valueList)) {
                remainingValueList = null;
                return 0;
            }

            var list = valueList.WrappedList;
            int removed = list.RemoveAll(match);

            if (list.Count == 0) {
                _lookup.Remove(valueList.Key);
                valueList.Detach();
                remainingValueList = null;
            }
            else {
                remainingValueList = valueList;
            }

            if (removed > 0) {
                _valueCount -= removed;
                _version++;
            }

            return removed;
        }

        /// <summary>
        /// Removes the value from the value list associated with the specified key at the specified index.
        /// </summary>
        /// <param name="key">The key of the value list that should have the value removed.</param>
        /// <param name="index">The zero-based index of the value to remove.</param>
        /// <exception cref="KeyNotFoundException">The key was not found.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The index was outside the bounds of the value list.</exception>
        public void RemoveAt(TKey key, int index)
        {
            if (!_lookup.TryGetValue(key, out var valueList))
                throw new KeyNotFoundException();

            var list = valueList.WrappedList;
            list.RemoveAt(index);
            _valueCount--;
            _version++;

            if (list.Count == 0) {
                _lookup.Remove(valueList.Key);
                valueList.Detach();
            }
        }

        /// <summary>
        /// Removes a range of values from the value list associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value list that should have the values removed.</param>
        /// <param name="index">The zero-based strating index of the range of values to remove.</param>
        /// <param name="count">The number of values to remove.</param>
        /// <exception cref="KeyNotFoundException">The key was not found.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The index or count were less than zero.</exception>
        /// <exception cref="ArgumentException">The index and count do not denote a valid range of elements in the value list.</exception>
        public void RemoveRange(TKey key, int index, int count)
        {
            if (!_lookup.TryGetValue(key, out var valueList))
                throw new KeyNotFoundException();

            var list = valueList.WrappedList;
            list.RemoveRange(index, count);
            _valueCount -= count;
            _version++;

            if (list.Count == 0) {
                _lookup.Remove(valueList.Key);
                valueList.Detach();
            }
        }

        /// <summary>
        /// Removes the elements of a collection from the value list associated with the specified key. If all the values are removed from the value list then
        /// the key is also removed.
        /// </summary>
        /// <param name="key">The key of the value list to remove the values from.</param>
        /// <param name="collection">The collection of elements to remove.</param>
        /// <returns>The number of values removed.</returns>
        public int RemoveRange(TKey key, IEnumerable<TValue> collection) => RemoveRange(key, collection, out _);

        /// <summary>
        /// Removes the elements of a collection from the value list associated with the specified key. If all the values are removed from the value list then
        /// the key is also removed.
        /// </summary>
        /// <param name="key">The key of the value list to remove the values from.</param>
        /// <param name="collection">The collection of elements to remove.</param>
        /// <param name="remainingValueList">The remaining list of values associated with the specified key, or <see langword="null"/> if there are no values
        /// remaining.</param>
        /// <returns>The number of values removed.</returns>
        public int RemoveRange(TKey key, IEnumerable<TValue> collection, out ValueList? remainingValueList)
        {
            if (!_lookup.TryGetValue(key, out var valueList)) {
                remainingValueList = null;
                return 0;
            }

            if (valueList == collection) {
                _lookup.Remove(key);
                valueList.Detach();

                _valueCount -= valueList.Count;
                _version++;

                remainingValueList = null;
                return valueList.Count;
            }

            var list = valueList.WrappedList;
            int removed = 0;

            foreach (var value in collection) {
                if (list.Remove(value)) {
                    removed++;

                    if (list.Count == 0) {
                        _lookup.Remove(key);
                        valueList.Detach();

                        _valueCount -= removed;
                        _version++;

                        remainingValueList = null;
                        return removed;
                    }
                }
            }

            if (removed > 0) {
                _valueCount -= removed;
                _version++;
            }

            remainingValueList = valueList;
            return removed;
        }

        /// <summary>
        /// Reverses the order of the values in each value list.
        /// </summary>
        public void Reverse()
        {
            foreach (var valueList in _lookup.Values)
                valueList.WrappedList.Reverse();
        }

        /// <summary>
        /// Reverses the order of the values in the value list associated with the specified key.
        /// </summary>
        /// <returns>The value list that was reversed, or <see langword="null"/> if the key was not found.</returns>
        public ValueList? Reverse(TKey key)
        {
            if (!_lookup.TryGetValue(key, out var valueList))
                return null;

            valueList.WrappedList.Reverse();
            return valueList;
        }

        /// <summary>
        /// Reverses the order of values in the specified range of the value list associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value list which should have elements reversed.</param>
        /// <param name="index">The zero-based starting index of the range to reverse.</param>
        /// <param name="count">The number of values in the range to reverse.</param>
        /// <exception cref="KeyNotFoundException">The key was not found.</exception>
        public void Reverse(TKey key, int index, int count)
        {
            if (!_lookup.TryGetValue(key, out var valueList))
                throw new KeyNotFoundException();

            valueList.WrappedList.Reverse(index, count);
        }

        /// <summary>
        /// Sets the values associated with the specified key. If the collection provided is empty then the key is removed from the dictionary.
        /// </summary>
        /// <returns>The value list associated with the specified key or <see langword="null"/> if the collection provided was empty.</returns>
        public ValueList? SetRange(TKey key, IEnumerable<TValue> collection)
        {
            List<TValue> list;

            if (_lookup.TryGetValue(key, out var valueList)) {
                if (collection == valueList)
                    return valueList;

                list = valueList.WrappedList;

                _valueCount -= list.Count;
                _version++;
                list.Clear();
                list.AddRange(collection);

                if (list.Count == 0) {
                    _lookup.Remove(key);
                    return null;
                }

                _valueCount += list.Count;
            }
            else {
                if ((collection is ICollection<TValue> c && c.Count == 0) || (list = collection.ToList()).Count == 0)
                    return null;

                valueList = new(this, key, list);
                _lookup.Add(key, valueList);

                _valueCount += list.Count;
                _version++;
            }

            return valueList;
        }

        /// <summary>
        /// Sorts the values in each value list using the default comparer.
        /// </summary>
        public void Sort()
        {
            foreach (var valueList in _lookup.Values) {
                valueList.WrappedList.Sort();
            }
        }

        /// <summary>
        /// Sorts the values in the value list associated with the specified key using the default comparer.
        /// </summary>
        /// <returns>The resulting sorted value list associated with the key or <see langword="null"/> if the key was not found.</returns>
        public ValueList? Sort(TKey key)
        {
            if (!_lookup.TryGetValue(key, out var valueList))
                return null;

            valueList.WrappedList.Sort();
            return valueList;
        }

        /// <summary>
        /// Sorts the values in each value list using the specified comparer.
        /// </summary>
        public void Sort(IComparer<TValue>? comparer)
        {
            foreach (var valueList in _lookup.Values) {
                valueList.WrappedList.Sort(comparer);
            }
        }

        /// <summary>
        /// Sorts the values in the value list associated with the specified key using the specified comparer.
        /// </summary>
        /// <returns>The resulting sorted value list associated with the key or <see langword="null"/> if the key was not found.</returns>
        public ValueList? Sort(TKey key, IComparer<TValue>? comparer)
        {
            if (!_lookup.TryGetValue(key, out var valueList))
                return null;

            valueList.WrappedList.Sort(comparer);
            return valueList;
        }

        /// <summary>
        /// Sorts the values in each value list using the specified comparison.
        /// </summary>
        public void Sort(Comparison<TValue> comparison)
        {
            foreach (var valueList in _lookup.Values) {
                valueList.WrappedList.Sort(comparison);
            }
        }

        /// <summary>
        /// Sorts the values in the value list associated with the specified key using the specified comparison.
        /// </summary>
        /// <returns>The resulting sorted value list associated with the key or <see langword="null"/> if the key was not found.</returns>
        public ValueList? Sort(TKey key, Comparison<TValue> comparison)
        {
            if (!_lookup.TryGetValue(key, out var valueList))
                return null;

            valueList.WrappedList.Sort(comparison);
            return valueList;
        }

        /// <summary>
        /// Sorts the values in a range of elements in the value list associated with the specified key using the specified comparer.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The key was not found.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The index or count were less than zero.</exception>
        /// <exception cref="ArgumentException">The index and count do not denote a valid range of elements in the value list.</exception>
        public void Sort(TKey key, int index, int count, IComparer<TValue>? comparer)
        {
            if (!_lookup.TryGetValue(key, out var valueList))
                throw new KeyNotFoundException();

            valueList.WrappedList.Sort(index, count, comparer);
        }

        /// <summary>
        /// Sets the key/value list pair capacity of this dictionary to what it would be if it had been originally initialized with all its entries, and
        /// optionally sets the capacity of each value list to the actual number of values in the list.
        /// </summary>
        /// <param name="trimValueLists"><see langword="true"/> to trim all the value lists as well, or <see langword="false"/> to only trim the
        /// dictionary.</param>
        public void TrimExcess(bool trimValueLists = true)
        {
            _lookup.TrimExcess();

            if (trimValueLists) {
                foreach (var valueList in _lookup.Values)
                    valueList.TrimExcess();
            }
        }

        /// <summary>
        /// Sets the key/value list pair capacity of this dictionary to hold up a specified number of entries without any further expansion of its backing
        /// storage, and optionally sets the capacity of each value list to the actual number of values in the list.
        /// </summary>
        /// <param name="dictionaryCapacity">The new key/value list pair capacity.</param>
        /// <param name="trimValueLists"><see langword="true"/> to trim all the value lists as well, or <see langword="false"/> to only trim the
        /// dictionary.</param>
        /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than the number of entries in the dictionary.</exception>
        public void TrimExcess(int dictionaryCapacity, bool trimValueLists = true)
        {
            _lookup.TrimExcess(dictionaryCapacity);

            if (trimValueLists) {
                foreach (var valueList in _lookup.Values)
                    valueList.TrimExcess();
            }
        }

        /// <summary>
        /// Gets the values for the specified key or <see langword="null"/> if the key was not found.
        /// </summary>
        /// <returns>A value indicating whether the key was found.</returns>
        public bool TryGetValues(TKey key, [NotNullWhen(true)] out ValueList? valueList) => _lookup.TryGetValue(key, out valueList);

        private void CheckVersion(int enumeratorVersion)
        {
            if (enumeratorVersion != _version)
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
        }

        private static Exception GetIndexOutOfRangeException() => new ArgumentOutOfRangeException("index", "Index must be within the bounds of the value list.");

        #region Explicit Interface Implementations

        /// <inheritdoc/>
        ICollection<TKey> IDictionary<TKey, ValueList>.Keys => Keys;

        /// <inheritdoc/>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, ValueList>.Keys => Keys;

        /// <inheritdoc/>
        ICollection<ValueList> IDictionary<TKey, ValueList>.Values => ValueLists;

        /// <inheritdoc/>
        IEnumerable<ValueList> IReadOnlyDictionary<TKey, ValueList>.Values => ValueLists;

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, ValueList>>.IsReadOnly => true;

        /// <summary>
        /// <inheritdoc/>
        /// Setter is not supported.
        /// </summary>
        /// <inheritdoc/>
        ValueList IDictionary<TKey, ValueList>.this[TKey key] {
            get => this[key] ?? throw new KeyNotFoundException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        ValueList IReadOnlyDictionary<TKey, ValueList>.this[TKey key] => this[key] ?? throw new KeyNotFoundException();

        /// <inheritdoc/>
        bool IDictionary<TKey, ValueList>.TryGetValue(TKey key, [MaybeNullWhen(false)] out ValueList value)
        {
            return TryGetValues(key, out value);
        }

        /// <inheritdoc/>
        bool IReadOnlyDictionary<TKey, ValueList>.TryGetValue(TKey key, [MaybeNullWhen(false)] out ValueList value)
        {
            return TryGetValues(key, out value);
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, ValueList>>.Contains(KeyValuePair<TKey, ValueList> item)
        {
            return _lookup.TryGetValue(item.Key, out var valueSet) && valueSet == item.Value;
        }

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, ValueList>>.CopyTo(KeyValuePair<TKey, ValueList>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, ValueList>>)_lookup).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        IEnumerator<KeyValuePair<TKey, ValueList>> IEnumerable<KeyValuePair<TKey, ValueList>>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Not Supported

        /// <inheritdoc/>
        void IDictionary<TKey, ValueList>.Add(TKey key, ValueList value) => throw new NotSupportedException();

        /// <inheritdoc/>
        bool IDictionary<TKey, ValueList>.Remove(TKey key) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, ValueList>>.Add(KeyValuePair<TKey, ValueList> item) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, ValueList>>.Clear() => throw new NotSupportedException();

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, ValueList>>.Remove(KeyValuePair<TKey, ValueList> item) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// Enumerates the elements of a <see cref="HashSetDictionary{TKey, TValue}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, ValueList>>
        {
            private readonly ListDictionary<TKey, TValue> _dictionary;
            private readonly Dictionary<TKey, ValueList>.Enumerator _enumerator;
            private readonly int _version;

            /// <inheritdoc/>
            public KeyValuePair<TKey, ValueList> Current => _enumerator.Current;

            /// <inheritdoc/>
            object IEnumerator.Current => Current;

            internal Enumerator(ListDictionary<TKey, TValue> dictionary)
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