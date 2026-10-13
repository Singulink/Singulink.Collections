using System.ComponentModel;

namespace Singulink.Collections;

/// <content>
/// Contains the <see cref="Enumerator"/> nested type for <see cref="WeakList{T}"/>.
/// </content>
public sealed partial class WeakList<T>
{
    /// <summary>
    /// Enumerates the values in a <see cref="WeakList{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>Enumerators are safe to use while other threads modify the list, and they skip nodes whose values have been garbage collected. They can move in
    /// both directions, and moving past either end of the list wraps around to the other end.</para>
    /// </remarks>
    public struct Enumerator
    {
        internal NodeEnumerator _nodeEnumerator;
        private T? _value;

        internal Enumerator(NodeEnumerator nodeEnumerator)
        {
            _nodeEnumerator = nodeEnumerator;
            _value = null;
        }

        /// <summary>
        /// Gets the node at the current position of the enumerator.
        /// </summary>
        /// <exception cref="InvalidOperationException">The enumerator is not positioned on a value, either because enumeration has not started, has reached the
        /// end, or the list has been disposed.</exception>
        public readonly Node CurrentNode => _nodeEnumerator.Current;

        /// <summary>
        /// Gets the value at the current position of the enumerator.
        /// </summary>
        /// <exception cref="InvalidOperationException">The enumerator is not positioned on a value, either because enumeration has not started, has reached the
        /// end, or the list has been disposed. An enumerator created from a start node is not positioned on a value until <see cref="MoveNext"/> or
        /// <see cref="MovePrevious"/> is called.</exception>
        public readonly T Current
        {
            get
            {
                var value = _value;

                if (value is null)
                    Throw.InvalidEnumeration();

                return value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the node at the current position was added to the list after the enumerator was created.
        /// </summary>
        /// <exception cref="InvalidOperationException">The enumerator is not positioned on a value.</exception>
        public readonly bool WasAddedDuringEnumeration => _nodeEnumerator.WasAddedDuringEnumeration;

        /// <summary>
        /// Returns this enumerator, which allows it to be used directly in a <see langword="foreach"/> statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly Enumerator GetEnumerator() => this;

        /// <summary>
        /// Advances the enumerator to the next value in the list.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator advanced to a value; <see langword="false"/> if the end of the list was reached or the list has been
        /// disposed.</returns>
        /// <remarks>
        /// If the enumerator is not positioned on a value, because enumeration has not started or has reached the end, this method moves to the first value in
        /// the list.
        /// </remarks>
        public bool MoveNext()
        {
            do
            {
                if (!_nodeEnumerator.MoveNext())
                {
                    _value = null;
                    return false;
                }
            }
            while ((_value = _nodeEnumerator.Current.Value) is null);
            return true;
        }

        /// <summary>
        /// Moves the enumerator to the previous value in the list.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator moved to a value; <see langword="false"/> if the start of the list was reached or the list has been
        /// disposed.</returns>
        /// <remarks>
        /// If the enumerator is not positioned on a value, because enumeration has not started or has reached the start, this method moves to the last value in
        /// the list.
        /// </remarks>
        public bool MovePrevious()
        {
            do
            {
                if (!_nodeEnumerator.MovePrevious())
                {
                    _value = null;
                    return false;
                }
            }
            while ((_value = _nodeEnumerator.Current.Value) is null);
            return true;
        }

        /// <summary>
        /// Returns an enumerable that continues this enumeration from its current position.
        /// </summary>
        /// <param name="reversed"><see langword="true"/> to enumerate towards the start of the list; <see langword="false"/> to enumerate towards the end.</param>
        /// <param name="skipNewNodes"><see langword="true"/> to skip nodes that were added to the list after this enumerator was created; <see langword="false"/>
        /// to include them.</param>
        /// <remarks>
        /// Nodes whose values have been garbage collected are skipped.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The enumerator was not created by a list.</exception>
        public readonly IEnumerable<T> AsEnumerable(bool reversed = false, bool skipNewNodes = false)
        {
            Throw.IfDisposed(_nodeEnumerator._list is null, typeof(Enumerator));

            GC.KeepAlive(_nodeEnumerator._list);
            return new HeapValueEnumerable(this, reversed, skipNewNodes);
        }
    }
}
