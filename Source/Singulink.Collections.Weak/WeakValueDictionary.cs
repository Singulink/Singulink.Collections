using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.Collections.WeakCollectionHelpers;

namespace Singulink.Collections;

#pragma warning disable CS0436 // Type conflicts with imported type
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable IDE0028 // Simplify collection initialization

/// <summary>
/// Represents a collection of keys and weakly referenced values that automatically removes entries as their values are garbage collected.
/// </summary>
/// <remarks>
/// <para>Values are held through weak references, so adding a value to the dictionary does not prevent it from being garbage collected. When a value is
/// collected its entry is removed from the dictionary automatically on the finalizer thread shortly afterwards, so the dictionary never needs to be cleaned
/// and lookups and enumeration never return <see langword="null"/> values.</para>
/// <para>Individual operations are safe for concurrent use from multiple threads without external synchronization, including while other threads are
/// enumerating the dictionary. Compound operations that must observe a consistent view of the dictionary still require external synchronization.</para>
/// <para>Key comparers that throw from <see cref="IEqualityComparer{T}.Equals(T, T)"/> or <see cref="IEqualityComparer{T}.GetHashCode(T)"/> are not
/// supported and may leave the dictionary unusable after the exception.</para>
/// <para>When a value dies, its entry lingers until the finalizer thread removes it. If an equal key is added during that window the entry is replaced in
/// place and the dictionary keeps the key instance it already had, so <see cref="Keys"/> and enumeration return the original key instance rather than the
/// one passed to the later add. This matches the key retention behavior of <see cref="Dictionary{TKey, TValue}"/> and is only observable when equal keys
/// are distinguishable, such as by reference. Callers that require the new key instance to be stored can synchronize access and call
/// <see cref="Remove(TKey)"/> before adding.</para>
/// <para>Dispose dictionaries that are no longer needed instead of letting them be finalized. The finalizer removes all remaining entries, which can block
/// the finalizer thread for a significant amount of time if the dictionary is large.</para>
/// </remarks>
public sealed partial class WeakValueDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
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
    /// Initializes a new instance of the <see cref="WeakValueDictionary{TKey, TValue}"/> class that uses the specified key comparer, or the default comparer
    /// if <see langword="null"/> is specified.
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
    /// Gets the comparer that is used to determine equality of the keys.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
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
    /// Gets the keys in the dictionary whose values are still available.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed. The exception is thrown when the keys are enumerated.</exception>
    public IEnumerable<TKey> Keys => this.Select((x) => x.Key);

    /// <summary>
    /// Gets the values in the dictionary that are still available.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed. The exception is thrown when the values are enumerated.</exception>
    public IEnumerable<TValue> Values => this.Select((x) => x.Value);

    /// <summary>
    /// Gets the number of entries in the dictionary.
    /// </summary>
    /// <remarks>
    /// The count includes entries whose values have been garbage collected but have not yet been removed from the dictionary, so it may briefly exceed the
    /// number of entries that enumeration returns.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
    public int Count
    {
        get
        {
            ThrowIfDisposed(out var lookup);
            int result = lookup.Count;
            GC.KeepAlive(this);
            return result;
        }
    }

    /// <summary>
    /// Gets the number of entries in the dictionary. This member is obsolete, use <see cref="Count"/> instead.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
    [Obsolete($"Use {nameof(Count)} instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int UnsafeCount => Count;

    /// <summary>
    /// Gets or sets the value associated with the specified key. Setting a value replaces any existing value for the key.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The key does not exist in the dictionary, or its value has been garbage collected.</exception>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
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
    /// <param name="value">When this method returns, contains the value associated with the key if it was found and is still available; otherwise
    /// <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the key was found and its value is still available; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
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
    /// Attempts to add the specified key and value to the dictionary.
    /// </summary>
    /// <returns><see langword="true"/> if the key and value were added; <see langword="false"/> if the dictionary already contains the key with a value that is
    /// still available.</returns>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
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
    /// <exception cref="ArgumentException">The dictionary already contains the key with a value that is still available.</exception>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
    public void Add(TKey key, TValue value)
    {
        if (!TryAdd(key, value))
            Throw.Arg("Specified key already exists.", nameof(key));
    }

    /// <summary>
    /// Removes the entry with the specified key from the dictionary.
    /// </summary>
    /// <returns><see langword="true"/> if the entry was found and removed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
    public bool Remove(TKey key)
    {
        return Remove(key, out _);
    }

    /// <summary>
    /// Removes the entry with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <param name="value">When this method returns, contains the value that was removed if the entry was found and its value was still available; otherwise
    /// <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the entry was found and removed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
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
    /// Removes the entry with the specified key from the dictionary if its value is equal to the specified value.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <param name="value">The value that the entry must have in order to be removed.</param>
    /// <param name="comparer">The comparer to use to determine equality of the values, or <see langword="null"/> to use the default comparer.</param>
    /// <returns><see langword="true"/> if the entry was found with an equal value and removed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
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
    /// Determines whether the dictionary contains the specified key and value pair.
    /// </summary>
    /// <param name="kvp">The key and value pair to locate.</param>
    /// <param name="comparer">The comparer to use to determine equality of the values, or <see langword="null"/> to use the default comparer.</param>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
    public bool Contains(KeyValuePair<TKey, TValue> kvp, IEqualityComparer<TValue>? comparer = null) => Contains(kvp.Key, kvp.Value, comparer);

    /// <summary>
    /// Determines whether the dictionary contains the specified key with a value equal to the specified value.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">The value to locate.</param>
    /// <param name="comparer">The comparer to use to determine equality of the values, or <see langword="null"/> to use the default comparer.</param>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
    public bool Contains(TKey key, TValue value, IEqualityComparer<TValue>? comparer = null)
    {
        bool result = TryGetValue(key, out var current) && (comparer ?? EqualityComparer<TValue>.Default).Equals(value, current);
        GC.KeepAlive(current); // Ensure the value can't get collected until we're ready to return, as we may be returning true.
        return result;
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key with a value that is still available.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
    public bool ContainsKey(TKey key) => TryGetValue(key, out _);

    /// <summary>
    /// Determines whether the dictionary contains a value equal to the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <param name="comparer">The comparer to use to determine equality of the values, or <see langword="null"/> to use the default comparer.</param>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
    public bool ContainsValue(TValue value, IEqualityComparer<TValue>? comparer = null) => Values.Contains(value, comparer);

    /// <summary>
    /// Removes all entries from the dictionary.
    /// </summary>
    /// <remarks>
    /// Entries are removed one at a time. <see cref="Dispose"/> is faster if the dictionary will not be used again.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
    public void Clear()
    {
        // Note: we attempt to dispose the entries here also.
        ThrowIfDisposed(out var lookup);
        try
        {
            foreach (var kvp in lookup)
                kvp.Value.Dispose();
        }
        finally
        {
            GC.KeepAlive(this);
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the entries in the dictionary whose values are still available.
    /// </summary>
    /// <remarks>
    /// Enumeration is safe while other threads modify the dictionary. Entries added by other threads during enumeration may or may not be included.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The dictionary has been disposed.</exception>
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
    /// Removes all entries from the dictionary and prevents further use. Most members throw <see cref="ObjectDisposedException"/> afterwards.
    /// </summary>
    /// <remarks>
    /// Disposing is faster than <see cref="Clear"/> and should be preferred for dictionaries that will not be used again, since it also avoids the cost of
    /// finalizing the dictionary.
    /// </remarks>
    public void Dispose()
    {
        _containerValues.Dispose(this);
        _lookup = null;
    }
}
