namespace Singulink.Collections.WeakCollectionHelpers;

// NOTE: this class is only intended to be used within the implementation of this namespace.

#pragma warning disable SA1401 // Fields should be private

#if NET
internal sealed class InternalNodeTrackingInfo<T, TNode, TContainer, TNodeHelpers>
    (LinkedList<WeakReference<InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>>> list)
    where T : class
    where TNode : class
    where TContainer : class
    where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
{
    // Note: on non-locking collections, we lock on InternalNodeTrackingInfo currently - this avoids an unnecessary field on InternalNodeTrackingInfo, without
    // adding extra complication to the generic logic to avoid the unnecessary field (as on locking collections, we don't need this field).
    internal LinkedList<WeakReference<InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>>> List = list;
}
#endif
