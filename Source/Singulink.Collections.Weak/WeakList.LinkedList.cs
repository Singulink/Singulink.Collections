using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

using Singulink.Collections.Utilities;

namespace Singulink.Collections;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

/// <content>
/// Contains the linked list implementation for <see cref="WeakList{T}"/>.
/// </content>
public sealed partial class WeakList<T>
{
    // Helper to allocate a node - doesn't link it into the linked list.
    // Note: callers must GC.KeepAlive the value until after it is fully linked in.
    // Note: callers must hold the lock for the list when calling this and have already checked for disposal.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Node AllocNode(T value)
    {
        DebugAssertNotDisposed();
        InternalNode internalNode = new();
        Node node = new(internalNode, this);
        InternalNodeFinalizeHelper internalNodeHelper = new();
        internalNode._node = WeakHandle.Alloc(node);
        internalNodeHelper._impl = StrongHandle.Alloc(internalNode);
        node._internalNodeHelper = new(internalNodeHelper);
#if NET
        internalNode._dependentHandle = new DependentHandle(value, internalNodeHelper);
        internalNode._finalizeHelperNode = WeakHandle.Alloc(_internalNodes.List.AddLast(node._internalNodeHelper));
        internalNode._trackingInfoHandle = StrongHandle.Alloc(_internalNodes);
#else
        internalNode._value = WeakHandle.Alloc(value);
        internalNode._cwtNode = WeakHandle.Alloc(_cwt.GetValue(value, static _ => []).AddLast(internalNodeHelper));
#endif
        GC.KeepAlive(internalNode);
        _version++;
        Debug.Assert(_version > 0, "Version overflowed.");
        node._version = _version;
        return node;
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    // Note: the caller must guarantee that value is not null.
    private Node InsertNearHelper(T value, Node node, bool addBefore)
    {
        try
        {
            // Increment size:
            // Note: our memory barrier to ensure the write is visible before the lock exits is later in the function.
            _size++;
            Debug.Assert(_size > 0 && _size < (nint)(~(nuint)0 / 2), "Size overflowed.");

            // Allocate our node:
            Node newNode = AllocNode(value);

            // Get the nodes before/after where we're inserting & whether it's the new tail node or not:
            var prevNode = addBefore ? node._prev : node;
            var nextNode = addBefore ? node : node._next;
            bool isTail = nextNode is null;

            // If it's meant to be the new tail node, update that:
            if (isTail) _tail = newNode;

            // Note: it's impossible for prevNode to be null, since we never insert before the pseudo-node.
            Debug.Assert(prevNode is not null, "Previous node must not be null when inserting near a node.");

            // Update the links of all the nodes:
            newNode._prev = prevNode;
            newNode._next = nextNode;
            prevNode._next = newNode;
            nextNode?._prev = newNode;

            // Return our node:
            return newNode;
        }
        catch
        {
            HandleFailureOrDispose();
            throw;
        }
        finally
        {
            GC.KeepAlive(value);
        }
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Node? GetNextNode(Node? n, out bool isRemovedNode)
    {
        // If n is null, return the first node if we have one:
        DebugAssertNotDisposed();
        isRemovedNode = false;
        if (n is null && _size > 0) return _head._next;
        else if (n is null) return null;

        // Check if node has been removed as the caller needs to handle it specially:
        if (n is { _isRemoved: true })
        {
            isRemovedNode = true;
            return n;
        }

        // Return the next node:
        return n._next;
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Node? GetPrevNode(Node? n, out bool isRemovedNode)
    {
        // If n is null, return the last node if we have one:
        DebugAssertNotDisposed();
        isRemovedNode = false;
        if (n is null && _size > 0) return _tail;
        if (n is null) return null;

        // Check if node has been removed as the caller needs to handle it specially:
        if (n is { _isRemoved: true })
        {
            isRemovedNode = true;
            return n;
        }

        // Return the previous node (or null, if it's the pseudo-node):
        return n._prev switch
        {
            { _isPseudoNode: true } => null,
            var prev => prev,
        };
    }

    // Finishes destroying a node by updating sizes, marking as removed, and cleaning up the internal node.
    private void FinishDestroyNode(Node n)
    {
        // Update size:
        _size--;

        // Mark node as removed for enumerators:
        n._isRemoved = true;

        // Note - we don't have to clean up the internal node, as it's only possible to get to this method from the method in that class that already.
        Debug.Assert((n.GetInternalNodeHelper()?._impl.Handle).GetValueOrDefault() == IntPtr.Zero, "Should only be called through InternalNode's deletion.");

        // Free node's reference to internal node finalizer helper:
        n._internalNodeHelper = null;
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    // Note: the caller must guarantee that node is valid.
    private void DeleteHelper(Node node)
    {
        try
        {
            // Check if already removed.
            if (node._isRemoved) return;

            // Get previous & next nodes:
            var prevNode = node._prev;
            var nextNode = node._next;

            // Note: prevNode should never be null, since we never remove the pseudo-node.
            Debug.Assert(prevNode is not null, "Previous node must not be null when deleting a node.");

            // Update links of previous and next nodes:
            prevNode._next = nextNode;
            nextNode?._prev = prevNode;

            // If we removed the tail node, the previous node becomes the new tail (it's the last live node, or the pseudo-node if the list is now empty).
            if (nextNode is null) _tail = prevNode;

            // Destroy the node (note: we leave prev & next links as they are so enumeration can continue):
            FinishDestroyNode(node);
        }
        catch
        {
            HandleFailureOrDispose();
            throw;
        }
    }

    // Helper for AddBefore and AddAfter
    private Node? AddNear(Node currentNode, T value, bool allowNearRemovedNode, bool addBefore)
    {
        // Validate parameters:
        CheckNode(currentNode);
        bool movedAlready = false;
        while (true)
        {
            // If we can tell we have a removed node without locking, handle now:
            if (currentNode is { _isRemoved: true })
            {
                if (!allowNearRemovedNode) return null;
                movedAlready = true;
                do currentNode = currentNode._prev;
                while (currentNode is { _isRemoved: true });
            }

            // Enter lock & finish checking:
            using (EnterLock(out bool wasDisposed))
            {
                Throw.IfDisposed(wasDisposed, typeof(WeakList<T>));

                // Handle a removed node if needed by running the outer loop again:
                if (currentNode is { _isRemoved: true }) continue;
                Debug.Assert(currentNode is not null, "Current node should not be null, since we're not disposed and thus going left must lead to the pseudo-node before we hit null.");

                // Determine if we want to add before or not:
                // Note: if we had to move due to a removed node, we need special handling.
                bool addBeforeLocal = addBefore;
                if (movedAlready) addBeforeLocal = false;

                // Add the node:
                return InsertNearHelper(value, currentNode, addBefore: addBeforeLocal);
            }
        }
    }

    private Node? TryInsertNear(T existingValue, T value, IEqualityComparer<T>? comparer, bool addBefore)
    {
        comparer ??= EqualityComparer<T>.Default;

        var enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.WasAddedDuringEnumeration) continue;
            var current = enumerator.CurrentNode;
            var currentValue = enumerator.Current;
            if (comparer.Equals(currentValue, existingValue))
            {
                using var scope = EnterLock(out bool wasDisposed);
                Throw.IfDisposed(wasDisposed, typeof(WeakList<T>));

                // Check if removed while we weren't holding the lock - we may as well make this somewhat atomic:
                if (!current._isRemoved)
                {
                    var result = AddNear(current, value, allowNearRemovedNode: false, addBefore);
                    GC.KeepAlive(value);
                    GC.KeepAlive(currentValue);
                    Debug.Assert(result is not null, "Result should not be null when adding near non-removed node.");
                    return result;
                }
            }

            GC.KeepAlive(currentValue);
        }

        GC.KeepAlive(value);
        return null;
    }

    // Handle failure in a way that doesn't potentially cause further corruption or finalizers to throw, etc.
    // Also implements the dispose logic.
    // The caller must hold the lock for the list when calling this.
    private void HandleFailureOrDispose()
    {
        // Assert not disposed yet:
        DebugAssertNotDisposed();

        // Mark disposed:
        // Note: we want other threads to be able to see it as soon as (in program order) we release the lock, even if they don't take it. Luckily, the Exit
        // method gives us volatile write barrier semantics, which means that the write here must be visible by the time it is released.
        var oldRoot = _head;
        _head = null;
        _tail = null;

        // Exit the lock held by this thread now so that other threads can proceed:
        while (_locker.IsHeldByCurrentThread) _locker.Exit();

        // Clean out resources:
#if NET
        _internalNodes.List.Clear();
        _cleanupHelper.ListRef.Dispose();
        GC.SuppressFinalize(_cleanupHelper);
        GC.KeepAlive(_cleanupHelper);
        GC.KeepAlive(_internalNodes);
#else
        // Note: on .NET Standard 2.0, there's no CWT.Clear(), so we just null it out and let the GC clean it up.
#if NETSTANDARD2_1_OR_GREATER
        _cwt.Clear();
#endif
        GC.KeepAlive(_cwt); // Ensure the CWT lives to here at least.
        _cwt = null;
#endif

        // Forget all nodes:
        var n = oldRoot;
        while (n is not null)
        {
            var next = n._next;
            n._isRemoved = true;
            n._prev = null;
            n._next = null;

            // Finalizer is not critical here, other than our handles & marking removed, so clean those up and then suppress:
            if (n.GetInternalNodeHelper() is { } helper)
            {
                n._internalNode?.EarlyDispose(helper, _locker, true);
            }

            // Set node finalizer to null:
            n._internalNodeHelper = null;

            // Move to next node:
            n = next;
        }

        // Suppress finalizer for this list now, as we've cleaned up everything:
        GC.SuppressFinalize(this);
    }
}
