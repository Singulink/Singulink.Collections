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
            public ref NodeState<TValue, Node, WeakValueDictionary<TKey, TValue>, NodeHelpers> GetNodeState(Node node) => ref node._impl;
            public void DeleteHelper(WeakValueDictionary<TKey, TValue> container, Node node) => container.DeleteHelper(node);
            public bool IsDisposed(WeakValueDictionary<TKey, TValue> container) => Volatile.Read(ref container._lookup) is null;
            public Lock GetLocker(WeakValueDictionary<TKey, TValue> container) => throw new NotImplementedException();
            public bool HasLocker => false;

            public ref ContainerValues<TValue, Node, WeakValueDictionary<TKey, TValue>, NodeHelpers> GetContainerValues(
                WeakValueDictionary<TKey, TValue> container)
            {
                return ref container._containerValues;
            }
        }

        // Node state:
        private NodeState<TValue, Node, WeakValueDictionary<TKey, TValue>, NodeHelpers> _impl;

        // Key
        public TKey Key { get; }

        // Value
        public WeakReference<TValue> Value { get; }

        // Private constructor:
        internal Node(
            TKey key,
            TValue value,
            InternalNode<TValue, Node, WeakValueDictionary<TKey, TValue>, NodeHelpers> internalNode,
            WeakValueDictionary<TKey, TValue> list)
        {
            Key = key;
            Value = new(value);
            _impl = new(internalNode, list);
            _impl.Alloc(value, this, internalNode, ref list._containerValues);
        }

        // Helper properties and methods that just wrap the ones on NodeState:
        public void Dispose() => _impl.Dispose(this);
    }

    // Handle failure in a way that reduces the risk of something being able to go wrong.
    private void HandleFailure()
    {
        // Mark disposed:
        _lookup = null;
        Thread.MemoryBarrier();

        // Suppress finalizer for this list now, as we've cleaned up everything:
        GC.SuppressFinalize(this);
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
}
