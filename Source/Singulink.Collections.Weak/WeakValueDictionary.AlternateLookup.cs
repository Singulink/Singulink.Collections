#if NET9_0_OR_GREATER

using System.Collections.Concurrent;
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
        ThrowIfDisposed();
        return new AlternateLookup<TAlternateKey>(this, _lookup.GetAlternateLookup<TAlternateKey>());
    }

    /// <inheritdoc cref="GetAlternateLookup{TAlternateKey}"/>
    public bool TryGetAlternateLookup<TAlternateKey>(
        [MaybeNullWhen(false)] out AlternateLookup<TAlternateKey> lookup)
        where TAlternateKey : notnull, allows ref struct
    {
        ThrowIfDisposed();

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
        private readonly ConcurrentDictionary<TKey, Node>.AlternateLookup<TAlternateKey> _altLookup;

        internal AlternateLookup(WeakValueDictionary<TKey, TValue> dictionary, ConcurrentDictionary<TKey, Node>.AlternateLookup<TAlternateKey> altLookup)
        {
            _dictionary = dictionary;
            _altLookup = altLookup;
        }

        /// <summary>
        /// Gets the value associated with the specified alternate key.
        /// </summary>
        public TValue this[TAlternateKey key]
        {
            get
            {
                if (!TryGetValue(key, out var value))
                    Throw.KeyNotFound();

                return value;
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
        public bool ContainsKey(TAlternateKey key)
        {
            _dictionary.ThrowIfDisposed();
            return TryGetValue(key, out _);
        }

        /// <summary>
        /// Returns a value indicating whether the dictionary contains the specified alternate key.
        /// </summary>
        public bool ContainsKey(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey)
        {
            _dictionary.ThrowIfDisposed();
            return TryGetValue(key, out actualKey, out _);
        }

        /// <inheritdoc cref="Remove(TAlternateKey, out TKey, out TValue)"/>
        public bool Remove(TAlternateKey key)
        {
            _dictionary.ThrowIfDisposed();
            return Remove(key, out _, out _);
        }

        /// <summary>
        /// Removes the value with the specified alternate key from the dictionary.
        /// </summary>
        public bool Remove(TAlternateKey key, [MaybeNullWhen(false)] out TKey actualKey, [MaybeNullWhen(false)] out TValue value)
        {
            _dictionary.ThrowIfDisposed();
            try
            {
                while (true)
                {
                    if (_altLookup.TryGetValue(key, out var actualKeyTmp, out var node))
                    {
                        if (node.Value.TryGetTarget(out var valueTmp))
                        {
                            bool removed = false;

                            // Try to remove this key & value pair. If we fail to remove it, then we need to try again, since it could be the case that there's a
                            // new value this should succeed for.
                            if (_dictionary._lookup.TryRemove(new KeyValuePair<TKey, Node>(actualKeyTmp, node)))
                            {
                                node.Dispose();
                                removed = true;
                            }
                            else
                            {
                                continue;
                            }

                            // Note: we do GC.KeepAlive on a temporary since 'value' could be overwritten before we could actually call that.
                            GC.KeepAlive(valueTmp);

                            if (removed)
                            {
                                actualKey = actualKeyTmp;
                                value = valueTmp;
                            }
                            else
                            {
                                actualKey = default;
                                value = default;
                            }

                            return removed;
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
            _dictionary.ThrowIfDisposed();

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