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

    // Since we store the list here directly, we need to hold a weak ref back to Node from InternalNode:
    internal readonly TContainer _container;

    // Constructor
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

            GC.KeepAlive(helper);
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
    private readonly InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>? GetInternalNodeHelper() => WeakReferenceHelpers.TryGetValue(_internalNodeHelper);

    /// <summary>
    /// Helper for allocating the node and related resources.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Alloc(T value, TNode node, InternalNode<T, TNode, TContainer, TNodeHelpers> internalNode, ref ContainerValues<T, TNode, TContainer, TNodeHelpers> containerValues)
    {
        InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers> internalNodeHelper = new();
        internalNode._node = WeakHandle.Alloc(node);
        internalNodeHelper._impl = StrongHandle.Alloc(internalNode);
        _internalNodeHelper = new(internalNodeHelper);
#if NET
        internalNode._dependentHandle = new DependentHandle(value, internalNodeHelper);
        lock (containerValues._internalNodes.Locker) internalNode._finalizeHelperNode = WeakHandle.Alloc(containerValues._internalNodes.List.AddLast(_internalNodeHelper));
        internalNode._trackingInfoHandle = StrongHandle.Alloc(containerValues._internalNodes);
#else
        internalNode._value = WeakHandle.Alloc(value);
        Debug.Assert(containerValues._cwt != null, "CWT should not be null here, as the container is not disposed.");
        var list = containerValues._cwt.GetValue(value, static _ => []);
        bool entered = !default(TNodeHelpers).HasLocker; // We need to lock on the linked list, if we don't have the container lock.
        if (entered) Monitor.Enter(list);
        try
        {
            internalNode._cwtNode = WeakHandle.Alloc(list.AddLast(internalNodeHelper));
        }
        finally
        {
            if (entered) Monitor.Exit(list);
        }
#endif
        GC.KeepAlive(node);
    }

    /// <summary>
    /// The method to call to clean out the node for 'HandleFailureOrDispose' methods.
    /// </summary>
    /// <remarks>
    /// This method does not support non-locking collections.
    /// </remarks>
    public void CleanUpForHandleFailureOrDispose()
    {
        // Finalizer is not critical here, other than our handles & marking removed, so clean those up and then suppress:
        if (GetInternalNodeHelper() is { } helper)
        {
            _internalNode?.EarlyDispose(helper, default(TNodeHelpers).GetLocker(_container), true);
        }

        // Set node finalizer to null:
        _internalNodeHelper = null;
    }

    /// <summary>
    /// Helper method to check that the finalize helper handle is zero currently - this allows checking that the code was called from the finalizer in a likely correct state (sanity check only).
    /// </summary>
    /// <remarks>
    /// This method does not support non-locking collections.
    /// </remarks>
    public readonly bool IsFinalizeHelperHandleZero()
    {
        return (GetInternalNodeHelper()?._impl.Handle).GetValueOrDefault() == IntPtr.Zero;
    }
}
