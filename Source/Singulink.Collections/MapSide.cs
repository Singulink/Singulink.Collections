using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections
{
    /// <summary>
    /// Represents the left or right side of a <see cref="Map{TLeft, TRight}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type for this map side.</typeparam>
    /// <typeparam name="TValue">The value type for this map side.</typeparam>
    public sealed class MapSide<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> _lookup;
        private readonly Dictionary<TValue, TKey> _reverseLookup;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <exception cref="ArgumentNullException">The specified key was null.</exception>
        /// <exception cref="KeyNotFoundException">The key was not found.</exception>
        /// <exception cref="ArgumentException">The value already exists on another key on this map side.</exception>
        public TValue this[TKey key] {
            get => _lookup[key];
            set {
                if (_reverseLookup.TryGetValue(value, out var existingKey)) {
                    if (_lookup.Comparer.Equals(key, existingKey))
                        return;

                    throw new ArgumentException("Duplicate value on this map side.");
                }

                _lookup[key] = value;
                _reverseLookup.Add(value, key);
            }
        }

        /// <summary>
        /// Gets the number of entries in the map.
        /// </summary>
        public int Count => _lookup.Count;

        /// <summary>
        /// Gets a collection containing the keys in this map side (which are the values on the other map side).
        /// </summary>
        public Dictionary<TKey, TValue>.KeyCollection Keys => _lookup.Keys;

        /// <summary>
        /// Gets a collection containing the values in this map side (which are the keys on the other map side).
        /// </summary>
        public Dictionary<TValue, TKey>.KeyCollection Values => _reverseLookup.Keys;

        internal Dictionary<TKey, TValue> Lookup => _lookup;

        internal MapSide(Dictionary<TKey, TValue> lookup, Dictionary<TValue, TKey> reverseLookup)
        {
            _lookup = lookup;
            _reverseLookup = reverseLookup;
        }

        /// <summary>
        /// Adds the specified key and value to this map side and the reverse to the other map side.
        /// </summary>
        /// <param name="key">The key to add to this map side (and the value on the other map side).</param>
        /// <param name="value">The value to add to this map side (and the key on the other map side).</param>
        /// <exception cref="ArgumentException">The key or value already exist in the map.</exception>
        public void Add(TKey key, TValue value)
        {
            if (!_lookup.TryAdd(key, value))
                throw new ArgumentException("Duplicate key on this map side.", nameof(key));

            if (!_reverseLookup.TryAdd(value, key)) {
                _lookup.Remove(key);
                throw new ArgumentException("Duplicate value on this map side.", nameof(value));
            }
        }

        /// <summary>
        /// Determines whether this map side contains the specified key and value association.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><see langword="true"/> if this map side contains the specified key and value, otherwise <see langword="false"/>.</returns>
        public bool Contains(TKey key, TValue value)
        {
            return _lookup.TryGetValue(key, out var existingValue) && _reverseLookup.Comparer.Equals(existingValue, value);
        }

        /// <summary>
        /// Determines whether this map side contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><see langword="true"/> if this map side contains the specified key, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified key was null.</exception>
        public bool ContainsKey(TKey key) => _lookup.ContainsKey(key);

        /// <summary>
        /// Determines whether this map side contains the specified value.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns><see langword="true"/> if this map side contains the specified value, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified value was null.</exception>
        public bool ContainsValue(TValue value) => _reverseLookup.ContainsKey(value);

        /// <summary>
        /// Removes an association from the map given the specified key on this side of the map.
        /// </summary>
        /// <returns><see langword="true"/> if this key is successfully found and removed, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified key was null.</exception>
        public bool Remove(TKey key)
        {
            if (_lookup.Remove(key, out var removedValue)) {
                bool result = _reverseLookup.Remove(removedValue);
                Debug.Assert(result, "reverse map side remove failure");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes an association from the map given the specified key and value on this side of the map.
        /// </summary>
        /// <returns><see langword="true"/> if this key and value were successfully found and removed, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified key or value was null.</exception>
        public bool Remove(TKey key, TValue value)
        {
            if (!Contains(key, value))
                return false;

            _lookup.Remove(key);
            _reverseLookup.Remove(value);
            return true;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default
        /// value for the type of the value parameter.</param>
        /// <returns><see langword="true"/> if this map side contains the specified key, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified key was null.</exception>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _lookup.TryGetValue(key, out value);

        /// <summary>
        /// Returns an enumerator that iterates through the keys and values on this map side.
        /// </summary>
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _lookup.GetEnumerator();

        #region Explicit Interface Implementations

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        /// <inheritdoc/>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        /// <inheritdoc/>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        /// <inheritdoc/>
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        /// <inheritdoc/>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        /// <inheritdoc/>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            _lookup.Clear();
            _reverseLookup.Clear();
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => Contains(item.Key, item.Value);

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_lookup).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key, item.Value);

        #endregion
    }
}