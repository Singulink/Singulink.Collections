using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

// NOTE: this class is only intended to be used within the implementation of this namespace.

namespace Singulink.Collections.WeakCollectionHelpers;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable SA1401 // Fields should be private

// This part is spun out of Node to ensure that the list isn't kept alive by InternalNodeFinalizeHelper, and it's spun off from InternalNodeFinalizeHelper
// to ensure we can dispose & access it at any time (including before InternalNodeFinalizeHelper is disposed).
internal sealed class InternalNode<T, TNode, TContainer, TNodeHelpers>
    where T : class
    where TNode : class
    where TContainer : class
    where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
{
    // Keeps track of how many times we've attempted to finalize:
    // Note: we use this to avoid trying to wait a long time in the finalizer for the lock if it has contention - it is not required for correctness.
    // Note: we set this to -1 when it's been removed.
    internal int _finalizeAttemptCount;

    // A weak handle to the node.
    internal WeakHandle _node; // Type of value is Node.

#if !NET
    // No DependentHandle type on .NET Standard, so we just use a normal WeakReference in here & use a CWT as backing store, and keep track of the node
    // that keeps this instance alive so that we can remove it if we dispose.
    internal WeakHandle _cwtNode; // Type of value is LinkedListNode<InternalNodeFinalizeHelper>.

    // Store the value (note: we have to use a weak reference, as it could contain a ref back to the list or Node):
    // Note: we use WeakHandle here to avoid needing to allocate a separate WeakReference object - otherwise it'd be WeakReference<T>?.
    // Note: we don't need this value on .NET, since we have it in the DependentHandle.
    internal WeakHandle _value; // Type of value is T.
#else
    // We use DependentHandle to keep InternalNodeFinalizeHelper alive at least as long as the value.
    internal DependentHandle _dependentHandle;

    // On .NET we want to keep the InternalNodeTrackingInfo alive if this is alive (including during finalization).
    // We use a StrongHandle here to prevent the GC from collecting the tracking info before InternalNode's finalizer runs.
    // But, we cannot access via that, so we also store a weak ref to the node we want to remove here & only use that in the finalizer.
    internal WeakHandle _finalizeHelperNode; // Type of value is LinkedListNode<WeakReference<InternalNodeFinalizeHelper>>.
    internal StrongHandle _trackingInfoHandle; // Type of value is InternalNodeTrackingInfo.

    private void RemoveFinalizeHelper()
    {
        if (_finalizeHelperNode.TryGetTarget<LinkedListNode<WeakReference<InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>>>>() is { } finalizeHelperNode)
        {
            finalizeHelperNode.List?.Remove(finalizeHelperNode);
            GC.KeepAlive(finalizeHelperNode);
        }

        _finalizeHelperNode.Dispose();
    }
#endif

    // Removes the node from the container's backing collection under the lock and disposes the per-node handles. Returns false (without disposing
    // anything) if the lock was contended and the caller should retry later; the retry budget is tracked by _finalizeAttemptCount.
    private bool RemoveFromContainer(TNode node, ref StrongHandle implHandle, StrongHandle original, bool disposing)
    {
        // Try to enter the lock now:
        var container = default(TNodeHelpers).GetNodeState(node)._container;
        bool entered = true;
        bool wasDisposed = default(TNodeHelpers).HasLocker ? default(TNodeHelpers).IsDisposed(container) : false;
        using var scope = default(TNodeHelpers).HasLocker
            ? (_finalizeAttemptCount < 5 && !disposing)
                ? LockScope.TryEnterLock<T, TNode, TContainer, TNodeHelpers>(container, out wasDisposed, out entered)
                : LockScope.EnterLock<T, TNode, TContainer, TNodeHelpers>(container, out wasDisposed)
            : default;
        if (!wasDisposed)
        {
            if (entered)
            {
                _finalizeAttemptCount = -1;
                try
                {
                    default(TNodeHelpers).DeleteHelper(container, node);

                    // Free node's reference to internal node finalizer helper:
                    default(TNodeHelpers).GetNodeState(node)._internalNodeHelper = null;
                }
                finally
                {
#if NET
                    // Dispose the finalizer helper node, if it's still alive:
                    RemoveFinalizeHelper();

                    // It is important that we try to dispose the value or dependent handle while we hold the lock, so do that here:
                    _dependentHandle.Dispose();
                    _trackingInfoHandle.Dispose();
#else
                    // Capture the value (through its weak handle) before disposing it; if it is still alive we use it to evict the CWT entry below.
                    var value = _value.TryGetTarget<T>();
                    _value.Dispose();

                    // Ensure removed from CWT tracking stuff (the remove logic might have missed it, since we're not necessarily alive anymore, so it
                    // might not be able to look up these lists):
                    if (_cwtNode.TryGetTarget<LinkedListNode<InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>>>() is { } cwtNode)
                    {
                        // Note: removing the node nulls cwtNode.List, so capture the per-value list first.
                        var perValueList = cwtNode.List;
                        if (perValueList != null)
                        {
                            // We need to lock on the linked list here, if we aren't already holding the container lock.
                            bool entered2 = !default(TNodeHelpers).HasLocker;
                            if (entered2) Monitor.Enter(perValueList);
                            try
                            {
                                perValueList.Remove(cwtNode);

                                // If the value is still alive and this was its last node, evict the now-empty per-value entry from the CWT so it does not linger
                                // until the value is collected (it survives Clear() / Remove() otherwise).
                                // Note: if the value is already dead, the CWT entry will already automatically remove itself at some point.
                                if (value is not null && perValueList is { Count: 0 })
                                {
                                    var cwt = default(TNodeHelpers).GetContainerValues(container)._cwt;
                                    if (cwt != null && cwt.TryGetValue(value, out var actualList) && actualList == perValueList) cwt.Remove(value);
                                }
                            }
                            finally
                            {
                                if (entered2) Monitor.Exit(perValueList);
                            }
                        }
                    }

                    _cwtNode.Dispose();
#endif
                }
            }
            else
            {
                // Let's just try again later as it might not be contested then (up to 5 times):
                _finalizeAttemptCount++;
                implHandle.Handle = original.Handle;
                Thread.MemoryBarrier(); // Ensure the handle gets updated before we exit the method (we don't hold the lock here).
                return false;
            }
        }
        else
        {
            // Mark as removed even though list is disposed - callers may still check IsRemoved:
            _finalizeAttemptCount = -1;

            // Dispose these in here, as we don't pass out "wasDisposed" to callers, which means they can't check for that (which is fine to leave to
            // dispose there, but they can't really check for that):
#if NET
            _dependentHandle.Dispose();
            _finalizeHelperNode.Dispose();
            _trackingInfoHandle.Dispose();
#else
            _value.Dispose();
            _cwtNode.Dispose();
#endif
        }

        // Success:
        return true;
    }

    // Core teardown for a node: looks up the node (if not supplied), removes it from its container, and releases every handle this instance owns.
    // Returns false if the removal couldn't acquire the lock and should be retried (the finalizer re-registers in that case); otherwise true.
    internal bool Dispose(bool disposing, TNode? node, ref StrongHandle implHandle, StrongHandle previousHandle)
    {
        bool releaseHandle = true;
        bool hasNodeValue = false;
        try
        {
            // Try to get the node if we don't have it yet:
            node ??= _node.TryGetTarget<TNode>();

            // If we don't have the node, we can't call RemoveFromContainer, so we just finalize our unmanaged resources:
            if (node is null) return true;

            // Remove the node from the list:
            hasNodeValue = true;
            if (!RemoveFromContainer(node, ref implHandle, previousHandle, disposing))
            {
                releaseHandle = false;
                return false;
            }
        }
        finally
        {
            if (releaseHandle)
            {
#if NET
                Debug.Assert(
                    !(hasNodeValue
                        && (_dependentHandle.IsAllocated || _finalizeHelperNode.Handle != IntPtr.Zero || _trackingInfoHandle.Handle != IntPtr.Zero)),
                    "These values should already have been disposed in RemoveFromContainer.");
                _dependentHandle.Dispose();
                _finalizeHelperNode.Dispose();
                _trackingInfoHandle.Dispose();
#else
                Debug.Assert(
                    !(hasNodeValue && (_value.Handle != IntPtr.Zero || _cwtNode.Handle != IntPtr.Zero)),
                    "These values should already have been disposed in RemoveFromContainer.");
                _value.Dispose();
                _cwtNode.Dispose();
#endif
                previousHandle.Dispose();
                _node.Dispose();
            }
        }

        return true;
    }

    // Helper for all the places where we want to quickly dispose the internal node from the list (e.g., for disposing the list):
    internal void EarlyDispose(InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers> internalNodeHelper, Lock? locker, bool isDisposed)
    {
        var impl = new StrongHandle(Interlocked.Exchange(ref internalNodeHelper._impl.Handle, IntPtr.Zero));
        if (impl.Handle != IntPtr.Zero)
        {
            Debug.Assert(isDisposed || locker is null || locker.IsHeldByCurrentThread, "Lock should be held by current thread.");
            _finalizeAttemptCount = -1;
#if NET
            _dependentHandle.Dispose();
            _finalizeHelperNode.Dispose();
            _trackingInfoHandle.Dispose();
#else
            _value.Dispose();
            _cwtNode.Dispose();
#endif
            _node.Dispose();
            impl.Dispose();
            GC.SuppressFinalize(internalNodeHelper);
            GC.KeepAlive(internalNodeHelper); // Ensure it's alive long enough that we don't run the finalizer.
        }
        else
        {
            // Another thread is concurrently disposing this node - that's fine, it will handle cleanup.
            // Note: we don't check _dependentHandle.IsAllocated etc. here because the other thread may not have
            // disposed them yet (the handle exchange happens before the disposal).
        }
    }
}