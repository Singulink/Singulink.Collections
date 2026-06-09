using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Singulink.Collections;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable RCS1043 // Remove 'partial' modifier from type with a single part

/// <content>
/// Contains the <see cref="Node"/> nested type for <see cref="ConcurrentWeakList{T}"/>.
/// </content>
public sealed partial class ConcurrentWeakList<T>
{
    /// <summary>
    /// Represents a node in a <see cref="ConcurrentWeakList{T}" />.
    /// </summary>
    /// <remarks>
    /// Holding a strong reference to a <see cref="Node" /> does not prevent the value it references from being garbage collected.
    /// </remarks>
    public sealed partial class Node : IDisposable
    {
        // We need a weak reference here as the Node will be strongly held by the data structure until it's removed, but the InternalNodeFinalizeHelper is the
        // thing that is meant to be automatically GC'd.
        // Note: we don't use WeakHandle here, as we want to ensure that we're trivially thread-safe when trying to "dispose" it & read it simultaneously.
        internal WeakReference<InternalNodeFinalizeHelper>? _internalNodeHelper; // Type of value is InternalNodeFinalizeHelper.
        internal InternalNode? _internalNode;

        // Since we store the list here directly, we need to hold a weak ref back to Node from InternalNode:
        internal readonly ConcurrentWeakList<T> _list;

        // Our doubly linked list state:
        internal Node? _prev;
        internal Node? _next;
        internal bool _isPseudoNode;
        internal ulong _version;  // The version of the list when this node was added.

        // Used to mark nodes that have been removed but are still alive for enumerations.
        // When set, the _next node is set to the next node in the list, so that we can continue enumerating.
        // Also, _prev is set to the node that was previously ordered before this one, so that we can go backwards.
        internal bool _isRemoved;

        // Private constructor:
        internal Node(InternalNode? internalNode, ConcurrentWeakList<T> list)
        {
            _internalNode = internalNode;
            _list = list;
        }

        /// <summary>
        /// Gets the list that this node belongs to, or used to belong to.
        /// </summary>
        public ConcurrentWeakList<T> List
        {
            get
            {
                GC.KeepAlive(_list);
                return _list;
            }
        }

        /// <summary>
        /// Gets the target value of this node if it is still available, otherwise <see langword="null" />.
        /// </summary>
        /// <remarks>
        /// This method can return <see langword="null" /> even if the node has not yet been removed from the list, so don't try to use this to optimize
        /// avoiding calling <see cref="Dispose()" /> unnecessarily; it's better to just call it unconditionally.
        /// </remarks>
        public T? Value
        {
            get
            {
                // Note: we must take the lock here, as otherwise we could be partway through disposing or updating the value:
                // Note: we technically still could be partway through disposing after taking the lock, but not in a problematic way.
                if (_list._head is null) return null;
                using var scope = _list.EnterLock(out bool wasDisposed);
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
        /// Gets the version of the list when this node was added.
        /// </summary>
        public ListVersion Version => new(_version);

        /// <summary>
        /// Gets a value indicating whether this node has been removed from the list.
        /// </summary>
        public bool IsRemoved
        {
            get
            {
                // Note: when the lock is held, it is enough to just check the color, but otherwise checking IsRemoved is more up-to-date.
                var internalNode = _internalNode;
                if (internalNode is null) return true;
                Thread.MemoryBarrier(); // Ensure we get the latest value.
                bool result = Volatile.Read(ref internalNode._finalizeAttemptCount) == -1;
                GC.KeepAlive(_list);
                return result;
            }
        }

        /// <summary>
        /// Disposes the node, removing it from the <see cref="ConcurrentWeakList{T}" /> it belongs to.
        /// </summary>
        public void Dispose()
        {
            if (_internalNode is not null && GetInternalNodeHelper() is { } helper)
            {
                var impl = new StrongHandle(Interlocked.Exchange(ref helper._impl.Handle, IntPtr.Zero));
                if (impl.Handle != IntPtr.Zero && _internalNode.Dispose(disposing: true, this, ref helper._impl, impl))
                {
                    GC.SuppressFinalize(helper);
                }

                GC.KeepAlive(helper);
                GC.KeepAlive(_internalNode);
            }

            _internalNodeHelper = null;
            _internalNode = null;
            GC.KeepAlive(_list);
        }

#if NET
        /// <summary>
        /// Try to update the target of this node to a new value.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the value is null.</exception>
        /// <remarks>
        /// This method is only supported on frameworks that have <see cref="DependentHandle" />.
        /// </remarks>
        public bool TryUpdateTarget(T newTarget)
        {
            // Note: nothing in theory prevents us from implementing this on .NET Standard, but it would be more complex (due to having to update the CWT), so
            // we just don't support it there for now.
            using var scope = _list.EnterLock(out bool wasDisposed);
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

        // Helper method for testing & for internal use:
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal InternalNodeFinalizeHelper? GetInternalNodeHelper() => TryGetValue(_internalNodeHelper);
    }
}
