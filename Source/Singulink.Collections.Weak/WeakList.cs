using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Singulink.Collections.Utilities;
using Singulink.Collections.WeakCollectionHelpers;

namespace Singulink.Collections;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

/// <summary>
/// Represents a collection of weakly referenced values that maintains relative insertion order. This type is also automatically safe for concurrent access at
/// an operation level (that is, individual operations are thread-safe, but your own custom compound operations may still need external synchronization).
/// </summary>
/// <remarks>
/// <para>This type automatically cleans references for garbage collected values on its own without requiring any user interaction.</para>
/// <para>Note: Highly contested scenarios may experience significant practical performance degradation due to lock contention, be aware of this when using in
/// such environments.</para>
/// <para>Note: All provided big O runtimes are strict, but those above O(1) assume the case where no new nodes were added by another thread during the
/// operation - if new nodes were added concurrently, it may cause the operation to take longer than expected (e.g., if a concurrent operation happens to
/// always add a new node just after an enumeration's current node, then it will have to loop through all of those until it gets past them).</para>
/// <para>For optimal performance, avoid letting the finalizer run; instead, dispose the list explicitly or clear it - otherwise, the finalizer thread may be
/// blocked for a significant amount of time if the list is large.</para>
/// <para>Note: This type is not safe to resurrect, or use in a partially finalized state.</para>
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
    /// Initializes a new instance of the <see cref="WeakList{T}"/> class with no elements.
    /// </summary>
    public WeakList()
    {
        _head = new(null, this) { _isPseudoNode = true };
        _tail = _head;
        _containerValues = new();
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
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
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
    /// Gets the version of the list - this allows callers to detect whether a node was added after they begun a non-atomic operation.
    /// </summary>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
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
    /// Adds a value to the start of the list - takes O(1) time.
    /// </summary>
    /// <exception cref="ArgumentNullException">If the value is null.</exception>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
    public Node AddFirst(T value)
    {
        using var scope = EnterLock(out bool wasDisposed);
        Throw.IfDisposed(wasDisposed, typeof(WeakList<T>));
        DebugAssertNotDisposed();
        return InsertNearHelper(value, _head, addBefore: false); // Note: we add after, since the first one is the pseudo-node.
    }

    /// <summary>
    /// Adds a value to the end of the list - takes O(1) time.
    /// </summary>
    /// <exception cref="ArgumentNullException">If the value is null.</exception>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
    public Node AddLast(T value)
    {
        using var scope = EnterLock(out bool wasDisposed);
        Throw.IfDisposed(wasDisposed, typeof(WeakList<T>));
        DebugAssertNotDisposed();
        return InsertNearHelper(value, _tail, addBefore: false);
    }

    /// <summary>
    /// Adds a value before the specified node of the list - takes O(1) time.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="AddBefore(Node, T)" /> override behaves as if <paramref name="allowBeforeRemovedNode"/> is <see langword="true" />.</para>
    /// <para>If a node has been removed, multiple adds near it might result in inconsistent ordering compared to if it was still in the list.</para>
    /// <para>If adding next to a removed node, then the O(1) runtime is no longer guaranteed.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">If the value is null.</exception>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">If the node does not belong to this list.</exception>
    public Node? AddBefore(Node currentNode, T value, bool allowBeforeRemovedNode) => AddNear(currentNode, value, allowBeforeRemovedNode, addBefore: true);

    /// <inheritdoc cref="AddBefore(Node, T, bool)" />
    public Node AddBefore(Node currentNode, T value)
    {
        var result = AddBefore(currentNode, value, true);
        Debug.Assert(result is { }, "Result should not be null when allowing adding before removed node.");
        return result;
    }

    /// <summary>
    /// Adds a value after the specified node of the list - takes O(1) time.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="AddAfter(Node, T)" /> override behaves as if <paramref name="allowAfterRemovedNode"/> is <see langword="true" />.</para>
    /// <para>If a node has been removed, multiple adds near it might result in inconsistent ordering compared to if it was still in the list.</para>
    /// <para>If adding next to a removed node, then the O(1) runtime is no longer guaranteed.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">If the value is null.</exception>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">If the node does not belong to this list.</exception>
    public Node? AddAfter(Node currentNode, T value, bool allowAfterRemovedNode) => AddNear(currentNode, value, allowAfterRemovedNode, addBefore: false);

    /// <inheritdoc cref="AddAfter(Node, T, bool)" />
    public Node AddAfter(Node currentNode, T value)
    {
        var result = AddAfter(currentNode, value, true);
        Debug.Assert(result is { }, "Result should not be null when allowing adding after removed node.");
        return result;
    }

    /// <summary>
    /// Tries to add a value before the specified value of the list, or returns <see langword="null" /> - this method has the same runtime as
    /// <see cref="GetEnumerator()" /> plus O(n * comparer.Equals).
    /// </summary>
    /// <exception cref="ArgumentNullException">If the value is null.</exception>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
    public Node? TryInsertBefore(T existingValue, T value, IEqualityComparer<T>? comparer = null) => TryInsertNear(existingValue, value, comparer, addBefore: true);

    /// <summary>
    /// Tries to add a value after the specified value of the list, or returns <see langword="null" /> - this method has the same runtime as
    /// <see cref="GetEnumerator()" /> plus O(n * comparer.Equals).
    /// </summary>
    /// <exception cref="ArgumentNullException">If the value is null.</exception>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
    public Node? TryInsertAfter(T existingValue, T value, IEqualityComparer<T>? comparer = null) => TryInsertNear(existingValue, value, comparer, addBefore: false);

    /// <summary>
    /// Tries to add a value before the specified value of the list, or throws - this method has the same runtime as
    /// <see cref="GetEnumerator()" /> plus O(n * comparer.Equals).
    /// </summary>
    /// <exception cref="ArgumentNullException">If the value is null.</exception>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
    /// <exception cref="ArgumentException">If the specified existing value was not found.</exception>
    public Node InsertBefore(T existingValue, T value, IEqualityComparer<T>? comparer = null)
    {
        var result = TryInsertNear(existingValue, value, comparer, addBefore: true);
        if (result is null) Throw.ItemNotFound(nameof(existingValue));
        return result;
    }

    /// <summary>
    /// Tries to add a value after the specified value of the list, or throws - this method has the same runtime as
    /// <see cref="GetEnumerator()" /> plus O(n * comparer.Equals).
    /// </summary>
    /// <exception cref="ArgumentNullException">If the value is null.</exception>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
    /// <exception cref="ArgumentException">If the specified existing value was not found.</exception>
    public Node InsertAfter(T existingValue, T value, IEqualityComparer<T>? comparer = null)
    {
        var result = TryInsertNear(existingValue, value, comparer, addBefore: false);
        if (result is null) Throw.ItemNotFound(nameof(existingValue));
        return result;
    }

    /// <summary>
    /// Gets an enumerator for values in the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function only enumerates the available values, and skips nodes whose values have been collected.
    /// </para>
    /// <para>
    /// New nodes added during enumeration may be included in the enumeration, depending on timing; see <see cref="Enumerator.WasAddedDuringEnumeration" />
    /// and <see cref="Enumerator.AsEnumerable(bool, bool)" /> for controlling this behavior.
    /// </para>
    /// <inheritdoc cref="GetNodeEnumerator()" path="/remarks/*[position()>2]" />
    /// </remarks>
    public Enumerator GetEnumerator() => new(GetNodeEnumerator());

    /// <summary>
    /// Gets an enumerator for the values in the list that enumerates from the <paramref name="startNode"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="startNode"/> will not be included in the enumeration; enumeration will begin from the next valid node after or before it.
    /// </para>
    /// <inheritdoc cref="GetEnumerator()" path="/remarks/*" />
    /// </remarks>
    /// <exception cref="InvalidOperationException">If the specified node does not belong to this list.</exception>
    public Enumerator GetEnumerator(Node startNode) => new(GetNodeEnumerator(startNode));

    /// <summary>
    /// Gets an enumerator for nodes in the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function enumerates over all nodes in the list, regardless of whether their values have been collected yet or not (value removal is not immediate).
    /// </para>
    /// <para>
    /// New nodes added during enumeration may be included in the enumeration, depending on timing; see <see cref="NodeEnumerator.WasAddedDuringEnumeration" />
    /// and <see cref="NodeEnumerator.AsEnumerable(bool, bool)" /> for controlling this behavior.
    /// </para>
    /// <para>
    /// Given a list with no new nodes being added concurrently, full enumeration takes O(n) time.
    /// </para>
    /// <para>
    /// An individual enumeration step takes O(1) time when not resuming from a removed node.
    /// </para>
    /// <para>
    /// These big O runtimes assume no nodes being added concurrently, see <see cref="WeakList{T}" /> for remarks about that case.
    /// </para>
    /// </remarks>
    public NodeEnumerator GetNodeEnumerator() => new(this, null);

    /// <summary>
    /// Gets an enumerator for nodes in the list that enumerates from the <paramref name="startNode"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="startNode"/> will not be included in the enumeration; enumeration will begin from the next valid node after or before it.
    /// </para>
    /// <inheritdoc cref="GetNodeEnumerator()" path="/remarks/*" />
    /// </remarks>
    /// <exception cref="InvalidOperationException">If the specified node does not belong to this list.</exception>
    public NodeEnumerator GetNodeEnumerator(Node startNode)
    {
        CheckNode(startNode);
        return new(this, startNode);
    }

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new HeapValueEnumerator(GetEnumerator(), reversed: false, skipNewNodes: false);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => new HeapValueEnumerator(GetEnumerator(), reversed: false, skipNewNodes: false);

    /// <summary>
    /// Finds the first node matching the specified predicate - this method has the same runtime as <see cref="GetEnumerator()" /> plus O(n * predicate).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method enumerates the list looking for a matching node; if nodes are added by other threads during the operation, they will not be included.
    /// </para>
    /// </remarks>
    public (Node Node, T Value)? Find(Predicate<T> match)
    {
        var enumerator = GetEnumerator();

        while (enumerator.MoveNext())
        {
            if (enumerator.WasAddedDuringEnumeration) continue;
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
    /// Finds the first node that is considered equal according to the comparer - this method has the same runtime as <see cref="GetEnumerator()" /> plus
    /// O(n * comparer.Equals).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method enumerates the list looking for a matching node; if nodes are added by other threads during the operation, they will not be included.
    /// </para>
    /// </remarks>
    public (Node Node, T Value)? Find(T value, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;

        var enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.WasAddedDuringEnumeration) continue;
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
    /// Determines if the list contains any node that is considered equal according to the comparer - this method has the same runtime as
    /// <see cref="GetEnumerator()" /> plus O(n * comparer.Equals).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method enumerates the list looking for a matching node; if nodes are added by other threads during the operation, they will not be included.
    /// </para>
    /// </remarks>
    public bool Contains(T value, IEqualityComparer<T>? comparer = null)
    {
        // Just delegate to Find:
        return Find(value, comparer).HasValue;
    }

    /// <summary>
    /// Removes the specified node from the list if it is still in the list - takes O(1) time.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the specified node never belonged to this list.</exception>
    public void Remove(Node node)
    {
        CheckNode(node);
        node.Dispose();
        GC.KeepAlive(this);
    }

    /// <summary>
    /// Removes the first instance of the specified value from the list if it is in the list - this method has the same runtime as
    /// <see cref="GetEnumerator()" /> plus O(n * comparer.Equals).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method enumerates the list looking for a matching node; if nodes are added by other threads during the operation, they will not be included.
    /// </para>
    /// </remarks>
    public bool Remove(T value, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;

        var enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.WasAddedDuringEnumeration) continue;
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
    /// Removes all nodes from the list - takes O(n) time if no new nodes are added concurrently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method removes all nodes one-by-one; using <see cref="Dispose" /> is faster if you do not need to reuse the list instance.
    /// </para>
    /// <para>
    /// This big O runtime assume no nodes being added concurrently, see <see cref="WeakList{T}" /> for remarks about that case.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        // We clear by repeatedly deleting any node we can find until none are left - it is important we do it like this to ensure we only block for O(1)
        // time at most at once. If the user wants to clear "properly", they should call Dispose and create a new instance.

        var enumerator = GetNodeEnumerator();

        while (enumerator.MoveNext())
        {
            if (!enumerator.WasAddedDuringEnumeration) enumerator.Current.Dispose();
        }
    }

    /// <summary>
    /// <para>
    /// Performs a locked operation on the list, allowing multiple operations to be completed without potential intermediate changes.
    /// </para>
    /// <para>
    /// Note: holding the lock for more than a short period of time may cause finalizer starvation due to blocking the finalizer thread, hence why this API is
    /// considered unsafe; if you need to perform long-running multi-part operations, you should use your own different lock and ensure you handle concurrent
    /// removal with it from the list's internal lock (this setup won't block the finalizer).
    /// </para>
    /// </summary>
    /// <exception cref="ObjectDisposedException">If the instance has been disposed.</exception>
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
    /// <para>
    /// Attempts to perform a locked operation on the list, allowing multiple operations to be completed without potential intermediate changes.
    /// </para>
    /// <para>
    /// If the lock could not be immediately acquired, or the list has been disposed, the operation will not be performed and <see langword="false" /> will be
    /// returned.
    /// </para>
    /// <para>
    /// Note: holding the lock for more than a short period of time may cause finalizer starvation due to blocking the finalizer thread, hence why this API is
    /// considered unsafe; if you need to perform long-running multi-part operations, you should use your own different lock and ensure you handle concurrent
    /// removal with it from the list's internal lock (this setup won't block the finalizer).
    /// </para>
    /// </summary>
    public bool UnsafeTryPerformLockedOperation<TState>(TState state, Action<WeakList<T>, TState> operation)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
    {
        using var scope = TryEnterLock(out bool wasDisposed, out bool entered);
        if (wasDisposed || !entered) return false;
        operation(this, state);
        return true;
    }

    /// <summary>
    /// Disposes the <see cref="WeakList{T}" />, removing all nodes and preventing further use.
    /// </summary>
    public void Dispose()
    {
        using var scope = EnterLock(out bool wasDisposed);
        if (wasDisposed) return;
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
