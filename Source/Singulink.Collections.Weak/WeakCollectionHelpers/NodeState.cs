using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

// NOTE: for correct usage, ensure you follow what WeakList does.
// NOTE: only the APIs marked public are intended to be used outside of the namespace.

namespace Singulink.Collections.WeakCollectionHelpers;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

// State & public accessors for Node stuff that reaches into InternalNode, that real Node types can call into instead of leaking abstractions outwards too much.
internal struct NodeState<T, TNode, TContainer, TNodeHelpers>
    where T : class
    where TNode : class
    where TContainer : class
    where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
{
    // We need a weak reference here as the Node will be strongly held by the data structure until it's removed, but the InternalNodeFinalizeHelper is the
    // thing that is meant to be automatically GC'd.
    // Note: we don't use WeakHandle here, as we want to ensure that we're trivially thread-safe when trying to "dispose" it & read it simultaneously.
    internal WeakReference<InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>>? _internalNodeHelper; // Type of value is InternalNodeFinalizeHelper.

    // Strong reference to the InternalNode.
    private InternalNode<T, TNode, TContainer, TNodeHelpers>? _internalNode;

    // Since we store the container here directly, we need to hold a weak ref back to Node from InternalNode:
    internal readonly TContainer _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeState{T, TNode, TContainer, TNodeHelpers}"/> struct based on the provided parameters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NodeState(InternalNode<T, TNode, TContainer, TNodeHelpers>? internalNode, TContainer container)
    {
        _internalNode = internalNode;
        _container = container;
    }

    /// <summary>
    /// Gets the container that this node belongs to, or used to belong to.
    /// </summary>
    public readonly TContainer Container
    {
        get
        {
            var container = _container;
            GC.KeepAlive(container);
            return container;
        }
    }

    /// <summary>
    /// Gets the container that this node belongs to, or used to belong to, without any additional checks or operations.
    /// </summary>
    public readonly TContainer ContainerDirect => _container;

    /// <summary>
    /// Gets the target value of this node if it is still available, otherwise <see langword="null" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method can return <see langword="null" /> even if the node has not yet been removed from the container, so don't try to use this to optimize
    /// avoiding calling <see cref="Dispose(TNode)" /> unnecessarily; it's better to just call it unconditionally.
    /// </para>
    /// <para>
    /// Note: this property is not possible to use safely on non-locking collections.
    /// </para>
    /// </remarks>
    public readonly T? Value
    {
        get
        {
            // Note: we must take the lock here, as otherwise we could be partway through disposing or updating the value:
            // Note: we technically still could be partway through disposing after taking the lock, but not in a problematic way.
            if (default(TNodeHelpers).IsDisposed(_container)) return null;
            using var scope = LockScope.EnterLock<T, TNode, TContainer, TNodeHelpers>(_container, out bool wasDisposed);
            if (wasDisposed) return null;
            var internalNode = _internalNode;
            if (internalNode is null) return null;
            var helper = GetInternalNodeHelper();
            if (helper is null) return null;

            // Do the actual get:
            T? retV;
#if NET
            var dependentHandle = internalNode._dependentHandle;
            if (!dependentHandle.IsAllocated) return null;
            object? result = dependentHandle.Target;
            Debug.Assert(result is T or null, "Stored target should be of the correct type or null.");
            retV = Unsafe.As<T?>(result);
#else
            var result = internalNode._value;
            retV = result.TryGetTarget<T>();
#endif

            // Keep helper alive & return:
            GC.KeepAlive(helper);
            return retV;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this node has been removed from the container.
    /// </summary>
    public readonly bool IsRemoved
    {
        get
        {
            var internalNode = _internalNode;
            if (internalNode is null) return true;
            Thread.MemoryBarrier(); // Ensure we get the latest value (this stops the read from being re-ordered earlier, but it can still re-order to later).
            bool result = internalNode._finalizeAttemptCount == -1;
            GC.KeepAlive(_container);
            return result;
        }
    }

    /// <summary>
    /// Disposes the node, removing it from the container it belongs to.
    /// </summary>
    public void Dispose(TNode self)
    {
        var internalNode = _internalNode;
        if (internalNode is not null && GetInternalNodeHelper() is { } helper)
        {
            var impl = new StrongHandle(Interlocked.Exchange(ref helper._impl.Handle, IntPtr.Zero));
            if (impl.Handle != IntPtr.Zero && internalNode.Dispose(disposing: true, self, ref helper._impl, impl))
            {
                GC.SuppressFinalize(helper);
            }

            GC.KeepAlive(helper); // Ensure the finalizer can't run while we're attempting to dispose, so we can be sure it's done at the end of this method.
            GC.KeepAlive(self);
        }

        _internalNodeHelper = null;
        _internalNode = null;
        GC.KeepAlive(_container);
    }

#if NET
    /// <summary>
    /// Try to update the target of this node to a new value.
    /// </summary>
    /// <exception cref="ArgumentNullException">If the value is null.</exception>
    /// <remarks>
    /// <para>
    /// This method is only supported on frameworks that have <see cref="DependentHandle" />.
    /// </para>
    /// <para>
    /// Note: this method is not possible to use safely on non-locking collections.
    /// </para>
    /// </remarks>
    public readonly bool TryUpdateTarget(T newTarget)
    {
        // Note: nothing in theory prevents us from implementing this on .NET Standard, but it would be more complex (due to having to update the CWT), so
        // we just don't support it there for now.
        using var scope = LockScope.EnterLock<T, TNode, TContainer, TNodeHelpers>(_container, out bool wasDisposed);
        if (GetInternalNodeHelper() is not { } helper) return false;
        if (wasDisposed) return false;
        if (helper._impl.Handle == IntPtr.Zero) return false;
        var internalNode = _internalNode;
        if (internalNode is null) return false;

        // Update the DependentHandle:
        if (!internalNode._dependentHandle.IsAllocated) return false;
        object? oldTarget = internalNode._dependentHandle.Target;
        DependentHandle dh = new(newTarget, helper);
        var oldDh = internalNode._dependentHandle;
        internalNode._dependentHandle = dh;
        oldDh.Dispose();

        // Keep alive the old target, new target, and the helper until after we've updated the handle:
        GC.KeepAlive(oldTarget);
        GC.KeepAlive(newTarget);
        GC.KeepAlive(helper);
        return true;
    }
#endif

    /// <summary>
    /// Helper method for testing and for internal use.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>? GetInternalNodeHelper()
    {
        return WeakReferenceHelpers.TryGetValue(_internalNodeHelper);
    }

    /// <summary>
    /// Helper for allocating the node and related resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Callers must have already checked for disposal.
    /// </para>
    /// <para>
    /// Callers must GC.KeepAlive the value until after it is fully linked into the collection.
    /// </para>
    /// <para>
    /// For locking collections, the caller must hold the lock to call this method.
    /// </para>
    /// <para>
    /// This method requires the caller to hold the lock if it is a locking collection, or to ensure the collection is kept alive until after the node is fully
    /// linked in otherwise.
    /// </para>
    /// <para>
    /// For lock-free collections: if there are custom fields on the node type that are not initialized before calling this method, it is the caller's
    /// responsibility to ensure they cannot have their writes re-ordered improperly such that another thread can view the incorrect value.
    /// </para>
    /// <para>
    /// For non-locking collections, this API can throw <see cref="ObjectDisposedException" />, which the caller should be prepared to handle (e.g., by
    /// releasing any unmanaged resources and rethrowing).
    /// </para>
    /// </remarks>
    public void Alloc(
        T value,
        TNode node,
        InternalNode<T, TNode, TContainer, TNodeHelpers> internalNode,
        TContainer container)
    {
        ref var containerValues = ref default(TNodeHelpers).GetContainerValues(container);
        InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers> internalNodeHelper = new();
        internalNode._node = WeakHandle.Alloc(node);
        internalNodeHelper._impl = StrongHandle.Alloc(internalNode);
        _internalNodeHelper = new(internalNodeHelper);
#if NET
        internalNode._dependentHandle = new DependentHandle(value, internalNodeHelper);
        internalNode._trackingInfoHandle = StrongHandle.Alloc(containerValues._internalNodes);
        if (default(TNodeHelpers).HasLocker)
        {
            internalNode._finalizeHelperNode = WeakHandle.Alloc(containerValues._internalNodes.List.AddLast(_internalNodeHelper));
        }
        else
        {
            // Note: we only need to hold this lock on the non-locking collection for the following reason:
            // - We stop concurrent manual removals by holding the collection lock.
            // - We stop concurrent cleanup helper removal by keeping the collection alive until after this code.
            // - Therefore, there are no cases where it could be modified concurrently, and we have appropriate barriers from the lock to ensure consistency.
            // However, a non-locking collection could have removals occuring concurrently, therefore we need to lock the usage of this list always.
            bool continueAllocating = true;
            lock (containerValues._internalNodes.Locker)
            {
                // Firstly, check if we are not meant to be allocating any more (disposed), and if so, prepare to release handles / similar and throw:
                if (default(TNodeHelpers).GetDisableAllocations(container))
                {
                    continueAllocating = false;
                }

                // Otherwise, allocate
                else
                {
                    var trackingList = default(TNodeHelpers).GetNodeHelperList(container);
                    Debug.Assert(trackingList != null, "Tracking list should not be null here, as the container is not disposed.");
                    lock (default(TNodeHelpers).GetNodeHelperListLock(container))
                    {
                        default(TNodeHelpers).GetNodeHelperNode(node) = trackingList.AddLast(node);
                    }

                    internalNode._finalizeHelperNode = WeakHandle.Alloc(containerValues._internalNodes.List.AddLast(_internalNodeHelper));
                }
            }

            // Handle if we're not continuing with allocating:
            if (!continueAllocating)
            {
                internalNode._node.Dispose();
                internalNodeHelper._impl.Dispose();
                internalNode._dependentHandle.Dispose();
                internalNode._trackingInfoHandle.Dispose();
                GC.SuppressFinalize(internalNodeHelper);
                GC.KeepAlive(internalNodeHelper);
                default(TNodeHelpers).ThrowDisposed();
            }
        }
#else
        internalNode._value = WeakHandle.Alloc(value);
        var cwt = containerValues._cwt;
        if (default(TNodeHelpers).HasLocker)
        {
            internalNode._cwtNode = WeakHandle.Alloc(cwt.AddNoLock(value, internalNodeHelper));
        }
        else
        {
            // We need to lock on the cwt & linked list, if we don't have the container lock.
            bool continueAllocating = true;
            lock (default(TNodeHelpers).GetAllocationLock(container))
            {
                if (default(TNodeHelpers).GetDisableAllocations(container))
                {
                    continueAllocating = false;
                }
                else
                {
                    internalNode._cwtNode = WeakHandle.Alloc(cwt.Add(value, internalNodeHelper));
                    var trackingList = default(TNodeHelpers).GetNodeHelperList(container);
                    Debug.Assert(trackingList != null, "Tracking list should not be null here, as the container is not disposed.");
                    lock (default(TNodeHelpers).GetNodeHelperListLock(container))
                    {
                        default(TNodeHelpers).GetNodeHelperNode(node) = trackingList.AddLast(node);
                    }
                }
            }

            // Handle if we're not continuing with allocating:
            if (!continueAllocating)
            {
                internalNode._node.Dispose();
                internalNodeHelper._impl.Dispose();
                internalNode._value.Dispose();
                GC.SuppressFinalize(internalNodeHelper);
                GC.KeepAlive(internalNodeHelper);
                default(TNodeHelpers).ThrowDisposed();
            }
        }
#endif
        GC.KeepAlive(node);

        // Probably we could be fine without a write barrier if we are careful about how we set up our fields, but it is safer to have a write barrier at the
        // end of this, to ensure that we can re-order any code above in any way and not need to worry about it. The main reason it could be fine without it is
        // that field accesses can't be re-ordered after a write of the object that contains them.
        // Note: this is only necessary on the lock-free collections, as the locking ones are in a lock that provides a write barrier on release.
        // Note: we are guaranteed a write barrier by the Monitor.Exit / exit of lock above, which we use to achieve the above.
    }

    /// <summary>
    /// The method to call to clean out the node for 'HandleFailureOrDispose' methods.
    /// </summary>
    /// <remarks>
    /// This method does not support non-locking collections (except by the internal implementation as an implementation detail in some cases).
    /// </remarks>
    public void CleanUpForHandleFailureOrDispose()
    {
        // Finalizer is not critical here, other than our handles & marking removed, so clean those up and then suppress:
        if (GetInternalNodeHelper() is { } helper)
        {
            Lock? locker = default(TNodeHelpers).HasLocker ? default(TNodeHelpers).GetLocker(_container) : null;
            _internalNode?.EarlyDispose(helper, locker, true);
        }

        // Set node finalizer to null:
        _internalNodeHelper = null;
    }

    /// <summary>
    /// Helper method to check that the finalize helper handle is zero currently - this allows checking that the code was called from the finalizer in a likely
    /// correct state (sanity check only).
    /// </summary>
    /// <remarks>
    /// This method does not support non-locking collections.
    /// </remarks>
    public readonly bool IsFinalizeHelperHandleZero()
    {
        return (GetInternalNodeHelper()?._impl.Handle).GetValueOrDefault() == IntPtr.Zero;
    }
}
