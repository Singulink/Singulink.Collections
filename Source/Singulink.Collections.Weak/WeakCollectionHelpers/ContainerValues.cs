using System.Diagnostics;
using System.Runtime.CompilerServices;

// NOTE: for correct usage, ensure you follow what WeakList does.
// NOTE: only the constructor and CleanOut method are intended to be used outside of the namespace.

namespace Singulink.Collections.WeakCollectionHelpers;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable IDE0028 // Simplify collection initialization

// Per-container state embedded inside the concrete container type.
internal struct ContainerValues<T, TNode, TContainer, TNodeHelpers>
    where T : class
    where TNode : class
    where TContainer : class
    where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
{
#if !NET
    // No DependentHandle type on .NET Standard, so we store the values in a CWT instead:
    // IMPORTANT: InternalNodeFinalizeHelper must not hold a strong reference to the CWT or WeakList, otherwise it will leak
    // due to https://github.com/dotnet/runtime/issues/12255.
    // NOTE: uses of the linked lists are expected to lock on the list. This is important, since otherwise we can run into race conditions. E.g., if we just
    // checked it's empty, we will want to remove it from the cwt, but it may have become used again between when we checked it and when we tried to remove it.
    // We can either lock on the container to achieve this, or we can just lock on the linked list itself (needs to be consistent though).
    internal ConditionalWeakTable<T, LinkedList<InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>>>? _cwt = new();
#endif

    // NOTE!!! For correctness, it's crucial that no finalizer accesses any managed values except through weak references, as otherwise they may be partially
    // null-ed out already by the time the finalizer runs, leading to bugs - therefore, we carefully ensure we do all of that through weak references, while
    // still ensuring that the finalizers can run & collect everything.
    // We use this side-data structure on .NET (not standard) to allow us to still clean up nodes when the collection is collected.
    // The way it works is that the collection hold a strong ref to the list, and so does the internal node, but the helper only holds it as weak. That way,
    // while the collection is alive, it can modify the list, but once it's collected, the helper can find any InternalNodes that are still alive (if any),
    // since they hold also hold a strong ref to the list; but it does not need to hold a strong reference to the linked list, which would be problematic.
#if NET
    internal readonly InternalNodeTrackingInfo<T, TNode, TContainer, TNodeHelpers> _internalNodes;
    internal readonly CleanupHelper<T, TNode, TContainer, TNodeHelpers> _cleanupHelper;
#endif

    public ContainerValues()
    {
#if NET
        _internalNodes = new([], new());
        _cleanupHelper = new(WeakHandle.Alloc(_internalNodes));
#endif
    }

    // Note: we require the caller to be holding the lock (or for no new allocations to occur concurrently otherwise) for this method to be safe.
    // Note: this method is not possible to use safely on non-locking collections.
    internal void CleanOut()
    {
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
        Debug.Assert(_cwt is not null, "CWT should not be null here.");
        _cwt.Clear();
#endif
        GC.KeepAlive(_cwt); // Ensure the CWT lives to here at least.
        _cwt = null;
#endif
    }
}
