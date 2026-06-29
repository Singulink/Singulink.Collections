using System.Diagnostics;
using System.Runtime.CompilerServices;

// NOTE: for correct usage, ensure you follow what WeakList does.
// NOTE: only the APIs marked public are intended to be used outside of the namespace.

namespace Singulink.Collections.WeakCollectionHelpers;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable IDE0028 // Simplify collection initialization

// Per-container state embedded inside the concrete container type.
// Note: the difference between a locking and non-locking collection is that the locking one has its own lock and holds it for the duration of all underlying
// collection adjustments and all calls into this namespace (that require locking).
internal struct ContainerValues<T, TNode, TContainer, TNodeHelpers>
    where T : class
    where TNode : class
    where TContainer : class
    where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
{
#if !NET
    // No DependentHandle type on .NET Standard, so we store the values in a CWT instead:
    // IMPORTANT: InternalNodeFinalizeHelper must not hold a strong reference to the CWT or the container, otherwise it will leak
    // due to https://github.com/dotnet/runtime/issues/12255.
    internal ConditionalWeakTableListWrapper<T, InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>> _cwt = new();
#endif

    // NOTE!!! For correctness, it's crucial that no finalizer accesses any managed values except through weak references, as otherwise they may be partially
    // null-ed out already by the time the finalizer runs, leading to bugs - therefore, we carefully ensure we do all of that through weak references, while
    // still ensuring that the finalizers can run & collect everything.
    // We use this side-data structure on .NET (not standard) to allow us to still clean up nodes when the container is collected.
    // The way it works is that the container holds a strong ref to the list, and so does the internal node, but the helper only holds it as weak. That way,
    // while the container is alive, it can modify the container, but once it's collected, the helper can find any InternalNodes that are still alive (if any),
    // since they hold also hold a strong ref to the list; but it does not need to hold a strong reference to the linked list, which would be problematic.
#if NET
    internal readonly InternalNodeTrackingInfo<T, TNode, TContainer, TNodeHelpers> _internalNodes;
    internal readonly CleanupHelper<T, TNode, TContainer, TNodeHelpers> _cleanupHelper;
#endif

    public ContainerValues(TContainer container)
    {
#if NET
        _internalNodes = new([]);
        _cleanupHelper = new(WeakHandle.Alloc(_internalNodes));
#endif
        if (!default(TNodeHelpers).HasLocker)
        {
            default(TNodeHelpers).GetDisableAllocations(container) = false;
            default(TNodeHelpers).GetNodeHelperList(container) = [];
        }
    }

    // Note: we require the caller to be holding the lock (or for no new allocations/removals to occur concurrently otherwise) for this method to be safe.
    // Note: this method is not possible to use safely on non-locking collections.
    public void CleanOut()
    {
        // Clean out resources:
#if NET
        _internalNodes.List.Clear();
        _cleanupHelper.ListRef.Dispose();
        GC.SuppressFinalize(_cleanupHelper);
        GC.KeepAlive(_cleanupHelper);
        GC.KeepAlive(_internalNodes);
#else
        _cwt.Clear();
        GC.KeepAlive(_cwt); // Ensure the CWT lives to here at least.
#endif
    }

    // This method is the implementation for the dispose logic for non-locking collections (only).
    public void Dispose(TContainer container)
    {
        // Firstly, we need to acquire the lock and ensure no new nodes are being allocated:
#if NET
        lock (_internalNodes.Locker)
#else
        var cwt = _cwt;
        lock (default(TNodeHelpers).GetAllocationLock(container))
#endif
        {
            // Check if we're already disposed:
            ref bool disableAllocations = ref default(TNodeHelpers).GetDisableAllocations(container);

            if (disableAllocations)
            {
                return;
            }

            // Mark as disposing
            disableAllocations = true;
        }

        // Now, get the list of nodes we still have and set it to null (do this inside the lock so we can allow the existing cleanups to work as expected):
        ref var listField = ref default(TNodeHelpers).GetNodeHelperList(container);
        var list = listField;
        Debug.Assert(list != null, "List should not be null here.");
        lock (default(TNodeHelpers).GetNodeHelperListLock(container))
        {
            // Mark as null - this allows any other cleanups to just skip this step immediately.
            listField = null;
        }

        // Clean out resources:
#if NET
        lock (_internalNodes.Locker) _internalNodes.List.Clear();
        _cleanupHelper.ListRef.Dispose();
        GC.SuppressFinalize(_cleanupHelper);
        GC.KeepAlive(_cleanupHelper);
        GC.KeepAlive(_internalNodes);
#else
        _cwt.Clear();
        GC.KeepAlive(_cwt); // Ensure the CWT lives to here at least.
#endif

        // Dispose all nodes that may still be alive (note: it is critical that no other threads access this list for mutation anymore):
        foreach (var node in list)
        {
            // Again, we call CleanUpForHandleFailureOrDispose as an implementation detail.
            default(TNodeHelpers).GetNodeState(node).CleanUpForHandleFailureOrDispose();
        }

        // Suppress finalizer for this collection now (if it has one - otherwise, this does nothing), as we've cleaned up everything:
        GC.SuppressFinalize(container);
        GC.KeepAlive(container);
    }
}
