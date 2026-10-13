using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Singulink.Collections.WeakCollectionHelpers;

namespace Singulink.Collections;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

/// <summary>
/// Represents a collection of weakly referenced values that maintains relative insertion order and automatically removes values as they are garbage
/// collected.
/// </summary>
/// <typeparam name="T">The type of the values in the list.</typeparam>
/// <remarks>
/// <para>Values are held through weak references, so adding a value to the list does not prevent it from being garbage collected. When a value is collected
/// its node is removed from the list automatically on the finalizer thread shortly afterwards, so the list never needs to be cleaned and enumeration never
/// returns <see langword="null"/> values.</para>
/// <para>Individual operations are safe for concurrent use from multiple threads without external synchronization, including while other threads are
/// enumerating the list. Compound operations that must observe a consistent view of the list still require synchronization, either with
/// <see cref="UnsafePerformLockedOperation{TState}(TState, Action{WeakList{T}, TState})"/> or with your own lock. Heavily contended lists may experience
/// reduced performance due to lock contention.</para>
/// <para>Operation times stated in this documentation assume that no nodes are added by other threads while the operation runs. Operations that enumerate
/// the list visit nodes that other threads add after the current position, so concurrent additions can lengthen them.</para>
/// <para>Dispose lists that are no longer needed instead of letting them be finalized. The finalizer removes all remaining nodes, which can block the
/// finalizer thread for a significant amount of time if the list is large. A list must not be used after it has been finalized.</para>
/// </remarks>
public sealed partial class WeakList<T> : IEnumerable<T>, IDisposable where T : class
{
    // These are the values we need for our weak tracking support.
    private ContainerValues<T, Node, WeakList<T>, Node.NodeHelpers> _containerValues;

    // The locker for the list.
    private readonly Lock _locker;

    // The head and tail nodes of the linked list:
    // Note: when not disposed, we always have at least one node (the pseudo-node), which is always ordered first.
    // When disposed, this is set to null.
    private Node? _head;
    private Node? _tail;

    // The current size of the list - note: we store as nint to make the size update operations faster (no overflow check needed).
    // It can be safely read with or without the lock held, but updates must be done with the lock held, and holding the lock is necessary to get an up-to-date
    // value.
    private nint _size;

    // This allows us to track whether an item was added before or after an enumeration:
    private ulong _version;

