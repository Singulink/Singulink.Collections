using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

using Singulink.Collections.WeakCollectionHelpers;

namespace Singulink.Collections;

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable RCS1043 // Remove 'partial' modifier from type with a single part

/// <content>
/// Contains the <see cref="Node"/> nested type for <see cref="WeakList{T}"/> type.
/// </content>
public sealed partial class WeakList<T>
{
    /// <summary>
    /// Represents a node in a <see cref="WeakList{T}" />.
    /// </summary>
    /// <remarks>
    /// Holding a strong reference to a <see cref="Node" /> does not prevent the value it references from being garbage collected.
    /// </remarks>
    public sealed partial class Node : IDisposable
    {
        // Our callbacks for NodeState to use
        internal struct NodeHelpers : INodeHelpers<T, Node, WeakList<T>, NodeHelpers>
        {
            public ref NodeState<T, Node, WeakList<T>, NodeHelpers> GetNodeState(Node node) => ref node._impl;
            public void DeleteHelper(WeakList<T> container, Node node) => container.DeleteHelper(node);
            public bool IsDisposed(WeakList<T> container) => container._head is null;
            public ref ContainerValues<T, Node, WeakList<T>, NodeHelpers> GetContainerValues(WeakList<T> container) => ref container._containerValues;
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
                _impl.Alloc(s.Value, this, s.InternalNode, ref list._containerValues);
            }
            else
            {
                _impl = new(null, list);
            }
        }

        /// <summary>
        /// Gets the list that this node belongs to, or used to belong to.
        /// </summary>
        public WeakList<T> List => _impl.Container;
        internal WeakList<T> ListDirect => _impl.ContainerDirect;

        /// <summary>
        /// Gets the target value of this node if it is still available, otherwise <see langword="null" />.
        /// </summary>
        /// <remarks>
        /// This method can return <see langword="null" /> even if the node has not yet been removed from the list, so don't try to use this to optimize
        /// avoiding calling <see cref="Dispose()" /> unnecessarily; it's better to just call it unconditionally.
        /// </remarks>
        public T? Value => _impl.Value;

        /// <summary>
        /// Gets the version of the list when this node was added.
        /// </summary>
        public ListVersion Version => new(_version);

        /// <summary>
        /// Gets a value indicating whether this node has been removed from the list.
        /// </summary>
        public bool IsRemoved => _impl.IsRemoved;

        /// <summary>
        /// Disposes the node, removing it from the <see cref="WeakList{T}" /> it belongs to.
        /// </summary>
        public void Dispose() => _impl.Dispose(this);

#if NET
        /// <summary>
        /// Try to update the target of this node to a new value.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the value is null.</exception>
        /// <remarks>
        /// This method is only supported on frameworks that have <see cref="DependentHandle" />.
        /// </remarks>
        public bool TryUpdateTarget(T newTarget) => _impl.TryUpdateTarget(newTarget);
#endif

        // The method to call to clean out the node for 'HandleFailureOrDispose' methods.
        internal void CleanUpForHandleFailureOrDispose(ref ContainerValues<T, Node, WeakList<T>, NodeHelpers> containerValues) => _impl.CleanUpForHandleFailureOrDispose(ref containerValues);
    }
}
