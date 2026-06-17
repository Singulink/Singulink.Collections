using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.Collections.Utilities;
using Singulink.Collections.WeakCollectionHelpers;

namespace Singulink.Collections;

#pragma warning disable CS0436 // Type conflicts with imported type
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable IDE0028 // Simplify collection initialization

/// <summary>
/// Represents a collection of keys and weakly referenced values. This type is also automatically safe for concurrent access, and will automatically remove dead
/// objects from the collection.
/// </summary>
public partial class WeakValueDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
    where TKey : notnull
    where TValue : class
{
    // The actual dictionary that we use.
    // IMPORTANT: whenever we remove or overwrite an entry in here, we are responsible for calling Dispose() on the node we displaced. If we don't, the node
    // (and its weak-tracking memory) is leaked for as long as the value stays alive instead of being reclaimed when we remove it.
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
        _containerValues = new(this);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="WeakValueDictionary{TKey, TValue}"/> class.
    /// </summary>
    ~WeakValueDictionary()
    {
        // We want to block usage after potential resurrection (as it could be dangerous), as it could be actively problematic, so mark as disposed now:
        _disableAllocations = true;
        Thread.MemoryBarrier();
    }

    // Helper to throw if disposed and get the non-null lookup at the same time:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed(out ConcurrentDictionary<TKey, Node> lookup)
    {
        Throw.IfDisposed(_disableAllocations, typeof(WeakValueDictionary<TKey, TValue>));
        var result = _lookup;
        Throw.IfDisposed(result == null, typeof(WeakValueDictionary<TKey, TValue>));
        lookup = result;
    }

    /// <summary>
    /// Gets the equality comparer used to compare keys in the dictionary.
    /// </summary>
    public IEqualityComparer<TKey> Comparer
    {
        get
        {
            ThrowIfDisposed(out var lookup);
            var result = lookup.Comparer;
            GC.KeepAlive(this);
            return result;
        }
    }

    /// <summary>
    /// Gets the keys in the dictionary.
    /// </summary>
    public IEnumerable<TKey> Keys => this.Select((x) => x.Key);

    /// <summary>
    /// Gets the values in the dictionary.
    /// </summary>
    public IEnumerable<TValue> Values => this.Select((x) => x.Value);

    /// <summary>
    /// Gets the number of entries in the internal data structure. This value can change at any time, and additionally may be overcounting the real amount of
    /// live entries, since it does not exclude entries whose values have been collected where the entry has not yet been collected.
    /// </summary>
    public int UnsafeCount
    {
        get
        {
            ThrowIfDisposed(out var lookup);
            return lookup.Count;
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
            ThrowIfDisposed(out var lookup);
            bool nonFailureException = false;
            try
            {
                // Allocate a node for this.
                Node newNode;
                try
                {
                    newNode = AllocNode(key, value);
                }
                catch (ObjectDisposedException)
                {
                    nonFailureException = true;
                    throw;
                }

                // Add or update the value for this key.
                Node? previousNode = null;
                lookup.AddOrUpdate(key, newNode, (_, oldNode) =>
                {
                    // We must call Dispose() on the node we overwrite, otherwise it is leaked for as long as its value stays alive.
                    // If we had a previous node (from this method being called more than once), we can dispose it (rather than forcing finalizer thread to).
                    // Nodes are never re-used, so we know that if it is no longer the current node we're replacing, then it is out of the dictionary and safe
                    // to dispose (they are safe for multiple disposal across multiple threads).
                    previousNode?.Dispose();

                    // Update our previous state:
                    previousNode = oldNode;

                    // Return the value we want to use:
                    return newNode;
                });

                // Dispose the node we overwrote, otherwise it is leaked for as long as its value stays alive.
                previousNode?.Dispose();
            }
            catch when (!nonFailureException)
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
        ThrowIfDisposed(out var lookup);
        try
        {
            if (lookup.TryGetValue(key, out var entry))
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
                    // Note: this one is non-critical, as its value is already dead (and hence all the stuff will die eventually).
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
        ThrowIfDisposed(out var lookup);
        bool nonFailureException = false;
        try
        {
            // Allocate a node for this.
            Node node;
            try
            {
                node = AllocNode(key, value);
            }
            catch (ObjectDisposedException)
            {
                nonFailureException = true;
                throw;
            }

            // Try TryAdd first
            Node? toDispose = null;
            if (lookup.TryAdd(key, node))
            {
                return true;
            }

            // Try replacing an entry if it's not representing an alive value
            // Note: it is important that our lambda is able to handle multiple calls.
            else if (lookup.AddOrUpdate(key, node, (_, old) =>
            {
                // We must call Dispose() on any node we displace below, otherwise it is leaked for as long as its value stays alive.
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
                // Dispose the node we displaced, otherwise it is leaked for as long as its value stays alive.
                toDispose?.Dispose();
                return true;
            }

            // Otherwise, we failed to add it.
            else
            {
                // We never put this node into the dictionary, so dispose it now, otherwise it is leaked for as long as the value stays alive.
                node.Dispose();
                return false;
            }
        }
        catch when (!nonFailureException)
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
        ThrowIfDisposed(out var lookup);
        try
        {
            if (lookup.TryRemove(key, out var node))
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

                // We removed the node from the dictionary, so we must call Dispose() on it, otherwise it is leaked for as long as its value stays alive.
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
        ThrowIfDisposed(out var lookup);
        comparer ??= EqualityComparer<TValue>.Default;
        bool nonFailureException = false;
        try
        {
            while (true)
            {
                if (lookup.TryGetValue(key, out var node))
                {
                    if (node.Value.TryGetTarget(out var valueTmp))
                    {
                        // Check if they are equal.
                        bool removed = false;
                        bool isEqual;
                        try
                        {
                            isEqual = comparer.Equals(value, valueTmp);
                        }
                        catch
                        {
                            nonFailureException = true;
                            GC.KeepAlive(valueTmp);
                            throw;
                        }

                        // If equal:
                        if (isEqual)
                        {
                            // Try to remove this key & value pair. If we fail to remove it, then we need to try again, since it could be the case that there's
                            // a new value this should either succeed or fail for (it is indeterminate).
                            if (lookup.TryRemove(new KeyValuePair<TKey, Node>(key, node)))
                            {
                                // We removed the node, so we must call Dispose() on it, otherwise it is leaked for as long as its value stays alive.
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
                        // Note: this one is non-critical, as its value is already dead (and hence all the stuff will die eventually).
                        node.Dispose();
                    }
                }

                return false;
            }
        }
        catch when (!nonFailureException)
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
    /// This method removes all nodes one-by-one; using <see cref="Dispose" /> is faster if you do not need to reuse the dictionary instance.
    /// </remarks>
    public void Clear()
    {
        // Note: we attempt to dispose the entries here also.
        ThrowIfDisposed(out var lookup);
        try
        {
            foreach (var kvp in lookup) kvp.Value.Dispose();
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
        ThrowIfDisposed(out var lookup);
        foreach (var kvp in lookup)
        {
            ThrowIfDisposed(out _);
            if (kvp.Value.Value.TryGetTarget(out var value))
            {
                GC.KeepAlive(this);
                yield return new KeyValuePair<TKey, TValue>(kvp.Key, value);
            }
            else
            {
                // We may as well dispose early if possible, since we're clearly done with it (the value has died).
                // Note: this one is non-critical, as its value is already dead (and hence all the stuff will die eventually).
                kvp.Value.Dispose();
            }
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the key/value pairs in the dictionary.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Disposes the <see cref="WeakValueDictionary{TKey, TValue}" />, removing all entries and preventing further use.
    /// </summary>
    public void Dispose()
    {
        _containerValues.Dispose(this);
        _lookup = null;
    }
}
