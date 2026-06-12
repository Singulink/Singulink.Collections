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
}
