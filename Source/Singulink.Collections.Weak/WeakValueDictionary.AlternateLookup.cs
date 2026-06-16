#if NET9_0_OR_GREATER

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.Collections.Utilities;

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
        ThrowIfDisposed(out var lookup);
        return new AlternateLookup<TAlternateKey>(this, lookup.GetAlternateLookup<TAlternateKey>());
    }

    /// <inheritdoc cref="GetAlternateLookup{TAlternateKey}"/>
    public bool TryGetAlternateLookup<TAlternateKey>(
        [MaybeNullWhen(false)] out AlternateLookup<TAlternateKey> lookup)
        where TAlternateKey : notnull, allows ref struct
    {
        ThrowIfDisposed(out var implLookup);

        if (implLookup.TryGetAlternateLookup<TAlternateKey>(out var altLookup))
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
        private readonly ConcurrentDictionary<TKey, Node>.AlternateLookup<TAlternateKey> _altLookup;

        internal AlternateLookup(WeakValueDictionary<TKey, TValue> dictionary, ConcurrentDictionary<TKey, Node>.AlternateLookup<TAlternateKey> altLookup)
        {
            _dictionary = dictionary;
            _altLookup = altLookup;
            Debug.Assert(_dictionary.Comparer is IAlternateEqualityComparer<TAlternateKey, TKey>, "The dictionary's comparer is of the wrong type.");
        }

        /// <summary>
        /// Gets or sets the value associated with the specified alternate key.
        /// </summary>
        public TValue this[TAlternateKey key]
        {
            get
            {
                if (!TryGetValue(key, out var value))
                    Throw.KeyNotFound();

                return value;
            }
            set
            {
                // Get a key either by looking up an existing one or by just allocating:
                _dictionary.ThrowIfDisposed(out _);
                var actualKey = _altLookup.TryGetValue(key, out var oldKey, out _) ? oldKey : Comparer.Create(key);

                // Just call into the non-alternate API with this key now:
                _dictionary[actualKey] = value;
            }
        }

        /// <summary>
        /// Adds the specified alternate key and value to the dictionary.
        /// </summary>
        public bool TryAdd(TAlternateKey key, TValue value)
        {
            // Try to find an existing key on a node that isn't meant to be alive any more:
            _dictionary.ThrowIfDisposed(out _);
            if (_altLookup.TryGetValue(key, out var actualKey, out var oldNode))
            {
                if (oldNode.Value.TryGetTarget(out var oldValue))
                {
                    // If we got a value, then we can't add this key.
                    // Otherwise, we can re-use the key value in our API call to _dictionary.TryAdd.
                    GC.KeepAlive(oldValue);
                    return false;
                }
            }
            else
            {
                // Otherwise initialize actualKey to a value we can call into the main API with:
                actualKey = Comparer.Create(key);
            }

            // Just call into the non-alternate API with this key now:
            return _dictionary.TryAdd(actualKey, value);
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
        public bool ContainsKey(TAlternateKey key) => TryGetValue(key, out _);

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
            _dictionary.ThrowIfDisposed(out var lookup);
            try
            {
                while (true)
                {
                    if (_altLookup.TryGetValue(key, out var actualKeyTmp, out var node))
                    {
                        if (node.Value.TryGetTarget(out var valueTmp))
                        {
                            // Try to remove this key & value pair. If we fail to remove it, then we need to try again, since it could be the case that there's
                            // a new value this should succeed for.
                            if (lookup.TryRemove(new KeyValuePair<TKey, Node>(actualKeyTmp, node)))
                            {
                                node.Dispose();
                            }
                            else
                            {
                                GC.KeepAlive(valueTmp);
                                continue;
                            }

                            // Note: we do GC.KeepAlive on a temporary since 'value' could be overwritten before we could actually call that.
                            GC.KeepAlive(valueTmp);
                            actualKey = actualKeyTmp;
                            value = valueTmp;
                            return true;
                        }
                        else
                        {
                            // We may as well dispose early if possible, since we're clearly done with it (the value has died).
                            node.Dispose();
                        }
                    }

                    actualKey = default;
                    value = default;
                    return false;
                }
            }
            catch
            {
                _dictionary.HandleFailure();
                throw;
            }
            finally
            {
                GC.KeepAlive(_dictionary);
            }
        }

        /// <inheritdoc cref="TryGetValue(TAlternateKey, out TKey, out TValue)"/>
        public bool TryGetValue(TAlternateKey key, [MaybeNullWhen(false)] out TValue value) => TryGetValue(key, out _, out value);

        /// <summary>
        /// Gets the value associated with the specified alternate key.
        /// </summary>
        public bool TryGetValue(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            _dictionary.ThrowIfDisposed(out _);

            try
            {
                if (_altLookup.TryGetValue(key, out var actualKeyTmp, out var entry))
                {
                    if (entry.Value.TryGetTarget(out var valueTmp))
                    {
                        // Note: we do GC.KeepAlive on a temporary since 'value' could be overwritten before we could actually call that.
                        value = valueTmp;
                        GC.KeepAlive(valueTmp);
                        actualKey = actualKeyTmp;
                        return true;
                    }
                    else
                    {
                        // We may as well dispose early if possible, since we're clearly done with it (the value has died).
                        entry.Dispose();
                    }
                }

                value = default;
                actualKey = default;
                return false;
            }
            finally
            {
                GC.KeepAlive(_dictionary);
            }
        }
    }
}

#endif
