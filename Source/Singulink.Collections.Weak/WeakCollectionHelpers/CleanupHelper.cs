namespace Singulink.Collections.WeakCollectionHelpers;

// NOTE: this class is only intended to be used within the implementation of this namespace.

#pragma warning disable SA1401 // Fields should be private

#if NET
internal sealed class CleanupHelper<T, TNode, TContainer, TNodeHelpers>(WeakHandle listRef)
    where T : class
    where TNode : class
    where TContainer : class
    where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
{
    internal WeakHandle ListRef = listRef; // Type of value is InternalNodeTrackingInfo.

    ~CleanupHelper()
    {
        if (ListRef.TryGetTarget<InternalNodeTrackingInfo<T, TNode, TContainer, TNodeHelpers>>() is { } list)
        {
            // Note: we only need the lock on non-locking collections. On locking collections, we already know that this cannot run concurrently with new node
            // allocations nor with removals, since they require the lock, which keeps the collection alive.
            bool entered = !default(TNodeHelpers).HasLocker;
            if (entered) Monitor.Enter(list);
            try
            {
                foreach (var handle in list.List)
                {
                    if (WeakReferenceHelpers.TryGetValue(handle) is { } node &&
                        node._impl.GetTarget<InternalNode<T, TNode, TContainer, TNodeHelpers>>() is { } n)
                    {
                        n.EarlyDispose(node, null, isDisposed: true);
                    }
                }
            }
            finally
            {
                if (entered) Monitor.Exit(list);
            }

            GC.KeepAlive(list);
        }

        ListRef.Dispose();
    }
}
#endif
