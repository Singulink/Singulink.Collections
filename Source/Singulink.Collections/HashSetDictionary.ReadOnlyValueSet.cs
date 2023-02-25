using System.Collections;
using System.Runtime.CompilerServices;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <content>
/// Contains the ReadOnlyValueSet implementation for HashSetDictionary.
/// </content>
public partial class HashSetDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a synchronized read-only set of values associcated with a key in a <see cref="HashSetDictionary{TKey, TValue}"/>.
    /// </summary>
    public class ReadOnlyValueSet : ISet<TValue>, IReadOnlySet<TValue>, IEquatable<ReadOnlyValueSet>
    {
#pragma warning disable SA1401 // Fields should be private

        private protected readonly HashSetDictionary<TKey, TValue> _dictionary;
        private protected readonly TKey _key;
        private protected HashSet<TValue> _lastSet;
        private protected ReadOnlyHashSet<TValue>? _transientReadOnlySet;

#pragma warning restore SA1401

        internal ReadOnlyValueSet(HashSetDictionary<TKey, TValue> dictionary, TKey key, HashSet<TValue> lastSet)
        {
            _dictionary = dictionary;
            _key = key;
            _lastSet = lastSet;
        }

        /// <summary>
        /// Gets the key this value set is associated with.
        /// </summary>
        public TKey Key => _key;

        /// <summary>
        /// Gets the dictionary this value set is associate with.
        /// </summary>
        public HashSetDictionary<TKey, TValue> Dictionary => _dictionary;

        /// <summary>
        /// Gets the number of items in this value set. If the count is zero, this value set is detached from <see cref="Dictionary"/>, i.e. calling <see
        /// cref="ContainsKey(TKey)"/> on the dictionary and passing in <see cref="Key"/> as a parameter will return <see langword="false"/>.
        /// When items are added to the value set it is attached (or re-attached) to the dictionary.
        /// </summary>
        public int Count => GetSet().Count;

        internal HashSet<TValue> LastSet => _lastSet;

        /// <summary>
        /// Returns a fast read-only wrapper around the underlying <see cref="HashSet{T}"/> that is only guaranteed to be valid until the values associated with
        /// <see cref="Key"/> in <see cref="Dictionary"/> are modified.
        /// </summary>
        /// <remarks>
        /// <para>References to transient sets should only be held for as long as a series of multiple consequtive read-only operations need to be performed on
        /// them. Once the values associated with <see cref="Key"/> are modified, the transient set's behavior is undefined and it may contain stale
        /// values.</para>
        /// <para>It is not beneficial to use a transient set for a single read operation, but when multipe read operations need to be done consequtively,
        /// slight performance gains may be seen from performing them on a transient read-only set instead of directly on a <see cref="ValueSet"/> or <see
        /// cref="ReadOnlyValueSet"/>, since the transient set does not check if it needs to synchronize with a newly attached value set in the
        /// dictionary.</para>
        /// </remarks>
        public ReadOnlyHashSet<TValue> AsTransient()
        {
            var set = GetSet();

            if (_transientReadOnlySet == null)
                _transientReadOnlySet = new(set);
            else
                _transientReadOnlySet.WrappedSet = set;

            return _transientReadOnlySet;
        }

        /// <summary>
        /// Determines whether the set contains the specified item.
        /// </summary>
        public bool Contains(TValue item) => GetSet().Contains(item);

        /// <summary>
        /// Copies the elements of the set to an array.
        /// </summary>
        public void CopyTo(TValue[] array) => GetSet().CopyTo(array);

        /// <summary>
        /// Copies the elements of the set to an array starting at the specified index.
        /// </summary>
        public void CopyTo(TValue[] array, int arrayIndex) => GetSet().CopyTo(array, arrayIndex);

        /// <summary>
        /// Determines whether the set is a proper subset of the specified collection.
        /// </summary>
        public bool IsProperSubsetOf(IEnumerable<TValue> other) => GetSet().IsProperSubsetOf(other);

        /// <summary>
        /// Determines whether the set is a proper superset of the specified collection.
        /// </summary>
        public bool IsProperSupersetOf(IEnumerable<TValue> other) => GetSet().IsProperSupersetOf(other);

        /// <summary>
        /// Determines whether the set is a subset of the specified collection.
        /// </summary>
        public bool IsSubsetOf(IEnumerable<TValue> other) => GetSet().IsSubsetOf(other);

        /// <summary>
        /// Determines whether the set is a superset of the specified collection.
        /// </summary>
        public bool IsSupersetOf(IEnumerable<TValue> other) => GetSet().IsSupersetOf(other);

        /// <summary>
        /// Determines whether this set and the specified set share any common elements.
        /// </summary>
        public bool Overlaps(IEnumerable<TValue> other) => GetSet().Overlaps(other);

        /// <summary>
        /// Determines whether this set and the specified collection contain the same elements.
        /// </summary>
        public bool SetEquals(IEnumerable<TValue> other) => GetSet().SetEquals(other);

        /// <summary>
        /// Returns an enumerator that iterates through the underlying <see cref="HashSet{T}"/>.
        /// </summary>
        public Enumerator GetEnumerator() => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected HashSet<TValue> GetSet()
        {
            return _lastSet.Count > 0 ? _lastSet : UpdateAndGetValues();

            [MethodImpl(MethodImplOptions.NoInlining)]
            HashSet<TValue> UpdateAndGetValues() => _dictionary.TryGetValues(_key, out var valueSet) ? _lastSet = valueSet._lastSet : _lastSet;
        }

        #region Equality and Hash Code

        /// <summary>
        /// Determines whether two value sets are equal. Value sets are considered equal if they point to the same <see cref="Dictionary"/> with the same <see
        /// cref="Key"/>.
        /// </summary>
        public static bool operator ==(ReadOnlyValueSet? x, ReadOnlyValueSet? y)
        {
            if (x is null)
                return y is null;

            return x.Equals(y);
        }

        /// <summary>
        /// Determines whether two value sets are not equal. Value sets are considered equal if they point to the same <see cref="Dictionary"/> with the same
        /// <see cref="Key"/>.
        /// </summary>
        public static bool operator !=(ReadOnlyValueSet? x, ReadOnlyValueSet? y) => !(x == y);

        /// <summary>
        /// Determines whether this value set is equal to another value set. Value sets are considered equal if they point to the same <see
        /// cref="Dictionary"/> with the same <see cref="Key"/>.
        /// </summary>
        public bool Equals(ReadOnlyValueSet? other)
        {
            return other != null && GetType() == other.GetType() && _dictionary == other._dictionary && _dictionary.KeyComparer.Equals(_key, other._key);
        }

        /// <summary>
        /// Determines whether this value set is equal to another object. Value sets are considered equal if they point to the same <see
        /// cref="Dictionary"/> and <see cref="Key"/>.
        /// </summary>
        public override bool Equals(object? obj) => Equals(obj as ReadOnlyValueSet);

        /// <summary>
        /// Returns a hash code for this value set.
        /// </summary>
        public override int GetHashCode() => (_dictionary, _dictionary.KeyComparer.GetHashCode(_key)).GetHashCode();

        #endregion

        #region Explicit Interface Implementations

        /// <summary>
        /// Gets a value indicating whether the set is read-only. Always returns <see langword="true"/>.
        /// </summary>
        bool ICollection<TValue>.IsReadOnly => true;

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Not Supported

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        bool ISet<TValue>.Add(TValue item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ISet<TValue>.ExceptWith(IEnumerable<TValue> other) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ISet<TValue>.IntersectWith(IEnumerable<TValue> other) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ISet<TValue>.SymmetricExceptWith(IEnumerable<TValue> other) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ISet<TValue>.UnionWith(IEnumerable<TValue> other) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        void ICollection<TValue>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">This operation is not supported.</exception>
        bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

        #endregion

        /// <summary>
        /// Enumerates the elements of a <see cref="ReadOnlyValueSet"/> or <see cref="ValueSet"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly ReadOnlyValueSet _valueSet;
            private readonly HashSet<TValue> _set;
            private HashSet<TValue>.Enumerator _setEnumerator;

            internal Enumerator(ReadOnlyValueSet valueSet)
            {
                _valueSet = valueSet;
                _set = valueSet.GetSet();
                _setEnumerator = _set.GetEnumerator();
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public TValue Current => _setEnumerator.Current;

            /// <inheritdoc cref="Current"/>
            object? IEnumerator.Current => Current;

            /// <summary>
            /// Releases all resources used by this enumerator.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            public bool MoveNext()
            {
                if (_set.Count == 0 && _set != _valueSet.GetSet())
                    Throw.EnumerationCollectionChanged();

                return _setEnumerator.MoveNext();
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            /// <exception cref="NotSupportedException">This operation is not supported.</exception>
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}