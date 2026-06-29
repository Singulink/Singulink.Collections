#if !NET
using System.Runtime.CompilerServices;

#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable SA1401 // Fields should be private

namespace Singulink.Collections.WeakCollectionHelpers;

/// <summary>
/// Wrapper around a <see cref="ConditionalWeakTable{TKey, TValue}"/> that allows storing multiple values per key, without the complexity drawback that repeated
/// removals from a CWT (some implementations) can cause. Note: we trade of having to lock for O(n) time (potentially blocking the finalizer thread) for having
/// good real-world performance, ideally we wouldn't have to do this.
/// </summary>
internal sealed class ConditionalWeakTableListWrapper<TKey, TValue>
    where TKey : class
    where TValue : class
{
    private readonly Lock _lock = new();
    private readonly LinkedList<(WeakReference<TKey> Key, WeakReference<EntryList> EntryList, bool IsEmpty)> _entries = new();
    private ConditionalWeakTable<TKey, EntryList> _table = new();
    private int _freeSlotCount;
    private int _keyCount;
    private int _opsSinceLastShrink;

    /// <summary>
    /// An entry in the conditional weak table list. Callers should not access any members on this type themselves.
    /// </summary>
    internal sealed class Entry(TValue value)
    {
        internal TValue Value = value;
        internal LinkedListNode<Entry>? Node;
    }

    private sealed class EntryList(LinkedList<Entry> list)
    {
        public LinkedList<Entry> List = list;
        public LinkedListNode<(WeakReference<TKey> Key, WeakReference<EntryList> EntryList, bool IsEmpty)>? Node;
    }

    /// <summary>
    /// Adds the given value to the list of values for the given key, and returns an <see cref="Entry"/> (opaque) that can be used to remove it later.
    /// </summary>
    public Entry Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            return AddNoLock(key, value);
        }
    }

    /// <summary>
    /// Adds the given value to the list of values for the given key, and returns an <see cref="Entry"/> (opaque) that can be used to remove it later.
    /// </summary>
    /// <remarks>
    /// This variant requires the caller to have external synchronization.
    /// </remarks>
    public Entry AddNoLock(TKey key, TValue value)
    {
        if (_table.TryGetValue(key, out var entryList))
        {
            if (entryList.List.Count == 0)
            {
                _freeSlotCount--;
                entryList.Node!.Value = entryList.Node.Value with { IsEmpty = false };
            }
        }
        else
        {
            entryList = new(new());
            _keyCount++;

            _table.Add(key, entryList);
            entryList.Node = _entries.AddLast((new(key), new(entryList), false));
        }

        var entry = new Entry(value);
        entry.Node = entryList.List.AddLast(entry);

        ShrinkIfNeededNoLock();

        GC.KeepAlive(key);
        return entry;
    }

    /// <summary>
    /// Removes the given entry for the given key, if it is still present.
    /// </summary>
    public void TryRemove(TKey key, Entry entry)
    {
        lock (_lock)
        {
            TryRemoveNoLock(key, entry);
        }
    }

    /// <summary>
    /// Removes the given entry for the given key, if it is still present.
    /// </summary>
    /// <remarks>
    /// This variant requires the caller to have external synchronization.
    /// </remarks>
    public void TryRemoveNoLock(TKey key, Entry entry)
    {
        if (entry.Node != null && _table.TryGetValue(key, out var entryList) && entryList.List == entry.Node.List)
        {
            entryList.List.Remove(entry.Node);
            entry.Node = null;

            if (entryList.List.Count == 0)
            {
                _freeSlotCount++;
                entryList.Node!.Value = entryList.Node.Value with { IsEmpty = true };
            }
        }

        ShrinkIfNeededNoLock();
    }

    /// <summary>
    /// Determines whether there are any entries for the given key.
    /// </summary>
    public bool HasAny(TKey key)
    {
        lock (_lock)
        {
            return HasAnyNoLock(key);
        }
    }

    /// <summary>
    /// Determines whether there are any entries for the given key.
    /// </summary>
    /// <remarks>
    /// This variant requires the caller to have external synchronization.
    /// </remarks>
    public bool HasAnyNoLock(TKey key)
    {
        return _table.TryGetValue(key, out var entryList) && entryList.List.Count > 0;
    }

    /// <summary>
    /// Clears the table of all entries.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            ClearNoLock();
        }
    }

    /// <summary>
    /// Clears the table of all entries.
    /// </summary>
    /// <remarks>
    /// This variant requires the caller to have external synchronization.
    /// </remarks>
    public void ClearNoLock()
    {
#if NETSTANDARD2_1_OR_GREATER
        // Note: Clear replaces the internal data structures if no enumeration is happening (and we don't do that), hence why we don't have to new one up also
        // as it avoids the issue with Remove.
        _table.Clear();
#else
        _table = new();
#endif
        _entries.Clear();
        _freeSlotCount = 0;
        _keyCount = 0;
        _opsSinceLastShrink = 0;
    }

    /// <summary>
    /// See <see cref="ShrinkIfNeededNoLock"/>, and other APIs for the distinction between locking and non-locking variants.
    /// </summary>
    public void ShrinkIfNeeded()
    {
        lock (_lock)
        {
            ShrinkIfNeededNoLock();
        }
    }

    /// <summary>
    /// This is a helper API to ensure that we both removed lists we no longer need and to work around the complexity issue of
    /// <see cref="ConditionalWeakTable{TKey, TValue}.Remove(TKey)"/>. In particular, on some runtimes, the <c>Remove</c> method doesn't attempt to reclaim
    /// memory until an arbitrary point in the future (in particular, when it resizes, which we can't force). However, we do want to remove our entries at some
    /// point and not keep an unbounded quantity of them alive unnecessarily, so this method is the core helper to achieve that, by allowing unused values to
    /// stay alive until we have too many, and then removing them and potentially re-allocating the table in such a way to achieve our overall amortized
    /// complexity goals.
    /// </summary>
    /// <remarks>
    /// In particular, this method is meant to ensure that all operations take amortized O(1) time and we are using strictly O(n) memory. Note: this requires
    /// that the caller must call the removal method or this method even if the entry is already dead, to gain the strict memory requirement.
    /// </remarks>
    public void ShrinkIfNeededNoLock()
    {
        // Only consider clearing out if at least half of the slots are free and we have at least 32 free slots in total, or have done too many operations.
        // This limits the amount of times we have to do this routine.
        _opsSinceLastShrink++;

        if ((_freeSlotCount > _keyCount / 2 && _freeSlotCount > 32) || (_opsSinceLastShrink > _keyCount / 2 && _opsSinceLastShrink > 32))
        {
            // Remove all unnecessary (due to key dying) free slots according to the _entries list:
            _opsSinceLastShrink = 0;
            var node = _entries.First;
            int removedCount = 0;
            while (node != null)
            {
                var next = node.Next;

                if (!node.Value.Key.TryGetTarget(out _))
                {
                    // NOTE: value must be alive if Key was alive, and can be presumed dead if key is dead.
                    _entries.Remove(node);
                    _keyCount--;

                    if (node.Value.IsEmpty)
                    {
                        _freeSlotCount--;
                        removedCount++;
                    }
                }

                node = next;
            }

            // If we removed less than half of the free slots, then we need to rebuild the table to maintain good amortized complexity:
            if (removedCount < _freeSlotCount)
            {
                var newTable = new ConditionalWeakTable<TKey, EntryList>();

                node = _entries.First;
                while (node != null)
                {
                    var next = node.Next;

                    if (node.Value.Key.TryGetTarget(out var key) && node.Value.EntryList.TryGetTarget(out var entryList) && entryList.List.Count > 0)
                    {
                        newTable.Add(key, entryList);
                    }
                    else
                    {
                        _entries.Remove(node);
                        _keyCount--;
                    }

                    node = next;
                }

                _freeSlotCount = 0;
                _table = newTable;
            }
        }
    }
}
#endif