    // Helper to assert not disposed in Debug mode (doesn't check in Release mode, but still gives nullable analysis info):
    [MemberNotNull(nameof(_head))]
    [MemberNotNull(nameof(_tail))]
    partial void DebugAssertNotDisposed();
#if DEBUG
    partial void DebugAssertNotDisposed()
    {
        Debug.Assert(_head is not null, "Object is disposed.");
        Debug.Assert(_tail is not null, "_tail should not be null since not disposed.");
    }
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakList{T}"/> class.
    /// </summary>
    public WeakList()
    {
        _head = new(null, this) { _isPseudoNode = true };
        _tail = _head;
        _containerValues = new(this);
        _locker = new();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="WeakList{T}"/> class.
    /// </summary>
    ~WeakList()
    {
        // We want to block usage after potential resurrection (as it could be dangerous), as it could be actively problematic, so mark as disposed now:
        _head = null;
        _tail = null;
        Thread.MemoryBarrier();
    }

    /// <summary>
    /// Gets the number of nodes in the list.
    /// </summary>
    /// <remarks>
    /// The count includes nodes whose values have been garbage collected but have not yet been removed from the list, so it may briefly exceed the number of
    /// values that enumeration returns.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public nint Count
    {
        get
        {
            // Note: we need an memory barrier & volatile read here, since otherwise the read might not be sequentially consistent.
            Thread.MemoryBarrier();
            nint size = Volatile.Read(ref _size);
            Throw.IfDisposed(_head == null, typeof(WeakList<T>));
            GC.KeepAlive(this);
            return size;
        }
    }

    /// <summary>
    /// Gets the number of nodes in the list. This member is obsolete, use <see cref="Count"/> instead.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    /// <exception cref="OverflowException">The list contains more nodes than <see cref="int.MaxValue"/>.</exception>
    [Obsolete($"Use {nameof(Count)} instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int UnsafeCount => checked((int)Count);

    /// <summary>
    /// Gets the current version of the list. The version increases every time a node is added, so it can be compared to <see cref="Node.Version"/> to
    /// determine whether a node was added before or after a point in time.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public ListVersion Version
    {
        get
        {
            // Note: we need an memory barrier & volatile read here, since otherwise the read might not be sequentially consistent.
            Thread.MemoryBarrier();
            ulong version = Volatile.Read(ref _version);
            Throw.IfDisposed(_head == null, typeof(WeakList<T>));
            GC.KeepAlive(this);
            return new ListVersion(version);
        }
    }

    /// <summary>
    /// Adds a value to the start of the list.
    /// </summary>
    /// <returns>The node that was added.</returns>
    /// <remarks>
    /// This operation takes O(1) time.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node AddFirst(T value)
    {
        using var scope = EnterLock(out bool wasDisposed);
        Throw.IfDisposed(wasDisposed, typeof(WeakList<T>));
        DebugAssertNotDisposed();
        return InsertNearHelper(value, _head, addBefore: false); // Note: we add after, since the first one is the pseudo-node.
    }

    /// <summary>
    /// Adds a value to the end of the list.
    /// </summary>
    /// <returns>The node that was added.</returns>
    /// <remarks>
    /// This operation takes O(1) time.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node AddLast(T value)
    {
        using var scope = EnterLock(out bool wasDisposed);
        Throw.IfDisposed(wasDisposed, typeof(WeakList<T>));
        DebugAssertNotDisposed();
        return InsertNearHelper(value, _tail, addBefore: false);
    }

    /// <summary>
    /// Adds a value before the specified node.
    /// </summary>
    /// <param name="currentNode">The node to add the value before.</param>
    /// <param name="value">The value to add.</param>
    /// <param name="allowBeforeRemovedNode"><see langword="true"/> to add the value near the position that <paramref name="currentNode"/> occupied if it has been
    /// removed from the list, or <see langword="false"/> to return <see langword="null"/> in that case.</param>
    /// <returns>The node that was added, or <see langword="null"/> if <paramref name="currentNode"/> has been removed from the list and
    /// <paramref name="allowBeforeRemovedNode"/> is <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This operation takes O(1) time unless <paramref name="currentNode"/> has been removed from the list, in which case the list is searched backwards
    /// from its former position for the nearest node that is still present and the value is added after that node.</para>
    /// <para>Values added relative to a node that has been removed may end up in a different relative order than they would have had if the node were still
    /// in the list.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="currentNode"/> does not belong to this list.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node? AddBefore(Node currentNode, T value, bool allowBeforeRemovedNode) => AddNear(currentNode, value, allowBeforeRemovedNode, addBefore: true);

    /// <summary>
    /// Adds a value before the specified node, or near the position the node occupied if it has been removed from the list.
    /// </summary>
    /// <param name="currentNode">The node to add the value before.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>The node that was added.</returns>
    /// <remarks>
    /// <inheritdoc cref="AddBefore(Node, T, bool)" path="/remarks/*"/>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="currentNode"/> does not belong to this list.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node AddBefore(Node currentNode, T value)
    {
        var result = AddBefore(currentNode, value, true);
        Debug.Assert(result is { }, "Result should not be null when allowing adding before removed node.");
        return result;
    }

    /// <summary>
    /// Adds a value after the specified node.
    /// </summary>
    /// <param name="currentNode">The node to add the value after.</param>
    /// <param name="value">The value to add.</param>
    /// <param name="allowAfterRemovedNode"><see langword="true"/> to add the value near the position that <paramref name="currentNode"/> occupied if it has been
    /// removed from the list, or <see langword="false"/> to return <see langword="null"/> in that case.</param>
    /// <returns>The node that was added, or <see langword="null"/> if <paramref name="currentNode"/> has been removed from the list and
    /// <paramref name="allowAfterRemovedNode"/> is <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This operation takes O(1) time unless <paramref name="currentNode"/> has been removed from the list, in which case the list is searched backwards
    /// from its former position for the nearest node that is still present and the value is added after that node.</para>
    /// <para>Values added relative to a node that has been removed may end up in a different relative order than they would have had if the node were still
    /// in the list.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="currentNode"/> does not belong to this list.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node? AddAfter(Node currentNode, T value, bool allowAfterRemovedNode) => AddNear(currentNode, value, allowAfterRemovedNode, addBefore: false);

    /// <summary>
    /// Adds a value after the specified node, or near the position the node occupied if it has been removed from the list.
    /// </summary>
    /// <param name="currentNode">The node to add the value after.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>The node that was added.</returns>
    /// <remarks>
    /// <inheritdoc cref="AddAfter(Node, T, bool)" path="/remarks/*"/>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="currentNode"/> does not belong to this list.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node AddAfter(Node currentNode, T value)
    {
        var result = AddAfter(currentNode, value, true);
        Debug.Assert(result is { }, "Result should not be null when allowing adding after removed node.");
        return result;
    }

    /// <summary>
    /// Searches the list for the first value that is equal to <paramref name="existingValue"/> and adds <paramref name="value"/> before it.
    /// </summary>
    /// <param name="existingValue">The value to search for.</param>
    /// <param name="value">The value to add.</param>
    /// <param name="comparer">The comparer to use to determine equality, or <see langword="null"/> to use the default comparer.</param>
    /// <returns>The node that was added, or <see langword="null"/> if <paramref name="existingValue"/> was not found in the list.</returns>
    /// <remarks>
    /// This operation enumerates the list, so it takes O(n) time plus the cost of the comparisons. Nodes added by other threads during the search are not
    /// considered.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node? TryInsertBefore(T existingValue, T value, IEqualityComparer<T>? comparer = null) => TryInsertNear(existingValue, value, comparer, addBefore: true);

    /// <summary>
    /// Searches the list for the first value that is equal to <paramref name="existingValue"/> and adds <paramref name="value"/> after it.
    /// </summary>
    /// <param name="existingValue">The value to search for.</param>
    /// <param name="value">The value to add.</param>
    /// <param name="comparer">The comparer to use to determine equality, or <see langword="null"/> to use the default comparer.</param>
    /// <returns>The node that was added, or <see langword="null"/> if <paramref name="existingValue"/> was not found in the list.</returns>
    /// <remarks>
    /// This operation enumerates the list, so it takes O(n) time plus the cost of the comparisons. Nodes added by other threads during the search are not
    /// considered.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node? TryInsertAfter(T existingValue, T value, IEqualityComparer<T>? comparer = null) => TryInsertNear(existingValue, value, comparer, addBefore: false);

    /// <summary>
    /// Searches the list for the first value that is equal to <paramref name="existingValue"/> and adds <paramref name="value"/> before it.
    /// </summary>
    /// <param name="existingValue">The value to search for.</param>
    /// <param name="value">The value to add.</param>
    /// <param name="comparer">The comparer to use to determine equality, or <see langword="null"/> to use the default comparer.</param>
    /// <returns>The node that was added.</returns>
    /// <remarks>
    /// This operation enumerates the list, so it takes O(n) time plus the cost of the comparisons. Nodes added by other threads during the search are not
    /// considered.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="existingValue"/> was not found in the list.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node InsertBefore(T existingValue, T value, IEqualityComparer<T>? comparer = null)
    {
        var result = TryInsertNear(existingValue, value, comparer, addBefore: true);

        if (result is null)
            Throw.ItemNotFound(nameof(existingValue));

        return result;
    }

    /// <summary>
    /// Searches the list for the first value that is equal to <paramref name="existingValue"/> and adds <paramref name="value"/> after it.
    /// </summary>
    /// <param name="existingValue">The value to search for.</param>
    /// <param name="value">The value to add.</param>
    /// <param name="comparer">The comparer to use to determine equality, or <see langword="null"/> to use the default comparer.</param>
    /// <returns>The node that was added.</returns>
    /// <remarks>
    /// This operation enumerates the list, so it takes O(n) time plus the cost of the comparisons. Nodes added by other threads during the search are not
    /// considered.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="existingValue"/> was not found in the list.</exception>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public Node InsertAfter(T existingValue, T value, IEqualityComparer<T>? comparer = null)
    {
        var result = TryInsertNear(existingValue, value, comparer, addBefore: false);

        if (result is null)
            Throw.ItemNotFound(nameof(existingValue));

        return result;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the values in the list.
    /// </summary>
    /// <remarks>
    /// <para>Nodes whose values have been garbage collected are skipped, so the enumerator never returns <see langword="null"/>. Use
    /// <see cref="GetNodeEnumerator()"/> to enumerate nodes instead of values.</para>
    /// <para>Nodes added by other threads while enumerating may be included depending on timing. Use <see cref="Enumerator.WasAddedDuringEnumeration"/> to
    /// detect them, or <see cref="Enumerator.AsEnumerable(bool, bool)"/> to skip them.</para>
    /// <para>Enumerating the entire list takes O(n) time. Each step takes O(1) time unless the current node was removed by another thread, in which case the
    /// enumerator searches forward for the next node that is still present.</para>
    /// </remarks>
    public Enumerator GetEnumerator() => new(GetNodeEnumerator());

    /// <summary>
    /// Returns an enumerator that iterates through the values in the list starting from the position of the specified node.
    /// </summary>
    /// <remarks>
    /// <para><paramref name="startNode"/> itself is not included in the enumeration. The first call to <see cref="Enumerator.MoveNext"/> moves to the value
    /// after it and the first call to <see cref="Enumerator.MovePrevious"/> moves to the value before it.</para>
    /// <inheritdoc cref="GetEnumerator()" path="/remarks/*"/>
    /// </remarks>
    /// <exception cref="InvalidOperationException"><paramref name="startNode"/> does not belong to this list.</exception>
    public Enumerator GetEnumerator(Node startNode) => new(GetNodeEnumerator(startNode));

    /// <summary>
    /// Returns an enumerator that iterates through the nodes in the list.
    /// </summary>
    /// <remarks>
    /// <para>All nodes are enumerated, including nodes whose values have been garbage collected but have not yet been removed from the list, so
    /// <see cref="Node.Value"/> may be <see langword="null"/> for some of them. Use <see cref="GetEnumerator()"/> to enumerate only the values that are still
    /// available.</para>
    /// <para>Nodes added by other threads while enumerating may be included depending on timing. Use <see cref="NodeEnumerator.WasAddedDuringEnumeration"/> to
    /// detect them, or <see cref="NodeEnumerator.AsEnumerable(bool, bool)"/> to skip them.</para>
    /// <para>Enumerating the entire list takes O(n) time. Each step takes O(1) time unless the current node was removed by another thread, in which case the
    /// enumerator searches forward for the next node that is still present.</para>
    /// </remarks>
    public NodeEnumerator GetNodeEnumerator() => new(this, null);

    /// <summary>
    /// Returns an enumerator that iterates through the nodes in the list starting from the position of the specified node.
    /// </summary>
    /// <remarks>
    /// <para><paramref name="startNode"/> itself is not included in the enumeration. The first call to <see cref="NodeEnumerator.MoveNext"/> moves to the node
    /// after it and the first call to <see cref="NodeEnumerator.MovePrevious"/> moves to the node before it.</para>
    /// <inheritdoc cref="GetNodeEnumerator()" path="/remarks/*"/>
    /// </remarks>
    /// <exception cref="InvalidOperationException"><paramref name="startNode"/> does not belong to this list.</exception>
    public NodeEnumerator GetNodeEnumerator(Node startNode)
    {
        CheckNode(startNode);
        return new(this, startNode);
    }

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new HeapValueEnumerator(GetEnumerator(), reversed: false, skipNewNodes: false);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => new HeapValueEnumerator(GetEnumerator(), reversed: false, skipNewNodes: false);

    /// <summary>
    /// Searches for the first value in the list that matches the specified predicate.
    /// </summary>
    /// <returns>The node and value of the first match, or <see langword="null"/> if no match was found.</returns>
    /// <remarks>
    /// This operation enumerates the list, so it takes O(n) time plus the cost of the predicate calls. Nodes added by other threads during the search are not
    /// considered.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public (Node Node, T Value)? Find(Predicate<T> match)
    {
        var enumerator = GetEnumerator();

        while (enumerator.MoveNext())
        {
            if (enumerator.WasAddedDuringEnumeration)
                continue;

            var current = enumerator.CurrentNode;
            var currentValue = enumerator.Current;

            if (match(currentValue))
            {
                // Make the operation somewhat atomic (i.e., if someone else removed it while we weren't holding the lock, we consider that as happened before
                // us and thus not counting as a successful find here):
                using var scope = EnterLock(out bool wasDisposed);

                if (wasDisposed)
                {
                    GC.KeepAlive(currentValue);
                    break;
                }
                else if (current._isRemoved)
                {
                    GC.KeepAlive(currentValue);
                    continue;
                }

                GC.KeepAlive(currentValue);
                return (current, currentValue);
            }

            GC.KeepAlive(currentValue);
        }

        return null;
    }

    /// <summary>
    /// Searches for the first value in the list that is equal to the specified value.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <param name="comparer">The comparer to use to determine equality, or <see langword="null"/> to use the default comparer.</param>
    /// <returns>The node and value of the first match, or <see langword="null"/> if no match was found.</returns>
    /// <remarks>
    /// This operation enumerates the list, so it takes O(n) time plus the cost of the comparisons. Nodes added by other threads during the search are not
    /// considered.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public (Node Node, T Value)? Find(T value, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;

        var enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.WasAddedDuringEnumeration)
                continue;

            var current = enumerator.CurrentNode;
            var currentValue = enumerator.Current;

            if (comparer.Equals(currentValue, value))
            {
                // Make the operation somewhat atomic (i.e., if someone else removed it while we weren't holding the lock, we consider that as happened before
                // us and thus not counting as a successful find here):
                using var scope = EnterLock(out bool wasDisposed);

                if (wasDisposed)
                {
                    GC.KeepAlive(value);
                    break;
                }
                else if (current._isRemoved)
                {
                    GC.KeepAlive(value);
                    continue;
                }

                GC.KeepAlive(currentValue);
                return (current, currentValue);
            }

            GC.KeepAlive(currentValue);
        }

        return null;
    }

    /// <summary>
    /// Determines whether the list contains a value that is equal to the specified value.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <param name="comparer">The comparer to use to determine equality, or <see langword="null"/> to use the default comparer.</param>
    /// <remarks>
    /// This operation enumerates the list, so it takes O(n) time plus the cost of the comparisons. Nodes added by other threads during the search are not
    /// considered.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public bool Contains(T value, IEqualityComparer<T>? comparer = null)
    {
        // Just delegate to Find:
        return Find(value, comparer).HasValue;
    }

    /// <summary>
    /// Removes the specified node from the list. Does nothing if the node has already been removed.
    /// </summary>
    /// <remarks>
    /// This operation takes O(1) time.
    /// </remarks>
    /// <exception cref="InvalidOperationException"><paramref name="node"/> does not belong to this list.</exception>
    public void Remove(Node node)
    {
        CheckNode(node);
        node.Dispose();
        GC.KeepAlive(this);
    }

    /// <summary>
    /// Removes the first value in the list that is equal to the specified value.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <param name="comparer">The comparer to use to determine equality, or <see langword="null"/> to use the default comparer.</param>
    /// <remarks>
    /// This operation enumerates the list, so it takes O(n) time plus the cost of the comparisons. Nodes added by other threads during the search are not
    /// considered.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public bool Remove(T value, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;

        var enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.WasAddedDuringEnumeration)
                continue;

