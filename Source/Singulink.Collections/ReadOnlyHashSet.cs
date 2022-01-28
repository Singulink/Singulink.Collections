using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Singulink.Collections;

#pragma warning disable SA1629 // Documentation text should end with a period

/// <summary>
/// Provides a read-only wrapper around a <see cref="HashSet{T}"/>.
/// </summary>
public class ReadOnlyHashSet<T>
#if NET
        : ISet<T>, IReadOnlySet<T>
#else
        : ISet<T>, IReadOnlyCollection<T>
#endif
{
    /// <summary>
    /// Gets an empty read-only hash set.
    /// </summary>
    public static ReadOnlyHashSet<T> Empty { get; } = new ReadOnlyHashSet<T>(new HashSet<T>());

    private readonly HashSet<T> _set;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyHashSet{T}"/> class.
    /// </summary>
    /// <param name="set">The hash set to wrap.</param>
    public ReadOnlyHashSet(HashSet<T> set)
    {
        _set = set;
    }

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.Count"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.Count"/>
    public int Count => _set.Count;

    /// <inheritdoc/>
    bool ICollection<T>.IsReadOnly => true;

    internal HashSet<T> WrappedSet => _set;

    /// <summary>
    /// Returns an <see cref="IEqualityComparer"/> object that can be used for equality testing of a <see cref="ReadOnlyHashSet{T}"/> object.
    /// </summary>
    public static IEqualityComparer<ReadOnlyHashSet<T>> CreateSetComparer() => new ReadOnlySetEqualityComparer(HashSet<T>.CreateSetComparer());

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.Contains(T)"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.Contains(T)"/>
    public bool Contains(T item) => _set.Contains(item);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.CopyTo(T[])"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.CopyTo(T[])"/>
    public void CopyTo(T[] array) => _set.CopyTo(array);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.CopyTo(T[], int)"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.CopyTo(T[], int)"/>
    public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.CopyTo(T[], int, int)"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.CopyTo(T[], int, int)"/>
    public void CopyTo(T[] array, int arrayIndex, int count) => _set.CopyTo(array, arrayIndex, count);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.EnsureCapacity(int)"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.EnsureCapacity(int)"/>
    public int EnsureCapacity(int capacity) => _set.EnsureCapacity(capacity);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.ExceptWith(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.ExceptWith(IEnumerable{T})"/>
    public void ExceptWith(IEnumerable<T> other) => _set.ExceptWith(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.IntersectWith(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.IntersectWith(IEnumerable{T})"/>
    public void IntersectWith(IEnumerable<T> other) => _set.IntersectWith(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.IsProperSubsetOf(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.IsProperSubsetOf(IEnumerable{T})"/>
    public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.IsProperSupersetOf(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.IsProperSupersetOf(IEnumerable{T})"/>
    public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.IsSubsetOf(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.IsSubsetOf(IEnumerable{T})"/>
    public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.IsSupersetOf(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.IsSupersetOf(IEnumerable{T})"/>
    public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.Overlaps(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.Overlaps(IEnumerable{T})"/>
    public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.SetEquals(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.SetEquals(IEnumerable{T})"/>
    public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.SymmetricExceptWith(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.SymmetricExceptWith(IEnumerable{T})"/>
    public void SymmetricExceptWith(IEnumerable<T> other) => _set.SymmetricExceptWith(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.TrimExcess"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.TrimExcess"/>
    public void TrimExcess() => _set.TrimExcess();

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.TryGetValue(T, out T)"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.TryGetValue(T, out T)"/>
    public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue) => _set.TryGetValue(equalValue, out actualValue);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.UnionWith(IEnumerable{T})"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.UnionWith(IEnumerable{T})"/>
    public void UnionWith(IEnumerable<T> other) => _set.UnionWith(other);

    /// <summary>
    /// (<see cref="HashSet{T}"/> wrapper)
    /// <inheritdoc cref="HashSet{T}.GetEnumerator"/>
    /// </summary>
    /// <inheritdoc cref="HashSet{T}.GetEnumerator"/>
    public HashSet<T>.Enumerator GetEnumerator() => _set.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Not supported.
    /// </summary>
    bool ISet<T>.Add(T item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    void ICollection<T>.Add(T item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    void ICollection<T>.Clear() => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

    private sealed class ReadOnlySetEqualityComparer : EqualityComparer<ReadOnlyHashSet<T>>
    {
        private readonly IEqualityComparer<HashSet<T>> _setComparer;

        public ReadOnlySetEqualityComparer(IEqualityComparer<HashSet<T>> setComparer)
        {
            _setComparer = setComparer;
        }

        public override bool Equals(ReadOnlyHashSet<T>? x, ReadOnlyHashSet<T>? y) => _setComparer.Equals(x?._set, y?._set);

        public override int GetHashCode(ReadOnlyHashSet<T> obj)
        {
            return _setComparer.GetHashCode(obj?._set!);
        }
    }
}