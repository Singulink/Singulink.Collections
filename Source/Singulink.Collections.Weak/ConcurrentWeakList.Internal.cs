using System.Diagnostics;
using System.Runtime;

namespace Singulink.Collections;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable RCS1043 // Remove 'partial' modifier from type with a single part

/// <content>
/// Contains the internal node tracking and finalization types for <see cref="ConcurrentWeakList{T}"/>.
/// </content>
public sealed partial class ConcurrentWeakList<T>
{
#if NET
    internal sealed class InternalNodeTrackingInfo(LinkedList<WeakReference<InternalNodeFinalizeHelper>> list, Lock locker)
    {
        public LinkedList<WeakReference<InternalNodeFinalizeHelper>> List = list;
        public Lock Locker = locker;
    }

    private sealed class CleanupHelper(WeakHandle listRef)
    {
        public WeakHandle ListRef = listRef; // Type of value is InternalNodeTrackingInfo.

        ~CleanupHelper()
        {
            if (ListRef.TryGetTarget<InternalNodeTrackingInfo>() is { } list)
            {
                lock (list.Locker)
                {
                    foreach (var handle in list.List)
                    {
                        if (TryGetValue(handle) is { } node && node._impl.GetTarget<InternalNode>() is { } n)
                        {
                            n.EarlyDispose(node, list.Locker, isDisposed: true);
                        }
                    }
                }

                GC.KeepAlive(list);
            }

            ListRef.Dispose();
        }
    }
#endif

    internal sealed partial class InternalNodeFinalizeHelper
    {
        // Note: we cannot store the list as a strong reference in this type with the current implementation - the Node stores such a reference.
        // Our reference to the list - note, it's important that this is a weak reference as we directly reference it from InternalNode, then we will leak the
        // whole list due to https://github.com/dotnet/runtime/issues/12255.
        public StrongHandle _impl; // The type of value is InternalNode.

        ~InternalNodeFinalizeHelper()
        {
            // Get the node to remove, or discover if we've already been disposed:
            var impl = new StrongHandle(Interlocked.Exchange(ref _impl.Handle, IntPtr.Zero));
            if (impl.Handle == IntPtr.Zero)
            {
                Debug.Fail("InternalNodeFinalizeHelper finalizer invoked, but was already disposed.");
            }
            else if (!impl.GetNotNullTarget<InternalNode>().Dispose(disposing: false, null, ref _impl, impl))
            {
                GC.ReRegisterForFinalize(this);
            }
        }
    }

    // This part is spun out of Node to ensure that the list isn't kept alive by InternalNodeFinalizeHelper, and it's spun off from InternalNodeFinalizeHelper
    // to ensure we can dispose & access it at any time (including before InternalNodeFinalizeHelper is disposed).
    internal sealed class InternalNode
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

        internal void RemoveFinalizeHelper()
        {
            if (_finalizeHelperNode.TryGetTarget<LinkedListNode<WeakReference<InternalNodeFinalizeHelper>>>() is { } finalizeHelperNode)
            {
                finalizeHelperNode.List?.Remove(finalizeHelperNode);
                GC.KeepAlive(finalizeHelperNode);
            }

            _finalizeHelperNode.Dispose();
        }
#endif

        internal bool RemoveFromList(Node node, ref StrongHandle implHandle, StrongHandle original, bool disposing)
        {
            // Try to enter the lock now:
            var list = node._list;
            bool entered = true;
            using var scope = (_finalizeAttemptCount < 5 && !disposing)
                ? list.TryEnterLock(out bool wasDisposed, out entered)
                : list.EnterLock(out wasDisposed);
            if (!wasDisposed)
            {
                if (entered)
                {
                    _finalizeAttemptCount = -1;
                    Thread.MemoryBarrier(); // Mark as removed.
                    try
                    {
                        list.DeleteHelper(node);
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
                        _value.Dispose();

                        // Ensure removed from CWT tracking stuff (the remove logic might have missed it, since we're not necessarily alive anymore, so it
                        // might not be able to look up these lists):
                        if (_cwtNode.TryGetTarget<LinkedListNode<InternalNodeFinalizeHelper>>() is { } cwtNode) cwtNode.List?.Remove(cwtNode);
                        _cwtNode.Dispose();
#endif
                    }
                }
                else
                {
                    // Let's just try again later as it might not be contested then (up to 5 times):
                    _finalizeAttemptCount++;
                    implHandle.Handle = original.Handle;
                    Thread.MemoryBarrier(); // Ensure the handle gets updated before we exit the lock.
                    return false;
                }
            }
            else
            {
                // Mark as removed even though list is disposed - callers may still check IsRemoved:
                _finalizeAttemptCount = -1;
                Thread.MemoryBarrier(); // Mark as removed.

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

        internal bool Dispose(bool disposing, Node? node, ref StrongHandle implHandle, StrongHandle previousHandle)
        {
            bool releaseHandle = true;
            bool hasNodeValue = false;
            try
            {
                // Try to get the node if we don't have it yet:
                node ??= _node.TryGetTarget<Node>();

                // If we don't have the node, we can't call RemoveFromList, so we just finalize our unmanaged resources:
                if (node is null) return true;

                // Remove the node from the list:
                hasNodeValue = true;
                if (!RemoveFromList(node, ref implHandle, previousHandle, disposing))
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
                        "These values should already have been disposed in RemoveFromList.");
                    _dependentHandle.Dispose();
                    _finalizeHelperNode.Dispose();
                    _trackingInfoHandle.Dispose();
#else
                    Debug.Assert(
                        !(hasNodeValue && (_value.Handle != IntPtr.Zero || _cwtNode.Handle != IntPtr.Zero)),
                        "These values should already have been disposed in RemoveFromList.");
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
        internal void EarlyDispose(InternalNodeFinalizeHelper internalNodeHelper, Lock? locker, bool isDisposed)
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
                Thread.MemoryBarrier(); // Ensure the removed mark (_finalizeAttemptCount) is visible before we exit the lock.
            }
            else
            {
                // Another thread is concurrently disposing this node - that's fine, it will handle cleanup.
                // Note: we don't check _dependentHandle.IsAllocated etc. here because the other thread may not have
                // disposed them yet (the handle exchange happens before the disposal).
            }
        }
    }
}