            var current = enumerator.CurrentNode;
            var currentValue = enumerator.Current;

            if (comparer.Equals(currentValue, value))
            {
                // Make the operation somewhat atomic (i.e., if someone else removed it while we weren't holding the lock, we consider that as happened before
                // us and thus not counting as a successful removal here):
                using var scope = EnterLock(out bool wasDisposed);

                if (wasDisposed)
                {
                    GC.KeepAlive(currentValue);
                    break;
                }
                else if (current._isRemoved)
                {
                    GC.KeepAlive(currentValue);
                    continue;
                }

                current.Dispose();
                GC.KeepAlive(currentValue);
                GC.KeepAlive(value);
                return true;
            }

            GC.KeepAlive(currentValue);
        }

        GC.KeepAlive(value);
        return false;
    }

    /// <summary>
    /// Removes all nodes from the list.
    /// </summary>
    /// <remarks>
    /// Nodes are removed one at a time, so this operation takes O(n) time. <see cref="Dispose"/> is faster if the list will not be used again.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public void Clear()
    {
        // We clear by repeatedly deleting any node we can find until none are left - it is important we do it like this to ensure we only block for O(1)
        // time at most at once. If the user wants to clear "properly", they should call Dispose and create a new instance.

        var enumerator = GetNodeEnumerator();

        while (enumerator.MoveNext())
        {
            if (!enumerator.WasAddedDuringEnumeration)
                enumerator.Current.Dispose();
        }
    }

    /// <summary>
    /// Runs the specified operation while holding the list's internal lock, so that multiple operations can be performed without other threads modifying the
    /// list in between them.
    /// </summary>
    /// <param name="state">A state value to pass to the operation.</param>
    /// <param name="operation">The operation to run. It receives the list and the state value.</param>
    /// <remarks>
    /// <para>The lock is also required by the finalizer thread to remove nodes whose values have been garbage collected, so holding it for more than a short
    /// time delays finalization of every object in the process. Keep operations short. For long-running compound operations, use your own lock and account for
    /// nodes being removed from the list concurrently.</para>
    /// <para>Disposing the list from within <paramref name="operation"/> releases the lock immediately, so any code that runs in the operation after the dispose
    /// call is no longer protected by it.</para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The list has been disposed.</exception>
    public void UnsafePerformLockedOperation<TState>(TState state, Action<WeakList<T>, TState> operation)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
    {
        using var scope = EnterLock(out bool wasDisposed);
        Throw.IfDisposed(wasDisposed, typeof(WeakList<T>));
        operation(this, state);
    }

    /// <summary>
    /// Runs the specified operation while holding the list's internal lock if the lock can be acquired immediately, so that multiple operations can be
    /// performed without other threads modifying the list in between them.
    /// </summary>
    /// <param name="state">A state value to pass to the operation.</param>
    /// <param name="operation">The operation to run. It receives the list and the state value.</param>
    /// <returns><see langword="true"/> if the operation was run; <see langword="false"/> if the lock could not be acquired immediately or the list has been
    /// disposed.</returns>
    /// <remarks>
    /// <para>The lock is also required by the finalizer thread to remove nodes whose values have been garbage collected, so holding it for more than a short
    /// time delays finalization of every object in the process. Keep operations short. For long-running compound operations, use your own lock and account for
    /// nodes being removed from the list concurrently.</para>
    /// <para>Disposing the list from within <paramref name="operation"/> releases the lock immediately, so any code that runs in the operation after the dispose
    /// call is no longer protected by it.</para>
    /// </remarks>
    public bool UnsafeTryPerformLockedOperation<TState>(TState state, Action<WeakList<T>, TState> operation)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
    {
        using var scope = TryEnterLock(out bool wasDisposed, out bool entered);

        if (wasDisposed || !entered)
            return false;

        operation(this, state);
        return true;
    }

    /// <summary>
    /// Removes all nodes from the list and prevents further use. Most members throw <see cref="ObjectDisposedException"/> afterwards, and existing enumerators
    /// return no further values.
    /// </summary>
    /// <remarks>
    /// Disposing is faster than <see cref="Clear"/> and should be preferred for lists that will not be used again, since it also avoids the cost of finalizing
    /// the list.
    /// </remarks>
    public void Dispose()
    {
        using var scope = EnterLock(out bool wasDisposed);

        if (wasDisposed)
            return;

        HandleFailureOrDispose();
    }

    // The caller must hold the lock for the list when calling this.
    // Throws if the wrong list, and returns true if the node is still in the list, or false if not.
    private bool CheckNode(Node n)
    {
        if (n.ListDirect != this)
        {
            [StackTraceHidden]
            static void Throw() => throw new InvalidOperationException("The specified node does not belong to this list.");
            Throw();
        }

        return !n._isRemoved;
    }
}
