using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <content>
/// Contains the <see cref="NodeEnumerator"/> nested type for <see cref="WeakList{T}"/>.
/// </content>
public sealed partial class WeakList<T>
{
    /// <summary>
    /// Enumerates the nodes in a <see cref="WeakList{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>Enumerators are safe to use while other threads modify the list. All nodes are enumerated, including nodes whose values have been garbage
    /// collected but have not yet been removed from the list. They can move in both directions, and moving past either end of the list wraps around to the
    /// other end.</para>
    /// </remarks>
    public struct NodeEnumerator
    {
        internal readonly WeakList<T>? _list;
        internal Node? _currentNode;
        internal ulong _listVersion;

        internal NodeEnumerator(WeakList<T> list, Node? node)
        {
            _list = list;
            _currentNode = node;

            using var scope = _list.EnterLock(out bool wasDisposed);

            if (wasDisposed)
                _currentNode = null;

            _listVersion = list._version;
        }

        /// <summary>
        /// Gets the node at the current position of the enumerator.
        /// </summary>
        /// <exception cref="InvalidOperationException">The enumerator is not positioned on a node, either because enumeration has not started, has reached the
        /// end, or the list has been disposed. An enumerator created from a start node is not positioned on a node until <see cref="MoveNext"/> or
        /// <see cref="MovePrevious"/> is called.</exception>
        public readonly Node Current
        {
            get
            {
                var currentNode = _currentNode;

                if (currentNode is null)
                    Throw.InvalidEnumeration();

                GC.KeepAlive(_list);
                return currentNode;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the node at the current position was added to the list after the enumerator was created.
        /// </summary>
        /// <exception cref="InvalidOperationException">The enumerator is not positioned on a node.</exception>
        public readonly bool WasAddedDuringEnumeration => Current._version > _listVersion;

        /// <summary>
        /// Returns this enumerator, which allows it to be used directly in a <see langword="foreach"/> statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly NodeEnumerator GetEnumerator() => this;

        /// <summary>
        /// Advances the enumerator to the next node in the list.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator advanced to a node; <see langword="false"/> if the end of the list was reached or the list has been
        /// disposed.</returns>
        /// <remarks>
        /// If the enumerator is not positioned on a node, because enumeration has not started or has reached the end, this method moves to the first node in the
        /// list.
        /// </remarks>
        public bool MoveNext()
        {
            // Check if we know it's disposed before locking:
            if (IsDisposed())
                goto disposed;

            // Get the next node (unless it has been removed):
            Node? newNode = _currentNode;
            bool isRemovedNode;
            using (_list.EnterLock(out bool wasDisposed))
            {
                if (wasDisposed)
                    goto disposed;

                newNode = _list.GetNextNode(newNode, out isRemovedNode);
            }

            // Handle removed node (it's important that we handle this outside the lock to control how long we hold it at most):
            // Since there's no way to guarantee that the non-removed node is still in the list by the time the caller uses it, we just try our best.
            if (isRemovedNode && newNode is not null)
            {
                do newNode = newNode._next;
                while (newNode is { _isRemoved: true } or { IsRemoved: true });
            }

            // Set the new node and return:
            _currentNode = newNode;
            return newNode is not null;

            // If disposed, clear current node and return false:
            disposed:
            _currentNode = null;
            return false;
        }

        /// <summary>
        /// Moves the enumerator to the previous node in the list.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator moved to a node; <see langword="false"/> if the start of the list was reached or the list has been
        /// disposed.</returns>
        /// <remarks>
        /// If the enumerator is not positioned on a node, because enumeration has not started or has reached the start, this method moves to the last node in the
        /// list.
        /// </remarks>
        public bool MovePrevious()
        {
            // Check if we know it's disposed before locking:
            if (IsDisposed())
                goto disposed;

            // Get the previous node (unless it has been removed):
            Node? newNode = _currentNode;
            bool isRemovedNode;
            using (_list.EnterLock(out bool wasDisposed))
            {
                if (wasDisposed)
                    goto disposed;

                newNode = _list.GetPrevNode(newNode, out isRemovedNode);
            }

            // Handle removed node (it's important that we handle this outside the lock to control how long we hold it at most):
            // Since there's no way to guarantee that the non-removed node is still in the list by the time the caller uses it, we just try our best.
            if (isRemovedNode && newNode is not null)
            {
                do newNode = newNode._prev;
                while (newNode is { _isRemoved: true } or { IsRemoved: true });
            }

            // Set the new node and return:
            _currentNode = newNode;
            return newNode is not null;

            // If disposed, clear current node and return false:
            disposed:
            _currentNode = null;
            return false;
        }

        /// <summary>
        /// Returns an enumerable that continues this enumeration from its current position.
        /// </summary>
        /// <param name="reversed"><see langword="true"/> to enumerate towards the start of the list; <see langword="false"/> to enumerate towards the end.</param>
        /// <param name="skipNewNodes"><see langword="true"/> to skip nodes that were added to the list after this enumerator was created; <see langword="false"/>
        /// to include them.</param>
        /// <remarks>
        /// Nodes whose values have been garbage collected are included.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The enumerator was not created by a list.</exception>
        public readonly IEnumerable<Node> AsEnumerable(bool reversed = false, bool skipNewNodes = false)
        {
            Throw.IfDisposed(_list is null, typeof(NodeEnumerator));
            GC.KeepAlive(_list);
            return new HeapNodeEnumerable(this, reversed, skipNewNodes);
        }

        [MemberNotNullWhen(false, nameof(_list))]
        internal readonly bool IsDisposed()
        {
            return _list is null or { _head: null };
        }
    }
}
