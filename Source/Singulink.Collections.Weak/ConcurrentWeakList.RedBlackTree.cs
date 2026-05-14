using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

using Singulink.Collections.Utilities;

namespace Singulink.Collections;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable RCS1032 // Remove redundant parentheses

/// <content>
/// Contains the red-black tree implementation for <see cref="ConcurrentWeakList{T}"/>.
/// </content>
public sealed partial class ConcurrentWeakList<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsRightChild(Node node)
    {
        return node._parent is not null && node._parent._right == node;
    }

    // Helper to detect corruption in Release mode that would cause the lock to be held forever, leading to an app-wide deadlock:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckIterationCount(ref int iterationCount)
    {
        if (++iterationCount > 512)
            ThrowUnreachableExceptionForOverIterated();
    }

    // Helper to allocate a node - doesn't set it up in the red-black tree.
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
        GC.KeepAlive(internalNodeHelper);

        // Note: our memory barrier to ensure the write is visible before the lock exits is in the caller (ManualAdd).
        _version++;
        Debug.Assert(_version > 0, "Version overflowed.");
        node._version = _version;
        return node;
    }

    // Adds to binary search tree at the given index, ignoring red-black tree rules - inserts as a red node.
    // This has the same restrictions as AllocNode, since it is not set up in a valid way for red-black trees.
    // Assumes that the caller validated the index.
    // Note: index is the caller index (0-based for real items); internally we offset by 1 to account for the pseudo-node.
    private Node BSTAdd(T value, nint index)
    {
        // Assert not disposed:
        DebugAssertNotDisposed();

        // Offset by 1 to account for the pseudo-node:
        nint implIndex = index + 1;

        // Find the node to place it under:
        Node parent = _root;
        bool becomeLeftChild;
        if (index == _size)
        {
            // Optimize for appending to end (common case) - go to rightmost node and insert as its right child:
            int iterationCount = 0;
            while (parent is { _right: not null })
            {
                CheckIterationCount(ref iterationCount);
                parent = parent._right;
            }

            becomeLeftChild = false;
        }
        else
        {
            // Loop normally, until we find the right place, using subtree size to calculate the index of the existing nodes at each step:
            nint childrenOrderedBeforeParent = (parent._left?._subtreeSize ?? 0) + 1;
            becomeLeftChild = implIndex < childrenOrderedBeforeParent;
            Node nextParent;
            int iterationCount = 0;
            while ((nextParent = becomeLeftChild ? parent._left : parent._right) != null)
            {
                CheckIterationCount(ref iterationCount);
                parent = nextParent;
                if (becomeLeftChild)
                {
                    // Subtract one for old parent and new parent's right subtree's size:
                    childrenOrderedBeforeParent -= 1 + (parent._right?._subtreeSize ?? 0);
                }
                else
                {
                    // Add new parent's left subtree size + 1 for the parent itself:
                    childrenOrderedBeforeParent += (parent._left?._subtreeSize ?? 0) + 1;
                }

                // Decide which way to go next:
                becomeLeftChild = implIndex < childrenOrderedBeforeParent;
            }
        }

        // Call into ManualAdd:
        return ManualAdd(value, parent, becomeLeftChild);
    }

    // Similar to BSTAdd, but adds at the position provided by the caller instead of searching for it.
    // This has the same restrictions as AllocNode.
    // Note: the caller must guarantee that their provided parent and becomeLeftChild are valid.
    private Node ManualAdd(T value, Node parent, bool becomeLeftChild)
    {
        // Increment size:
        // Note: our memory barrier to ensure the write is visible before the lock exits is later in the function.
        _size++;
        Debug.Assert(_size > 0 && _size < (nint)(~(nuint)0 / 2), "Size overflowed.");

        // Set up the new node:
        Debug.Assert((becomeLeftChild ? parent._left : parent._right) == null, "Slot already occupied.");
        var node = AllocNode(value);
        (becomeLeftChild ? ref parent._left : ref parent._right) = node;
        node._parent = parent;
        node._color = Node.Color.Red;

        // Update subtree sizes up the tree (custom step - takes O(log n) time):
        node._subtreeSize = 1;
        int iterationCount = 0;
        do
        {
            CheckIterationCount(ref iterationCount);
            parent._subtreeSize++;
            parent = parent._parent;
        }
        while (parent is not null);

        // Insert a memory barrier, to ensure that the new size & version are visible by the time the lock exits, to threads that do not re-enter it;
        // otherwise, nothing stops the write from being re-ordered after the lock is released.
        Thread.MemoryBarrier();

        // Return the new node:
        return node;
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    // Note: the tree might not be valid for red-black rules after this is called, even if it was before.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LeftRotate(Node n)
    {
        /*
        Change this:

          X
         / \
        A   Y
           / \
          B   C

        To this:

            Y
           / \
          X   C
         / \
        A   B

        */

        // Get all the nodes/subtrees that we need:
        var x = n;
        var y = x._right;
        Debug.Assert(y is { }, "Caller should ensure n._right is not null.");
        var b = y._left;

        // Update the parent to point to y instead of x:
        if (x._parent is null)
        {
            _root = y;
            y._parent = null;
        }
        else if (IsRightChild(x))
        {
            x._parent._right = y;
            y._parent = x._parent;
        }
        else
        {
            x._parent._left = y;
            y._parent = x._parent;
        }

        // Update all of the child pointers:
        x._parent = y;
        x._right = b;
        y._left = x;
        b?._parent = x;

        // Update x & y's subtree sizes:
        x._subtreeSize += (b?._subtreeSize ?? 0) - y._subtreeSize;
        y._subtreeSize += x._subtreeSize - (b?._subtreeSize ?? 0);
    }

    // Same restrictions as LeftRotate.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RightRotate(Node n)
    {
        /*
        Change this:

            X
           / \
          Y   C
         / \
        A   B

        To this:

          Y
         / \
        A   X
           / \
          B   C

        */

        // Get all the nodes/subtrees that we need:
        var x = n;
        var y = x._left;
        Debug.Assert(y is { }, "Caller should ensure n._left is not null.");
        var b = y._right;

        // Update the parent to point to y instead of x:
        if (x._parent is null)
        {
            _root = y;
            y._parent = null;
        }
        else if (IsRightChild(x))
        {
            x._parent._right = y;
            y._parent = x._parent;
        }
        else
        {
            x._parent._left = y;
            y._parent = x._parent;
        }

        // Update all of the child pointers:
        x._parent = y;
        x._left = b;
        y._right = x;
        b?._parent = x;

        // Update x & y's subtree sizes:
        x._subtreeSize += (b?._subtreeSize ?? 0) - y._subtreeSize;
        y._subtreeSize += x._subtreeSize - (b?._subtreeSize ?? 0);
    }

    // Same restrictions as AllocNode.
    private void FixInsert(Node n)
    {
        // Assert not disposed:
        DebugAssertNotDisposed();

        // Loop while the node's parent is not black & node is not the root:
        int iterationCount = 0;
        while (n is { _parent._color: Node.Color.Red })
        {
            // Get the uncle node:
            CheckIterationCount(ref iterationCount);
            var parent = n._parent;
            var grandparent = parent._parent;
            Debug.Assert(grandparent is { }, "Grandparent should not be null if parent is red.");
            var uncle = IsRightChild(parent) ? grandparent._left : grandparent._right;

            // If uncle is red:
            if (uncle is { _color: Node.Color.Red })
            {
                // Recolor parent & uncle to black, grandparent to red, and continue up the tree from grandparent:
                parent._color = Node.Color.Black;
                uncle._color = Node.Color.Black;
                grandparent._color = Node.Color.Red;
                n = grandparent;
            }
            else
            {
                // Check if triangle (convert into line):
                bool nIsRightChild = IsRightChild(n);
                if (nIsRightChild != IsRightChild(parent))
                {
                    // Rotate parent in opposite direction to n:
                    if (nIsRightChild) LeftRotate(parent);
                    else RightRotate(parent);

                    // Update nodes for next step (the rotate only results in these changes):
                    (n, parent) = (parent, n);
                }

                // Handle line:

                // Rotate grandparent in opposite direction to node:
                if (IsRightChild(n)) LeftRotate(grandparent);
                else RightRotate(grandparent);

                // Recolor parent to black and grandparent to red, then we're done:
                parent._color = Node.Color.Black;
                grandparent._color = Node.Color.Red;
                return;
            }
        }

        // Ensure root node is black:
        _root._color = Node.Color.Black;
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    // Note: the caller must guarantee that value is not null.
    private Node InsertAtHelper(T value, nint index)
    {
        try
        {
            var node = BSTAdd(value, index);
            FixInsert(node);
            Check();
            return node;
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
    // Note: the caller must guarantee that the provided parent and becomeLeftChild are valid.
    // Note: the caller must guarantee that value is not null.
    private Node InsertManualHelper(T value, Node parent, bool becomeLeftChild)
    {
        try
        {
            var node = ManualAdd(value, parent, becomeLeftChild);
            FixInsert(node);
            Check();
            return node;
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
    // Assumes that the caller validated the index.
    // Note: index is the caller index (0-based for real items); internally we offset by 1 to account for the pseudo-node.
    // Use index -1 to get the pseudo-node (internal index 0).
    private Node GetNodeAtImpl(nint index)
    {
        // Assert not disposed yet & check index:
        DebugAssertNotDisposed();
        Debug.Assert(index >= -1 && index < _size, "Index out of range.");

        // Offset by 1 to account for the pseudo-node:
        nint implIndex = index + 1;

        Node parent = _root;
        if (implIndex == parent._subtreeSize - 1)
        {
            // Optimize for getting last node - go to rightmost node and return it:
            int iterationCount = 0;
            while (parent is { _right: not null })
            {
                CheckIterationCount(ref iterationCount);
                parent = parent._right;
            }

            return parent;
        }
        else
        {
            // Loop normally, until we find the right place, using subtree size to calculate the index of the existing nodes at each step:
            nint childrenOrderedBeforeParent = (parent._left?._subtreeSize ?? 0) + 1;
            bool becomeLeftChild = implIndex < childrenOrderedBeforeParent;
            Node nextParent;
            int iterationCount = 0;
            while ((nextParent = becomeLeftChild ? parent._left : parent._right) != null)
            {
                // Check if we found the node:
                CheckIterationCount(ref iterationCount);
                if (implIndex == childrenOrderedBeforeParent - 1) return parent;

                // Move down the tree:
                parent = nextParent;
                if (becomeLeftChild)
                {
                    // Subtract one for old parent and new parent's right subtree's size:
                    childrenOrderedBeforeParent -= 1 + (parent._right?._subtreeSize ?? 0);
                }
                else
                {
                    // Add new parent's left subtree size + 1 for the parent itself:
                    childrenOrderedBeforeParent += (parent._left?._subtreeSize ?? 0) + 1;
                }

                // Decide which way to go next:
                becomeLeftChild = implIndex < childrenOrderedBeforeParent;
            }

            // We should have found it by now:
            Debug.Assert(implIndex == childrenOrderedBeforeParent - 1, "Failed to find node at index.");
            return parent;
        }
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Node? GetNextNode(Node? n, out bool isRemovedNode)
    {
        // If n is null, return the first node if we have one:
        isRemovedNode = false;
        if (n is null && _size > 0) return GetNodeAtImpl(0);
        else if (n is null) return null;

        // Check if node has been removed as the caller needs to handle it specially:
        if (n is { _color: Node.Color.Removed })
        {
            isRemovedNode = true;
            return n;
        }

        // If we have a right child, go down that:
        int iterationCount = 0;
        if (n._right is not null)
        {
            n = n._right;
            while (n._left is not null)
            {
                CheckIterationCount(ref iterationCount);
                n = n._left;
            }

            return n;
        }

        // Otherwise, go up until we find a parent that we are a left child of:
        while (n is not null && (n._parent is null || IsRightChild(n)))
        {
            CheckIterationCount(ref iterationCount);
            n = n._parent;
        }

        // Return the parent, or null if we reached the root:
        return n?._parent;
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Node? GetPrevNode(Node? n, out bool isRemovedNode)
    {
        // If n is null, return the last node if we have one:
        isRemovedNode = false;
        if (n is null && _size > 0) return GetNodeAtImpl(_size - 1);
        if (n is null) return null;

        // Check if node has been removed as the caller needs to handle it specially:
        if (n is { _color: Node.Color.Removed })
        {
            isRemovedNode = true;
            return n;
        }

        // If we have a left child, go down that:
        int iterationCount = 0;
        if (n._left is not null)
        {
            n = n._left;
            while (n._right is not null)
            {
                CheckIterationCount(ref iterationCount);
                n = n._right;
            }

            if (n._isPseudoNode) return null;
            return n;
        }

        // Otherwise, go up until we find a parent that we are a right child of:
        while (n is not null && !IsRightChild(n))
        {
            CheckIterationCount(ref iterationCount);
            n = n._parent;
        }

        // Return the parent, or null if we reached the root or the pseudo-node:
        Node? parent = n?._parent;
        if (parent?._isPseudoNode != false) return null;
        return parent;
    }

    // Prepares for BST deletion by preparing to remove using standard BST deletion logic and handling trivial recoloring cases.
    // Returns true if there's an extra black to fixup, false if not.
    private bool PrepareBSTDelete(ref Node n, out Node? prev, out Node? next)
    {
        // Get the nearby nodes:
        bool isRemovedNode;
        next = GetNextNode(n, out isRemovedNode);
        Debug.Assert(!isRemovedNode, "Node should be in tree.");
        prev = GetPrevNode(n, out isRemovedNode);
        Debug.Assert(!isRemovedNode, "Node should be in tree.");

        // Check if we have two children:
        if (n._left is not null && n._right is not null)
        {
            // Swap with the successor node:
            var successor = next;
            Debug.Assert(successor is not null, "Node with two children has no successor.");
            bool isNRightChild = IsRightChild(n);
            bool isSuccessorRightChild = IsRightChild(successor);
            bool successorIsDirectChild = successor._parent == n;
            n._subtreeSize = (successor._left?._subtreeSize ?? 0) + (successor._right?._subtreeSize ?? 0) + 1;
            successor._subtreeSize = n._left._subtreeSize + n._right._subtreeSize + 1;
            successor._left?._parent = n;
            successor._right?._parent = n;
            n._left._parent = successor;
            n._right._parent = successor;
            (n._parent, successor._parent) = (successor._parent, n._parent);
            (n._left, successor._left) = (successor._left, n._left);
            (n._right, successor._right) = (successor._right, n._right);
            (n._color, successor._color) = (successor._color, n._color);
            if (successorIsDirectChild) (n._parent, successor._right) = (successor, n);
            else if (n._parent is not null) (isSuccessorRightChild ? ref n._parent._right : ref n._parent._left) = n;
            else _root = n;
            if (successor._parent is not null) (isNRightChild ? ref successor._parent._right : ref successor._parent._left) = successor;
            else _root = successor;
        }

        // Handle 1 child case, and determine if we have an extra black to fixup:
        bool extraBlack = n._color == Node.Color.Black;
        Node? child = n._left ?? n._right;
        Debug.Assert(n._left is null || n._right is null, "Node has two children after swapping with successor.");
        if (child is not null)
        {
            // Rotate the node such that the child goes into its spot & it's a child of its current child:
            var parent = n._parent;
            if (parent == null) _root = child;
            else if (IsRightChild(n)) parent._right = child;
            else parent._left = child;
            child._parent = parent;
            FinishDestroyNode(n, prev, next);
            n = child;
            if (child is { _color: Node.Color.Red }) extraBlack = false;
        }

        // Ensure node is colored to black, as nil nodes are black:
        n._color = Node.Color.Black;

        // Return whether we have an extra black to fixup:
        return extraBlack;
    }

    // Finishes the BST deletion, which should be called after fixing up red-black properties:
    private void FinishBSTDelete(Node n, Node? prev, Node? next)
    {
        // Check we're not deleting the pseudo-node:
        Debug.Assert(!n._isPseudoNode, "Attempted to delete pseudo-node.");

        // Get the child (might be null):
        var parent = n._parent;
        Node? child = n._left ?? n._right;

        // Update parent to point to child & the child to point to parent:
        child?._parent = parent;
        if (parent is null) _root = child;
        else if (IsRightChild(n)) parent._right = child;
        else parent._left = child;

        // Destroy the node:
        FinishDestroyNode(n, prev, next);
    }

    // Finishes destroying a node by updating sizes, marking as removed, and cleaning up the internal node.
    private void FinishDestroyNode(Node n, Node? prev, Node? next)
    {
        // Update size:
        // Note: we need to use a memory barrier here, to ensure that the new size is visible by the time the lock exits, to threads that do not re-enter it;
        // otherwise, nothing stops the write from being re-ordered after the lock is released.
        _size--;
        Thread.MemoryBarrier();

        // Update subtree sizes up the tree (custom step - takes O(log n) time):
        var parent = n._parent;
        int iterationCount = 0;
        while (parent is not null)
        {
            CheckIterationCount(ref iterationCount);
            parent._subtreeSize--;
            parent = parent._parent;
        }

        // Mark node as removed for enumerators & clear references:
        n._color = Node.Color.Removed;
        n._left = prev;
        n._right = next;
        n._parent = null;

        // Note - we don't have to clean up the internal node, as it's only possible to get to this method from the method in that class that already.
        Debug.Assert((n.GetInternalNodeHelper()?._impl.Handle).GetValueOrDefault() == IntPtr.Zero, "Should only be called through InternalNode's deletion.");

        // Free node's reference to internal node finalizer helper:
        n._internalNodeHelper = null;
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    // Fixes up red-black tree properties just before a deletion that left an extra black.
    private void FixDelete(Node n)
    {
        // Loop until either the root is double black (which we can just recolor to black), or until we recolor to single black:
        int iterationCount = 0;
        while (n is { _color: Node.Color.Black, _parent: not null })
        {
            // Get sibling node:
            CheckIterationCount(ref iterationCount);
            var parent = n._parent;
            bool isRightChild = IsRightChild(n);
            var sibling = isRightChild ? parent._left : parent._right;
            Debug.Assert(sibling is { }, "Sibling should not be null during delete fixup.");

            // If sibling is red:
            if (sibling._color == Node.Color.Red)
            {
                // Recolor sibling to black, parent to red, rotate parent in direction of n, and continue:
                sibling._color = Node.Color.Black;
                parent._color = Node.Color.Red;
                if (isRightChild) RightRotate(parent);
                else LeftRotate(parent);
                isRightChild = IsRightChild(n);
                sibling = isRightChild ? parent._left : parent._right;
                Debug.Assert(sibling is { }, "Sibling should not be null during delete fixup.");
                Debug.Assert(parent == n._parent, "Parent changed unexpectedly during delete fixup.");
                if (sibling._color == Node.Color.Red) continue;
            }

            // Node's sibling is black.

            // If both of sibling's children are black:
            if (sibling._left is null or { _color: Node.Color.Black } && sibling._right is null or { _color: Node.Color.Black })
            {
                // Recolor sibling to red, move problem up the tree to parent:
                sibling._color = Node.Color.Red;
                n = parent;
            }
            else
            {
                // Get child of sibling that is the same direction as n and opposite:
                var sameDirNephew = isRightChild ? sibling._right : sibling._left;
                var oppositeDirNephew = isRightChild ? sibling._left : sibling._right;

                // If same dir nephew is red and opposite is black:
                if (sameDirNephew is { _color: Node.Color.Red } && oppositeDirNephew is null or { _color: Node.Color.Black })
                {
                    // Recolor same dir nephew to black, sibling to red, rotate sibling in opposite direction to n, update relevant nodes:
                    sameDirNephew._color = Node.Color.Black;
                    sibling._color = Node.Color.Red;
                    if (isRightChild) LeftRotate(sibling);
                    else RightRotate(sibling);
                    sibling = isRightChild ? parent._left : parent._right;
                    Debug.Assert(sibling is { }, "Sibling should not be null during delete fixup.");
                    oppositeDirNephew = isRightChild ? sibling._left : sibling._right;
                }

                // Now, opposite dir nephew must be red:
                Debug.Assert(oppositeDirNephew is { _color: Node.Color.Red }, "Opposite direction nephew is not red or is null during delete fixup.");

                // Recolor sibling to parent's color, parent to black, opposite dir nephew to black, rotate parent in direction of n:
                sibling._color = parent._color;
                parent._color = Node.Color.Black;
                oppositeDirNephew._color = Node.Color.Black;
                if (isRightChild) RightRotate(parent);
                else LeftRotate(parent);
                break;
            }
        }

        // Set color to single black:
        n._color = Node.Color.Black;
    }

    // The caller must hold the lock for the list when calling this and have already checked for disposal.
    // Note: the caller must guarantee that node is valid.
    private void DeleteHelper(Node node)
    {
        try
        {
            if (node._color == Node.Color.Removed) return; // Already removed.
            Node node2 = node;
            bool hasExtraBlack = PrepareBSTDelete(ref node2, out var prev, out var next);
            if (hasExtraBlack) FixDelete(node2);
            if (node == node2) FinishBSTDelete(node, prev, next);
            Check();
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
            if (currentNode is { _color: Node.Color.Removed })
            {
                if (!allowNearRemovedNode) return null;
                movedAlready = true;
                if (addBefore)
                {
                    do currentNode = currentNode._left;
                    while (currentNode is { _color: Node.Color.Removed });
                }
                else
                {
                    do currentNode = currentNode._right;
                    while (currentNode is { _color: Node.Color.Removed });
                }
            }

            // Enter lock & finish checking:
            using (EnterLock(out bool wasDisposed))
            {
                Throw.IfDisposed(wasDisposed, typeof(ConcurrentWeakList<T>));

                // Handle a removed node if needed by running the outer loop again:
                if (currentNode is { _color: Node.Color.Removed }) continue;

                // If we've already had to move due to a remove node, we want to add to the opposite side:
                if (movedAlready) addBefore = !addBefore;

                // Handle a node with a child on the side we want to add on:
                // Track original intent for null handling
                bool originalAddBefore = addBefore;
                if (addBefore)
                {
                    // Go to predecessor:
                    currentNode = GetPrevNode(currentNode, out bool isRemovedNode);
                    Debug.Assert(!isRemovedNode, "Node should be in tree.");
                    addBefore = false;
                }
                else
                {
                    // Go to successor:
                    currentNode = GetNextNode(currentNode, out bool isRemovedNode);
                    Debug.Assert(!isRemovedNode, "Node should be in tree.");
                    addBefore = true;
                }

                // If no node to add nearby:
                if (currentNode is null)
                {
                    // Add at start or end based on original intent:
                    // - Original addBefore=true means add before first → insert at 0
                    // - Original addBefore=false means add after last → insert at _size
                    return InsertAtHelper(value, originalAddBefore ? 0 : _size);
                }

                // If the child slot is already occupied, find the correct empty slot:
                // - If adding as left child but left is occupied, go to rightmost node in left subtree
                // - If adding as right child but right is occupied, go to leftmost node in right subtree
                if (addBefore && currentNode._left is not null)
                {
                    currentNode = currentNode._left;
                    int iterationCount = 0;
                    while (currentNode._right is not null)
                    {
                        CheckIterationCount(ref iterationCount);
                        currentNode = currentNode._right;
                    }

                    addBefore = false;
                }
                else if (!addBefore && currentNode._right is not null)
                {
                    currentNode = currentNode._right;
                    int iterationCount = 0;
                    while (currentNode._left is not null)
                    {
                        CheckIterationCount(ref iterationCount);
                        currentNode = currentNode._left;
                    }

                    addBefore = true;
                }

                // Add the node:
                return InsertManualHelper(value, currentNode, becomeLeftChild: addBefore);
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
                Throw.IfDisposed(wasDisposed, typeof(ConcurrentWeakList<T>));

                // Check if removed while we weren't holding the lock - we may as well make this somewhat atomic:
                if (current._color != Node.Color.Removed)
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
        // Note: we want other threads to be able to see it as soon as (in program order) we release the lock, even if they don't take it; hence the memory
        // barrier.
        var oldRoot = _root;
        _root = null;
        Thread.MemoryBarrier();

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
        ForgetNodeTree(oldRoot, 0);
        void ForgetNodeTree(Node n, int depth)
        {
            if (depth > 512) ThrowUnreachableExceptionForOverIterated();
            if (n._left is not null) ForgetNodeTree(n._left, depth + 1);
            if (n._right is not null) ForgetNodeTree(n._right, depth + 1);
            n._color = Node.Color.Removed;
            n._left = null;
            n._right = null;
            n._parent = null;

            // Finalizer is not critical here, other than our handles & marking removed, so clean those up and then suppress:
            if (n.GetInternalNodeHelper() is { } helper)
            {
                n._internalNode?.EarlyDispose(helper, _locker, true);
            }

            // Set node finalizer to null:
            n._internalNodeHelper = null;
        }

        // Suppress finalizer for this list now, as we've cleaned up everything:
        GC.SuppressFinalize(this);
    }

    // Helper method to check the integrity of the tree - only used in debug builds.
    partial void Check();
#if DEBUG
    partial void Check()
    {
        if (_root == null) return; // Disposed.

        // Check size makes sense:
        Debug.Assert(_size >= 0, "Size is negative.");

        // Check root is black:
        Debug.Assert(_root._color == Node.Color.Black, "Root node is not black.");

        // Check black descendent counts match & measures size matches claimed size:
        nint count = 0;
        Node? pseudoNode = null;
        BlackDescendentsCount(_root, ref count, ref pseudoNode, 0);
        Debug.Assert(count == _size, "Subtree size does not match actual size.");
        Debug.Assert(pseudoNode is not null, "Pseudo-node not found.");
        Debug.Assert(pseudoNode == GetNodeAtImpl(-1), "Node at index -1 is not the pseudo-node.");
    }

    private static int BlackDescendentsCount(Node n, ref nint count, ref Node? pseudoNode, int depth)
    {
        var l = n._left;
        var r = n._right;

        // Check depth makes sense:
        Debug.Assert(depth < 512, "Depth is way larger than should be possible (probably recursive definition).");

        // If red, both children must be black:
        if (n._color == Node.Color.Red)
        {
            Debug.Assert((l is null) || (l._color == Node.Color.Black), "Red node has red left child.");
            Debug.Assert((r is null) || (r._color == Node.Color.Black), "Red node has red right child.");
        }

        // Check that color is valid:
        Debug.Assert(n._color == Node.Color.Black || n._color == Node.Color.Red, "Node has invalid color.");

        // Check amount of black nodes in paths to descendents match:
        int lCount = l is not null ? BlackDescendentsCount(l, ref count, ref pseudoNode, depth + 1) : 0;
        int rCount = r is not null ? BlackDescendentsCount(r, ref count, ref pseudoNode, depth + 1) : 0;
        Debug.Assert(lCount == rCount, "Black descendent counts do not match.");

        // Check the left & right children's parent pointers:
        if (l is not null) Debug.Assert(l._parent == n, "Left child's parent pointer is incorrect.");
        if (r is not null) Debug.Assert(r._parent == n, "Right child's parent pointer is incorrect.");

        // If pseudo-node, ensure we only see one:
        if (n._isPseudoNode)
        {
            Debug.Assert(pseudoNode is null, "Multiple pseudo-nodes found.");
            pseudoNode = n;
        }
        else
        {
            count++;
        }

        Debug.Assert(n._subtreeSize > 0, "Node has subtree size of 0.");

        // Check subtree size:
        nint expectedSize = 1 + (l?._subtreeSize ?? 0) + (r?._subtreeSize ?? 0);
        Debug.Assert(n._subtreeSize == expectedSize, "Node subtree size is incorrect.");

        // Return black descendent count for this node, including self if black:
        return lCount + (n._color == Node.Color.Black ? 1 : 0);
    }
#endif
}
