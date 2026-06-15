using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Singulink.Collections.Utilities;
using Singulink.Collections.WeakCollectionHelpers;

namespace Singulink.Collections;

#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable CS0436 // Type conflicts with imported type

/// <summary>
/// Represents a collection of keys and weakly referenced values. If this collection is accessed concurrently from multiple threads (even in a read-only manner)
/// then all accesses must be synchronized with a full lock.
/// </summary>
public partial class WeakValueDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
    where TValue : class
{
    // The actual dictionary that we use
    private ConcurrentDictionary<TKey, Node>? _lookup;

    // These are the values we need for our weak tracking support.
    private ContainerValues<TValue, Node, WeakValueDictionary<TKey, TValue>, Node.NodeHelpers> _containerValues;

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
        _lookup = new(comparer);
        _containerValues = new();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="WeakValueDictionary{TKey, TValue}"/> class.
    /// </summary>
    ~WeakValueDictionary()
    {
        // We want to block usage after potential resurrection (as it could be dangerous), as it could be actively problematic, so mark as disposed now:
        // NOTE: we do not expose a dispose method directly, as it would be unsafe (since we do not perform suitable locking around stuff).
        // However, it is unsafe to use the collection after resurrection, so we still need to track it based on that, so we still track disposal internally.
        _lookup = null;
        Thread.MemoryBarrier();
    }

    // Helper to assert not disposed in Debug mode (doesn't check in Release mode, but still gives nullable analysis info):
    [MemberNotNull(nameof(_lookup))]
    private void ThrowIfDisposed()
    {
        Throw.IfDisposed(_lookup == null, typeof(WeakValueDictionary<TKey, TValue>));
    }

    /// <summary>
    /// Gets the equality comparer used to compare keys in the dictionary.
    /// </summary>
    public IEqualityComparer<TKey> Comparer
    {
        get
        {
            ThrowIfDisposed();
            var result = _lookup.Comparer;
            GC.KeepAlive(this);
            return result;
        }
    }

    /// <summary>
    /// Gets the keys in the dictionary.
    /// </summary>
    public IEnumerable<TKey> Keys
    {
        get
        {
            ThrowIfDisposed();
            return this.Select((x) => x.Key);
        }
    }

    /// <summary>
    /// Gets the values in the dictionary.
    /// </summary>
    public IEnumerable<TValue> Values
    {
        get
        {
            ThrowIfDisposed();
            return this.Select((x) => x.Value);
        }
    }

    /// <summary>
    /// Gets the number of entries in the internal data structure. This value can change at any time, and additionally may be overcounting the real amount of
    /// live entries, since it does not exclude entries whose values have been collected where the entry has not yet been collected.
    /// </summary>
    public int UnsafeCount
    {
        get
        {
            ThrowIfDisposed();
            return _lookup.Count;
        }
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    public TValue this[TKey key]
    {
        get
        {
            if (!TryGetValue(key, out var value))
                Throw.KeyNotFound();

            return value;
        }
        set
        {
            ThrowIfDisposed();
            try
            {
                // Add or update the value for this key.
                var newNode = AllocNode(key, value);
                Node? previousNode = null;
                _lookup.AddOrUpdate(key, newNode, (_, oldNode) =>
                {
                    // If we had a previous node (from this method being called more than once), we can dispose it (rather than forcing finalizer thread to).
                    // Nodes are never re-used, so we know that if it is no longer the current node we're replacing, then it is out of the dictionary and safe
                    // to dispose (they are safe for multiple disposal across multiple threads).
                    previousNode?.Dispose();

                    // Update our previous state:
                    previousNode = oldNode;

                    // Return the value we want to use:
                    return newNode;
                });

                // Dispose the previous one:
                previousNode?.Dispose();
            }
            catch
            {
                HandleFailure();
                throw;
            }
            finally
            {
                GC.KeepAlive(value);
                GC.KeepAlive(this);
            }
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
        ThrowIfDisposed();
        try
        {
            if (_lookup.TryGetValue(key, out var entry))
            {
                if (entry.Value.TryGetTarget(out var valueTmp))
                {
                    // Note: we do GC.KeepAlive on a temporary since 'value' could be overwritten before we could actually call that.
                    value = valueTmp;
                    GC.KeepAlive(valueTmp);
                    return true;
                }
                else
                {
                    // We may as well dispose early if possible, since we're clearly done with it (the value has died).
                    entry.Dispose();
                }
            }

            value = null;
            return false;
        }
        catch
        {
            HandleFailure();
            throw;
        }
        finally
        {
            GC.KeepAlive(this);
        }
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    public bool TryAdd(TKey key, TValue value)
    {
        ThrowIfDisposed();
        try
        {
            // Try TryAdd first
            var node = AllocNode(key, value);
            Node? toDispose = null;
            if (_lookup.TryAdd(key, node))
            {
                return true;
            }

            // Try replacing an entry if it's not representing an alive value
            // Note: it is important that our lambda is able to handle multiple calls.
            else if (_lookup.AddOrUpdate(key, node, (_, old) =>
            {
                // If we had a previous node (from this method being called more than once), we can dispose it (rather than forcing finalizer thread to).
                // Nodes are never re-used, so we know that if it is no longer the current node we're replacing, then it is out of the dictionary and safe
                // to dispose (they are safe for multiple disposal across multiple threads).
                toDispose?.Dispose();

                // Check if it is representing an alive value.
                if (old.Value.TryGetTarget(out var valueTmp))
                {
                    GC.KeepAlive(valueTmp);
                    toDispose = null; // This is necessary, as the callback may be called more than once.
                    return old;
                }

                // Otherwise, the value is dead, so we replace it.
                else
                {
                    toDispose = old;
                    return node;
                }
            }) == node)
            {
                toDispose?.Dispose();
                return true;
            }

            // Otherwise, we failed to add it.
            else
            {
                // Dispose now, since we failed to add it.
                node.Dispose();
                return false;
            }
        }
        catch
        {
            HandleFailure();
            throw;
        }
        finally
        {
            GC.KeepAlive(value);
            GC.KeepAlive(this);
        }
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <exception cref="ArgumentException">The specified key already exists in the dictionary.</exception>
    public void Add(TKey key, TValue value)
    {
        if (!TryAdd(key, value))
            Throw.Arg("Specified key already exists.", nameof(key));
    }

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// </summary>
    /// <returns><see langword="true"/> if the item was found and removed, otherwise <see langword="false"/>.</returns>
    public bool Remove(TKey key)
    {
        return Remove(key, out _);
    }

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    /// <param name="value">The value that was removed, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the item was found and removed, otherwise <see langword="false"/>.</returns>
    public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ThrowIfDisposed();
        try
        {
            if (_lookup.TryRemove(key, out var node))
            {
                bool result = false;
                if (node.Value.TryGetTarget(out var valueTmp))
                {
                    // Note: we do GC.KeepAlive on a temporary since 'value' could be overwritten before we could actually call that.
                    value = valueTmp;
                    GC.KeepAlive(valueTmp);
                    result = true;
                }
                else
                {
                    value = null;
                }

                // Dispose the node now that it has been removed from the dictionary, so its weak tracking state is cleaned up immediately rather than lingering
                // until the value is eventually collected (or, for an already-dead value, finalized).
                node.Dispose();
                return result;
            }

            value = null;
            return false;
        }
        catch
        {
            HandleFailure();
            throw;
        }
        finally
        {
            GC.KeepAlive(this);
        }
    }

    /// <summary>
    /// Removes the entry with the given key and value from the dictionary using the specified equality comparer for the value type.
    /// </summary>
    public bool Remove(TKey key, TValue value, IEqualityComparer<TValue>? comparer = null)
    {
        ThrowIfDisposed();
        comparer ??= EqualityComparer<TValue>.Default;
        try
        {
            while (true)
            {
                if (_lookup.TryGetValue(key, out var node))
                {
                    if (node.Value.TryGetTarget(out var valueTmp))
                    {
                        // Check if they are equal.
                        bool removed = false;
                        if (comparer.Equals(value, valueTmp))
                        {
                            // Try to remove this key & value pair. If we fail to remove it, then we need to try again, since it could be the case that there's
                            // a new value this should either succeed or fail for (it is indeterminate).
                            if (_lookup.TryRemove(new KeyValuePair<TKey, Node>(key, node)))
                            {
                                node.Dispose();
                                removed = true;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        // Note: we do GC.KeepAlive on a temporary since 'value' could be overwritten before we could actually call that.
                        GC.KeepAlive(valueTmp);
                        return removed;
                    }
                    else
                    {
                        // We may as well dispose early if possible, since we're clearly done with it (the value has died).
                        node.Dispose();
                    }
                }

                return false;
            }
        }
        catch
        {
            HandleFailure();
            throw;
        }
        finally
        {
            GC.KeepAlive(this);
        }
    }

    /// <summary>
    /// Indicates whether the dictionary contains the specified key/value pair using the optionally specified value comparer.
    /// </summary>
    public bool Contains(KeyValuePair<TKey, TValue> kvp, IEqualityComparer<TValue>? comparer = null) => Contains(kvp.Key, kvp.Value, comparer);

    /// <summary>
    /// Indicates whether the dictionary contains the key and value using the optionally specified value comparer.
    /// </summary>
    public bool Contains(TKey key, TValue value, IEqualityComparer<TValue>? comparer = null)
    {
        bool result = TryGetValue(key, out var current) && (comparer ?? EqualityComparer<TValue>.Default).Equals(value, current);
        GC.KeepAlive(current); // Ensure the value can't get collected until we're ready to return, as we may be returning true.
        return result;
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
    /// <remarks>
    /// This operation is not atomic, each value is removed one at a time in a way that is not special.
    /// </remarks>
    public void Clear()
    {
        // Note: we attempt to dispose the entries here also
        ThrowIfDisposed();
        try
        {
            foreach (var kvp in _lookup) kvp.Value.Dispose();
        }
        finally
        {
            GC.KeepAlive(this);
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the key/value pairs in the dictionary.
    /// </summary>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        ThrowIfDisposed();
        foreach (var kvp in _lookup)
        {
            ThrowIfDisposed();
            if (kvp.Value.Value.TryGetTarget(out var value))
            {
                GC.KeepAlive(this);
                yield return new KeyValuePair<TKey, TValue>(kvp.Key, value);
            }
            else
            {
                kvp.Value.Dispose();
            }
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the key/value pairs in the dictionary.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
