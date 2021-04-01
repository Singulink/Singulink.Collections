using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections
{
    public abstract class CollectionDictionary<TKey, TValue, TCollection, TReadOnlyCollection> : IEnumerable<KeyValuePair<TKey, TReadOnlyCollection>>
        where TKey : notnull
        where TCollection : class, ICollection<TValue>
        where TReadOnlyCollection : class, IReadOnlyCollection<TValue>
    {
        private readonly Dictionary<TKey, (TCollection Collection, TReadOnlyCollection ReadOnlyCollection)> _lookup = new();
        private ValueCollection? _values;

        private int _valueCount = 0;

        public CollectionDictionary()
        {
            _lookup = new();
        }

        public CollectionDictionary(int capacity)
        {
            _lookup = new(capacity);
        }

        public TReadOnlyCollection this[TKey key] => _lookup[key].ReadOnlyCollection;

        public Dictionary<TKey, (TCollection Collection, TReadOnlyCollection ReadOnlyCollection)>.KeyCollection Keys => _lookup.Keys;

        public ValueCollection Values => _values ??= new ValueCollection(this);

        public int KeyCount => _lookup.Count;

        public int ValueCount => _valueCount;

        public int GetValueCount(TKey key) => _lookup.TryGetValue(key, out var entry) ? entry.Collection.Count : 0;

        public bool Contains(TKey key, TValue value)
        {
            if (_lookup.TryGetValue(key, out var entry))
                return entry.Collection.Contains(value);

            return false;
        }

        public bool ContainsKey(TKey key) => _lookup.ContainsKey(key);

        public bool ContainsValue(TValue value)
        {
            foreach (var entry in _lookup.Values) {
                if (entry.Collection.Contains(value))
                    return true;
            }

            return false;
        }

        public bool TryGetValues(TKey key, [NotNullWhen(true)] out TReadOnlyCollection? values)
        {
            if (_lookup.TryGetValue(key, out var entry)) {
                values = entry.ReadOnlyCollection;
                return true;
            }

            values = null;
            return false;
        }

        public bool Add(TKey key, TValue value)
        {
            if (!_lookup.TryGetValue(key, out var entry)) {
                entry = CreateCollections();
                _lookup.Add(key, entry);
            }

            if (AddToCollection(entry.Collection, value)) {
                _valueCount++;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _lookup.Clear();
            _valueCount = 0;
        }

        public bool TryRemove(TKey key, [NotNullWhen(true)] out IEnumerable<TValue>? values)
        {
            values = Remove(key);
            return values != null;
        }

        /// <summary>
        /// Removes values with the specified key and returns the values removed (or null if there were no values).
        /// </summary>
        /// <param name="key">The key of the values to remove.</param>
        /// <returns>The number of removed values.</returns>
        public TReadOnlyCollection? Remove(TKey key)
        {
            if (_lookup.Remove(key, out var entry)) {
                _valueCount -= entry.Collection.Count;

                return entry.ReadOnlyCollection;
            }

            return null;
        }

        public bool Remove(TKey key, TValue value)
        {
            if (_lookup.TryGetValue(key, out var entry)) {
                if (entry.Collection.Remove(value)) {
                    if (entry.Collection.Count == 0)
                        _lookup.Remove(key);

                    _valueCount--;
                    return true;
                }
            }

            return false;
        }

        protected abstract (TCollection, TReadOnlyCollection) CreateCollections();

        protected abstract bool AddToCollection(TCollection collection, TValue value);

        #region IEnumerable

        public IEnumerator<KeyValuePair<TKey, TReadOnlyCollection>> GetEnumerator()
        {
            foreach (var entry in _lookup)
                yield return new KeyValuePair<TKey, TReadOnlyCollection>(entry.Key, entry.Value.ReadOnlyCollection);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Nested Types

        public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
        {
            private readonly CollectionDictionary<TKey, TValue, TCollection, TReadOnlyCollection> _dictionary;

            public ValueCollection(CollectionDictionary<TKey, TValue, TCollection, TReadOnlyCollection> dictionary)
            {
                _dictionary = dictionary;
            }

            public int Count => _dictionary.ValueCount;

            public bool Contains(TValue item) => _dictionary.ContainsValue(item);

            void ICollection<TValue>.CopyTo(TValue[] array, int index)
            {
                if ((uint)index > array.Length)
                    throw new IndexOutOfRangeException();

                if (array.Length - index < _dictionary.ValueCount)
                    throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");

                int i = index;

                foreach (var entry in _dictionary._lookup.Values) {
                    foreach (var value in entry.Collection)
                        array[i++] = value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                foreach (var entry in _dictionary._lookup.Values) {
                    foreach (var value in entry.Collection)
                        yield return value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            bool ICollection<TValue>.IsReadOnly => true;

            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

            void ICollection<TValue>.Clear() => throw new NotSupportedException();

            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();
        }

        #endregion
    }
}
