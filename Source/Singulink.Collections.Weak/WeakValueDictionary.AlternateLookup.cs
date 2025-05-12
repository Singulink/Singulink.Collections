#if NET9_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Singulink.Collections;

/// <content>
/// Contains the AlternateLookup implementation for WeakValueDictionary.
/// </content>
partial class WeakValueDictionary<TKey, TValue>
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
    /// Provides an alternate lookup that can be used to perform operations on a <see cref="WeakValueDictionary{TKey, TValue}"/> using <typeparamref
    /// name="TAlternateKey"/> instead of <typeparamref name="TKey"/>.
    /// </summary>
    public readonly struct AlternateLookup<TAlternateKey> where TAlternateKey : notnull, allows ref struct
    {
        private readonly WeakValueDictionary<TKey, TValue> _dictionary;
        private readonly Dictionary<TKey, WeakReference<TValue>>.AlternateLookup<TAlternateKey> _altLookup;

        internal AlternateLookup(WeakValueDictionary<TKey, TValue> dictionary, Dictionary<TKey, WeakReference<TValue>>.AlternateLookup<TAlternateKey> altLookup)
        {
            _dictionary = dictionary;
            _altLookup = altLookup;
        }

        /// <summary>
        /// Gets the value associated wit the specified alternate key.
        /// </summary>
        public TValue? this[TAlternateKey key]
        {
            get {
                if (_altLookup.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var value))
                    return value;

                return null;
            }
        }

        /// <summary>
        /// Gets the equality comparer used to compare keys in the alternate lookup.
        /// </summary>
        public IAlternateEqualityComparer<TAlternateKey, TKey> Comparer => Unsafe.As<IAlternateEqualityComparer<TAlternateKey, TKey>>(_dictionary.Comparer);

        /// <summary>
        /// Gets the underlying dictionary associated with this alternate lookup.
        /// </summary>
        public WeakValueDictionary<TKey, TValue> Dictionary => _dictionary;

        /// <inheritdoc cref="ContainsKey(TAlternateKey, out TKey)"/>/>
        public bool ContainsKey(TAlternateKey key) => _altLookup.ContainsKey(key);

        /// <summary>
        /// Returns a value indicating whether the dictionary contains the specified alternate key.
        /// </summary>
        public bool ContainsKey(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey) => TryGetValue(key, out actualKey, out _);

        /// <inheritdoc cref="Remove(TAlternateKey, out TKey, out TValue)"/>
        public bool Remove(TAlternateKey key) => Remove(key, out _, out _);

        /// <summary>
        /// Removes the value with the specified alternate key from the dictionary.
        /// </summary>
        public bool Remove(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            if (_altLookup.TryGetValue(key, out actualKey, out var weakRef) && weakRef.TryGetTarget(out value))
            {
                _dictionary.Remove(actualKey);
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc cref="TryGetValue(TAlternateKey, out TKey, out TValue)"/>
        public bool TryGetValue(TAlternateKey key, [MaybeNullWhen(false)] out TValue value) => TryGetValue(key, out _, out value);

        /// <summary>
        /// Gets the value associated with the specified alternate key.
        /// </summary>
        public bool TryGetValue(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            if (_altLookup.TryGetValue(key, out actualKey, out var weakRef) && weakRef.TryGetTarget(out value))
                return true;

            value = default;
            return false;
        }
    }
}

#endif