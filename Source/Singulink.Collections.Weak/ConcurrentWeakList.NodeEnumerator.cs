using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <content>
/// Contains the <see cref="NodeEnumerator"/> nested type for <see cref="ConcurrentWeakList{T}"/>.
/// </content>
public sealed partial class ConcurrentWeakList<T>
{
    /// <summary>
    /// Structure for enumerating over nodes in the list.
    /// </summary>
    public struct NodeEnumerator
    {
        internal readonly ConcurrentWeakList<T>? _list;
        internal Node? _currentNode;
        internal ulong _listVersion;

        internal NodeEnumerator(ConcurrentWeakList<T> list, Node? node)
        {
            _list = list;
            _currentNode = node;

            using var scope = _list.EnterLock(out bool wasDisposed);
            if (wasDisposed) _currentNode = null;

            _listVersion = list._version;
        }

        /// <summary>
        /// Gets the current node in the enumeration.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the enumeration has not started or has already finished.</exception>
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
        /// Gets a value indicating whether the current node was added to the list during the enumeration.
        /// </summary>
        /// <exception cref="NullReferenceException">May be thrown if the current node is not valid.</exception>
        public readonly bool WasAddedDuringEnumeration => _currentNode!._version > _listVersion;

        /// <summary>
        /// Helper API to support enumerating over an instance of <see cref="ConcurrentWeakList{T}.NodeEnumerator" />.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly NodeEnumerator GetEnumerator() => this;

        /// <summary>
        /// Moves to the next node in the enumeration.
        /// </summary>
        /// <remarks>
        /// <para>If the enumeration has not begun, or has reached the end, calling this method will move to the first valid node.</para>
        /// <para>If the enumerator or list has been disposed, this method will always return <see langword="false" />.</para>
        /// </remarks>
        public bool MoveNext()
        {
            // Check if we know it's disposed before locking:
            if (IsDisposed()) goto disposed;

            // Get the next node (unless it has been removed):
            Node? newNode = _currentNode;
            bool isRemovedNode;
            using (_list.EnterLock(out bool wasDisposed))
            {
                if (wasDisposed) goto disposed;
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
        /// Moves to the previous node in the enumeration.
        /// </summary>
        /// <remarks>
        /// <para>If the enumeration has not begun, or has reached the end, calling this method will move to the last valid node.</para>
        /// <para>If the enumerator or list has been disposed, this method will always return <see langword="false" />.</para>
        /// </remarks>
        public bool MovePrevious()
        {
            // Check if we know it's disposed before locking:
            if (IsDisposed()) goto disposed;

            // Get the previous node (unless it has been removed):
            Node? newNode = _currentNode;
            bool isRemovedNode;
            using (_list.EnterLock(out bool wasDisposed))
            {
                if (wasDisposed) goto disposed;
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
        /// Gets an enumerable for the remaining nodes in the enumeration.
        /// </summary>
        /// <param name="reversed">If <see langword="true" />, the enumeration will be in reverse order.</param>
        /// <param name="skipNewNodes">If <see langword="true" />, nodes added during enumeration will be skipped.</param>
        /// <remarks>
        /// Nodes that have values which have been collected will not be skipped.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the enumerator has been disposed.</exception>
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
