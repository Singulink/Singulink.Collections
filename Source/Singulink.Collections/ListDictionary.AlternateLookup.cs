#if NET9_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Singulink.Collections;

/// <content>
/// Contains the AlternateLookup implementation for ListDictionary.
/// </content>
partial class ListDictionary<TKey, TValue>
{
    /// <summary>
    /// Gets an alternate lookup that can be used to perform operations on this dictionary using <typeparamref name="TAlternateKey"/> instead of <typeparamref
    /// name="TKey"/>.
    /// </summary>
    public AlternateLookup<TAlternateKey> GetAlternateLookup<TAlternateKey>()
        where TAlternateKey : notnull, allows ref struct
    {
        return new AlternateLookup<TAlternateKey>(this, _lookup.GetAlternateLookup<TAlternateKey>());
    }

    /// <inheritdoc cref="GetAlternateLookup{TAlternateKey}"/>
    public bool TryGetAlternateLookup<TAlternateKey>(
        [MaybeNullWhen(false)] out AlternateLookup<TAlternateKey> lookup)
        where TAlternateKey : notnull, allows ref struct
    {
        if (_lookup.TryGetAlternateLookup<TAlternateKey>(out var altLookup))
        {
            lookup = new AlternateLookup<TAlternateKey>(this, altLookup);
            return true;
        }

        lookup = default;
        return false;
    }

    /// <summary>
    /// Provides an alternate lookup that can be used to perform operations on a <see cref="ListDictionary{TKey, TValue}"/> using <typeparamref
    /// name="TAlternateKey"/> instead of <typeparamref name="TKey"/>.
    /// </summary>
    public readonly struct AlternateLookup<TAlternateKey> where TAlternateKey : notnull, allows ref struct
    {
        private readonly ListDictionary<TKey, TValue> _dictionary;
        private readonly Dictionary<TKey, ValueList>.AlternateLookup<TAlternateKey> _altLookup;

        internal AlternateLookup(ListDictionary<TKey, TValue> dictionary, Dictionary<TKey, ValueList>.AlternateLookup<TAlternateKey> altLookup)
        {
            _dictionary = dictionary;
            _altLookup = altLookup;
        }

        /// <summary>
        /// Gets the value list associated with the specified alternate key. If the key is not found then a new value list is returned which can be used to add
        /// values to the key or to monitor when items are added to the key.
        /// </summary>
        public ValueList this[TAlternateKey key]
        {
            get {
                if (_altLookup.TryGetValue(key, out var valueSet))
                {
                    DebugValid(valueSet);
                    return valueSet;
                }

                return new ValueList(_dictionary, Comparer.Create(key));
            }
        }

        /// <summary>
        /// Gets the equality comparer used to compare keys in the alternate lookup.
        /// </summary>
        public IAlternateEqualityComparer<TAlternateKey, TKey> Comparer =>
            Unsafe.As<IAlternateEqualityComparer<TAlternateKey, TKey>>(_altLookup.Dictionary.Comparer);

        /// <summary>
        /// Gets the underlying dictionary associated with this alternate lookup.
        /// </summary>
        public ListDictionary<TKey, TValue> Dictionary => _dictionary;

        /// <inheritdoc cref="Clear(TAlternateKey, out TKey)"/>
        public bool Clear(TAlternateKey key) => Clear(key, out _);

        /// <summary>
        /// Clears the values in the list associated with the specified alternate key and removes the key from the dictionary.
        /// </summary>
        public bool Clear(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey)
        {
            if (_altLookup.TryGetValue(key, out actualKey, out var valueList))
            {
                DebugValid(valueList);
                var list = valueList.LastList;

                _dictionary._version++;
                _dictionary._valueCount -= list.Count;

                list.Clear();
                _altLookup.Remove(key);

                _dictionary.DebugValueCount();
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="Contains(TAlternateKey, TValue, out TKey)"/>
        public bool Contains(TAlternateKey key, TValue value) => Contains(key, value, out _);

        /// <summary>
        /// Returns a value indicating whether the specified alternate key and associated value are present in the dictionary.
        /// </summary>
        public bool Contains(TAlternateKey key, TValue value, [MaybeNullWhen(false)] out TKey actualKey)
        {
            if (_altLookup.TryGetValue(key, out actualKey, out var valueList))
            {
                DebugValid(valueList);
                return valueList.LastList.Contains(value);
            }

            return false;
        }

        /// <inheritdoc cref="ContainsKey(TAlternateKey, out TKey)"/>/>
        public bool ContainsKey(TAlternateKey key) => _altLookup.ContainsKey(key);

        /// <summary>
        /// Returns a value indicating whether the dictionary contains the specified alternate key.
        /// </summary>
        public bool ContainsKey(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey) => _altLookup.TryGetValue(key, out actualKey, out _);

        /// <summary>
        /// Gets the number of values in the dictionary associated with the specified alternate key or zero if the key is not present.
        /// </summary>
        public int GetValueCount(TAlternateKey key) => _altLookup.TryGetValue(key, out var valueList) ? valueList.Count : 0;

        /// <inheritdoc cref="TryGetValues(TAlternateKey, out TKey, out ValueList)"/>
        public bool TryGetValues(TAlternateKey key, [MaybeNullWhen(false)] out ValueList valueList) => _altLookup.TryGetValue(key, out valueList);

        /// <summary>
        /// Gets the value list for the specified alternate key or <see langword="null"/> if the key was not found.
        /// </summary>
        /// <returns>A value indicating whether the key was found.</returns>
        public bool TryGetValues(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey, [MaybeNullWhen(false)] out ValueList valueList) =>
            _altLookup.TryGetValue(key, out actualKey, out valueList);
    }
}

#endif