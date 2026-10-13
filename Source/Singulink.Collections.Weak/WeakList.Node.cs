using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;

using Singulink.Collections.WeakCollectionHelpers;

namespace Singulink.Collections;

#pragma warning disable RCS1043 // Remove 'partial' modifier from type with a single part
#pragma warning disable SA1401 // Fields should be private

/// <content>
/// Contains the <see cref="Node"/> nested type for <see cref="WeakList{T}"/>.
/// </content>
public sealed partial class WeakList<T>
{
    /// <summary>
    /// Represents a node in a <see cref="WeakList{T}"/>, which identifies the position of a value in the list.
    /// </summary>
    /// <remarks>
    /// <para>Holding a reference to a node does not prevent its value from being garbage collected. Nodes can be used to remove a value from the list in O(1)
    /// time, to add values relative to its position, or to start an enumeration from its position.</para>
    /// </remarks>
    public sealed partial class Node : IDisposable
    {
        // Our callbacks for NodeState to use
        internal readonly struct NodeHelpers : INodeHelpers<T, Node, WeakList<T>, NodeHelpers>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref NodeState<T, Node, WeakList<T>, NodeHelpers> GetNodeState(Node node) => ref node._impl;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DeleteHelper(WeakList<T> container, Node node) => container.DeleteHelper(node);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsDisposed(WeakList<T> container) => container._head is null;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref ContainerValues<T, Node, WeakList<T>, NodeHelpers> GetContainerValues(WeakList<T> container) => ref container._containerValues;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Lock GetLocker(WeakList<T> container) => container._locker;

            public bool HasLocker
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => true;
            }

            [DoesNotReturn]
            public void ThrowDisposed() => throw new NotSupportedException("Only supported on non-locking collections.");
            public ref bool GetDisableAllocations(WeakList<T> container) => throw new NotSupportedException("Only supported on non-locking collections.");
            public ref LinkedList<Node>? GetNodeHelperList(WeakList<T> container) => throw new NotSupportedException("Only supported on non-locking collections.");
            public ref LinkedListNode<Node>? GetNodeHelperNode(Node node) => throw new NotSupportedException("Only supported on non-locking collections.");
            public Lock GetNodeHelperListLock(WeakList<T> container) => throw new NotSupportedException("Only supported on non-locking collections.");
#if !NET
            public Lock GetAllocationLock(WeakList<T> container) => throw new NotSupportedException("Only supported on non-locking collections.");
#endif
        }

        // Node state:
        internal NodeState<T, Node, WeakList<T>, NodeHelpers> _impl;

        // Our doubly linked list state:
        internal Node? _prev;
        internal Node? _next;
        internal bool _isPseudoNode;
        internal ulong _version;  // The version of the list when this node was added.

        // Used to mark nodes that have been removed but are still alive for enumerations.
        // When set, the _next node is set to the next node in the list, so that we can continue enumerating.
        // Also, _prev is set to the node that was previously ordered before this one, so that we can go backwards.
        internal bool _isRemoved;

        // Private constructor:
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Node((T Value, InternalNode<T, Node, WeakList<T>, NodeHelpers> InternalNode)? state, WeakList<T> list)
        {
            if (state is { } s)
            {
                _impl = new(s.InternalNode, list);
                _impl.Alloc(s.Value, this, s.InternalNode, list);
            }
            else
            {
                _impl = new(null, list);
            }
        }

        /// <summary>
        /// Gets the list that this node belongs to. The list is still returned after the node has been removed from it.
        /// </summary>
        public WeakList<T> List => _impl.Container;
        internal WeakList<T> ListDirect => _impl.ContainerDirect;

        /// <summary>
        /// Gets the value of this node, or <see langword="null"/> if the value has been garbage collected.
        /// </summary>
        /// <remarks>
        /// The value can be <see langword="null"/> before the node has been removed from the list, since removal happens on the finalizer thread shortly after
        /// the value is collected. Do not use this property to decide whether to call <see cref="Dispose()"/>; disposing a node is always safe.
        /// </remarks>
        public T? Value => _impl.Value;

        /// <summary>
        /// Gets the version of the list at the time this node was added. See <see cref="WeakList{T}.Version"/>.
        /// </summary>
        public ListVersion Version => new(_version);

        /// <summary>
        /// Gets a value indicating whether this node has been removed from the list, either explicitly or because its value was garbage collected.
        /// </summary>
        public bool IsRemoved
        {
            get
            {
                // Note: when the lock is held, it is enough to just check the _isRemoved flag, but otherwise checking _finalizeAttemptCount is more up-to-date.
                return _impl.IsRemoved;
            }
        }

        /// <summary>
        /// Removes this node from the list. Does nothing if the node has already been removed.
        /// </summary>
        public void Dispose() => _impl.Dispose(this);

#if NET
        /// <summary>
        /// Replaces the value of this node with the specified value without changing its position in the list.
        /// </summary>
        /// <param name="newTarget">The new value for the node.</param>
        /// <returns><see langword="true"/> if the value was replaced; <see langword="false"/> if the node has been removed from the list or the list has been
        /// disposed.</returns>
        /// <remarks>
        /// This method is only available on .NET, since it depends on <see cref="DependentHandle"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="newTarget"/> is <see langword="null"/>.</exception>
        public bool TryUpdateTarget(T newTarget) => _impl.TryUpdateTarget(newTarget);
#endif

        // The method to call to clean out the node for 'HandleFailureOrDispose' methods.
        internal void CleanUpForHandleFailureOrDispose() => _impl.CleanUpForHandleFailureOrDispose();
    }
}
