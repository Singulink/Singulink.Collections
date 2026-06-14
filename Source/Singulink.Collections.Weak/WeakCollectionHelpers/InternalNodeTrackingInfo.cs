namespace Singulink.Collections.WeakCollectionHelpers;

// NOTE: this class is only intended to be used within the implementation of this namespace.

#pragma warning disable SA1401 // Fields should be private

#if NET
internal sealed class InternalNodeTrackingInfo<T, TNode, TContainer, TNodeHelpers>
    (LinkedList<WeakReference<InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>>> list, Lock locker)
    where T : class
    where TNode : class
    where TContainer : class
    where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
{
    internal LinkedList<WeakReference<InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>>> List = list;
    internal Lock Locker = locker;
}
#endif
