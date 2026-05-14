using System.ComponentModel;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <content>
/// Contains the <see cref="Enumerator"/> nested type for <see cref="ConcurrentWeakList{T}"/>.
/// </content>
public sealed partial class ConcurrentWeakList<T>
{
    /// <summary>
    /// Structure for enumerating over values in the list.
    /// </summary>
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
        /// Gets the current node in the enumeration.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If the enumerator has been disposed.</exception>
        /// <exception cref="InvalidOperationException">If the enumeration has not started or has already finished.</exception>
        public readonly Node CurrentNode => _nodeEnumerator.Current;

        /// <summary>
        /// Gets the current value in the enumeration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This API may return <see langword="null" /> on first call, even if provided a valid starting node, until either <see cref="MoveNext" /> or
        /// <see cref="MovePrevious" /> is called.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the enumeration has not started or has already finished.</exception>
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
        /// Gets a value indicating whether the current value was added to the list during the enumeration.
        /// </summary>
        /// <exception cref="NullReferenceException">May be thrown if the current value is not valid.</exception>
        public readonly bool WasAddedDuringEnumeration => _nodeEnumerator.WasAddedDuringEnumeration;

        /// <summary>
        /// Helper API to support enumerating over an instance of <see cref="ConcurrentWeakList{T}.Enumerator" />.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly Enumerator GetEnumerator() => this;

        /// <summary>
        /// Moves to the next value in the enumeration.
        /// </summary>
        /// <remarks>
        /// <para>If the enumeration has not begun, or has reached the end, calling this method will move to the first valid node.</para>
        /// <para>If the enumerator or list has been disposed, this method will always return <see langword="false" />.</para>
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
        /// Moves to the previous value in the enumeration.
        /// </summary>
        /// <remarks>
        /// <para>If the enumeration has not begun, or has reached the end, calling this method will move to the last valid node.</para>
        /// <para>If the enumerator or list has been disposed, this method will always return <see langword="false" />.</para>
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
        /// Gets an enumerable for the remaining items in the enumeration.
        /// </summary>
        /// <param name="reversed">If <see langword="true" />, the enumeration will be in reverse order.</param>
        /// <param name="skipNewNodes">If <see langword="true" />, nodes added during enumeration will be skipped.</param>
        /// <remarks>
        /// Nodes that have values which have been collected will be skipped.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the enumerator has been disposed.</exception>
        public readonly IEnumerable<T> AsEnumerable(bool reversed = false, bool skipNewNodes = false)
        {
            Throw.IfDisposed(_nodeEnumerator._list is null, typeof(Enumerator));

            GC.KeepAlive(_nodeEnumerator._list);
            return new HeapValueEnumerable(this, reversed, skipNewNodes);
        }
    }
}
