using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    internal LinkedList<WeakReference<InternalNodeFinalizeHelper<T, TNode, TContainer, TNodeHelpers>>> List = list;

    // The lock that non-locking collections use to coordinate allocation/removal of tracking info (locking collections use their own container lock instead).
    // We only allocate the Lock object for non-locking collections; access it via the Locker property.
    // Note: locking collections still carry this (null) reference field, so they pay 8 bytes per whole collection - we don't currently don't try to elide the
    // field itself (e.g., via a subclass), as that isn't worth the complexity.
    private readonly Lock? _locker = default(TNodeHelpers).HasLocker ? null : new();

    // The lock for non-locking collections. Only valid to access on non-locking collections (where it is always non-null).
    internal Lock Locker
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(!default(TNodeHelpers).HasLocker, "Locker should only be accessed on non-locking collections.");
            Debug.Assert(_locker is not null, "Locker should not be null on non-locking collections.");
            return _locker;
        }
    }
}
#endif
