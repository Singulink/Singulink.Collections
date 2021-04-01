using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Singulink.Collections
{
    public class ReadOnlyHashSet<T> : ICollection<T>, IReadOnlyCollection<T>, ISet<T>
    {
        private HashSet<T> _set;

        public ReadOnlyHashSet(HashSet<T> set)
        {
            _set = set;
        }

        public int Count => _set.Count;

        bool ICollection<T>.IsReadOnly => true;

        public bool Contains(T item) => _set.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

        public void ExceptWith(IEnumerable<T> other) => _set.ExceptWith(other);

        public void IntersectWith(IEnumerable<T> other) => _set.IntersectWith(other);

        public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other) => _set.SymmetricExceptWith(other);

        public void UnionWith(IEnumerable<T> other) => _set.UnionWith(other);

        public HashSet<T>.Enumerator GetEnumerator() => _set.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool ISet<T>.Add(T item) => throw new NotSupportedException();

        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        void ICollection<T>.Clear() => throw new NotSupportedException();

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    }
}
