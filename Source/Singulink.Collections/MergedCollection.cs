using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Singulink.Collections
{
    /// <summary>
    /// Provides a wrapper around two collections to be presented as one combined collection.
    /// </summary>
    public sealed class MergedCollection<T> : ICollection<T>
    {
        private readonly ICollection<T> _first;
        private readonly ICollection<T> _second;

        /// <summary>
        /// Create a new instance of <see cref="MergedCollection{T}"/> using the provided collections.
        /// </summary>
        /// <param name="first">The first collection to wrap.</param>
        /// <param name="second">The second collection to wrap.</param>
        public MergedCollection(ICollection<T> first, ICollection<T> second)
        {
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));
        }

        /// <summary>
        /// Gets the number of elements contained in this collection.
        /// </summary>
        public int Count => _first.Count + _second.Count;

        /// <summary>
        /// Gets a value indicating whether this collection is read-only. Always returns true.
        /// </summary>
        public bool IsReadOnly => true;

        /// <summary>
        /// Determines whether the collection contains the specified value.
        /// </summary>
        public bool Contains(T item) => _first.Contains(item) || _second.Contains(item);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _first)
                yield return item;

            foreach (var item in _second)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _first.CopyTo(array, arrayIndex);
            _second.CopyTo(array, arrayIndex + _first.Count);
        }

        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        void ICollection<T>.Clear() => throw new NotSupportedException();

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    }
}
