using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections.WeakCollectionHelpers;

// Interface for the helper methods that our shared weak collection helpers must call into.
internal interface INodeHelpers<T, TNode, TContainer, TNodeHelpers>
    where T : class
    where TNode : class
    where TContainer : class
    where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
{
    /// <summary>
    /// Gets the state field for a given node class instance.
    /// </summary>
    ref NodeState<T, TNode, TContainer, TNodeHelpers> GetNodeState(TNode node);

    /// <summary>
    /// The callback for doing the actual deletion of a node. See <see cref="WeakList{T}.DeleteHelper" /> for an example of how to implement this correctly.
    /// </summary>
    void DeleteHelper(TContainer container, TNode node);

    /// <summary>
    /// Returns whether the collection is already disposed or not.
    /// </summary>
    /// <remarks>
    /// On non-locking collections, this must simply be based on the field <see cref="GetDisableAllocations"/> represents.
    /// </remarks>
    bool IsDisposed(TContainer container);

    /// <summary>
    /// Gets the state field for a given container instance.
    /// </summary>
    ref ContainerValues<T, TNode, TContainer, TNodeHelpers> GetContainerValues(TContainer container);

    /// <summary>
    /// Gets the locker for a given container instance.
    /// </summary>
    Lock GetLocker(TContainer container);

    /// <summary>
    /// Gets a value indicating whether the container needs a locker or not. If not, then <see cref="GetLocker"/> will throw.
    /// </summary>
    bool HasLocker { get; }

    /// <summary>
    /// Gets a reference to the flag that the weak collection helpers can use to disable future allocations. This is ONLY applicable to non-locking collections.
    /// </summary>
    /// <remarks>
    /// Implementation detail notes: This should be checked in the allocation routine, and be accessed always under the appropriate allocation-coordination lock
    /// (on .NET standard, that is the lock from <c>GetAllocationLock</c>, which should be held for the duration of linking into the conditional weak table, and
    /// on .NET that is the <c>InternalNodeTrackingInfo</c> instance's locker (similarly, we should be holding this for the duration of linking into that)).
    /// </remarks>
    ref bool GetDisableAllocations(TContainer container);

    /// <summary>
    /// Callback for throwing the appropriate exception for when the collection is already disposed. This is ONLY applicable to non-locking collections.
    /// </summary>
    [DoesNotReturn]
    void ThrowDisposed();

    /// <summary>
    /// Gets a reference to the helper linked list field for a given container instance. This is ONLY applicable to non-locking collections.
    /// </summary>
    /// <remarks>
    /// Implementation detail notes: This list and its nodes should only be modified or read under the lock from <see cref="GetNodeHelperListLock"/>. Additions
    /// to this list must only occur when we are not disabling allocations and are also holding the appropriate locks to ensure that.
    /// </remarks>
    ref LinkedList<TNode>? GetNodeHelperList(TContainer container);

    /// <summary>
    /// Gets a reference to the helper linked list node field for a given node instance. This is ONLY applicable to non-locking collections.
    /// </summary>
    ref LinkedListNode<TNode>? GetNodeHelperNode(TNode node);

    /// <summary>
    /// Gets the lock that coordinates access to the node helper tracking list. This is ONLY applicable to non-locking collections.
    /// </summary>
    Lock GetNodeHelperListLock(TContainer container);

#if !NET
    /// <summary>
    /// Gets the lock that coordinates allocation against the conditional weak table. This is ONLY applicable to non-locking collections.
    /// </summary>
    Lock GetAllocationLock(TContainer container);
#endif
}
