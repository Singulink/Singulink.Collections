using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.Collections.Utilities;
using Singulink.Collections.WeakCollectionHelpers;

namespace Singulink.Collections;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

/// <content>
/// Contains the implementation of the weak collection helper stuff for <see cref="WeakValueDictionary{TKey, TValue}"/>.
/// </content>
partial class WeakValueDictionary<TKey, TValue>
{
    internal sealed class Node
    {
        // Our callbacks for NodeState to use
        internal readonly struct NodeHelpers : INodeHelpers<TValue, Node, WeakValueDictionary<TKey, TValue>, NodeHelpers>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref NodeState<TValue, Node, WeakValueDictionary<TKey, TValue>, NodeHelpers> GetNodeState(Node node) => ref node._impl;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DeleteHelper(WeakValueDictionary<TKey, TValue> container, Node node) => container.DeleteHelper(node);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsDisposed(WeakValueDictionary<TKey, TValue> container) => container._disableAllocations;

            public bool HasLocker
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref bool GetDisableAllocations(WeakValueDictionary<TKey, TValue> container) => ref container._disableAllocations;

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ThrowDisposed() => Throw.IfDisposed(true, typeof(WeakValueDictionary<TKey, TValue>));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref LinkedList<Node>? GetNodeHelperList(WeakValueDictionary<TKey, TValue> container) => ref container._nodeHelperList;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref LinkedListNode<Node>? GetNodeHelperNode(Node node) => ref node._nodeHelperNode;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref ContainerValues<TValue, Node, WeakValueDictionary<TKey, TValue>, NodeHelpers> GetContainerValues(
                WeakValueDictionary<TKey, TValue> container)
            {
                return ref container._containerValues;
            }

            public Lock GetLocker(WeakValueDictionary<TKey, TValue> container) => throw new NotSupportedException("Only supported on locking collections.");
        }

        // Node state:
        private NodeState<TValue, Node, WeakValueDictionary<TKey, TValue>, NodeHelpers> _impl;

        // Key
        public TKey Key { get; }

        // Value
        public WeakReference<TValue> Value { get; }

        // Field for use by WeakCollectionHelpers:
        private LinkedListNode<Node>? _nodeHelperNode;

        // Private constructor:
        internal Node(
            TKey key,
            TValue value,
            InternalNode<TValue, Node, WeakValueDictionary<TKey, TValue>, NodeHelpers> internalNode,
            WeakValueDictionary<TKey, TValue> dictionary)
        {
            Key = key;
            Value = new(value);
            _impl = new(internalNode, dictionary);
            _impl.Alloc(value, this, internalNode, dictionary);
        }

        // Helper properties and methods that just wrap the ones on NodeState:
        public void Dispose() => _impl.Dispose(this);
    }

    // Handle failure in a way that reduces the risk of something being able to go wrong.
    private void HandleFailure()
    {
        // Mark disposed:
        Dispose();
        Thread.MemoryBarrier();
    }

    // Helper to allocate a node - doesn't link it into the dictionary.
    // Note: callers must GC.KeepAlive the value until after it is fully added in.
    // Note: callers must have already checked for disposal.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Node AllocNode(TKey key, TValue value)
    {
        return new(key, value, new(), this);
    }

    // Helper for deleting a node from the dictionary from the weak finalizer.
    private void DeleteHelper(Node node)
    {
        _lookup?.TryRemove(new KeyValuePair<TKey, Node>(node.Key, node));
    }

    // Fields for use by WeakCollectionHelpers:
    private bool _disableAllocations;
    private LinkedList<Node>? _nodeHelperList;
}
