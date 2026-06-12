using System.Diagnostics;

// NOTE: this class is only intended to be used within the implementation of this namespace.

namespace Singulink.Collections.WeakCollectionHelpers;

#pragma warning disable SA1401 // Fields should be private

internal sealed class InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>
    where T : class
    where TNode : class
    where TContainer : class
    where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
{
    // Note: we cannot store the list as a strong reference in this type with the current implementation - the Node stores such a reference.
    // Our reference to the list - note, it's important that this is a weak reference as we directly reference it from InternalNode, then we will leak the
    // whole list due to https://github.com/dotnet/runtime/issues/12255.
    internal StrongHandle _impl; // The type of value is InternalNode.

    ~InternalNodeFinalizeHelper()
    {
        // Get the node to remove, or discover if we've already been disposed:
        var impl = new StrongHandle(Interlocked.Exchange(ref _impl.Handle, IntPtr.Zero));
        if (impl.Handle == IntPtr.Zero)
        {
            Debug.Fail("InternalNodeFinalizeHelper finalizer invoked, but was already disposed.");
        }
        else if (!impl.GetNotNullTarget<InternalNode<T, TNode, TContainer, TNodeHelpers>>().Dispose(disposing: false, null, ref _impl, impl))
        {
            GC.ReRegisterForFinalize(this);
        }
    }
}
