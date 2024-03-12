using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Provides a read-only wrapper around a <see cref="HashSet{T}"/>.
/// </summary>
public class ReadOnlyHashSet<T> : ISet<T>, IReadOnlySet<T>
{
    /// <summary>
    /// Gets an empty read-only hash set.
    /// </summary>
    public static ReadOnlyHashSet<T> Empty { get; } = new ReadOnlyHashSet<T>([]);

    private HashSet<T> _set;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyHashSet{T}"/> class.
    /// </summary>
    /// <param name="set">The hash set to wrap.</param>
    public ReadOnlyHashSet(HashSet<T> set)
    {
        _set = set;
    }

    /// <summary>
    /// Gets the number of items contained in the set.
    /// </summary>
    public int Count => _set.Count;

    internal HashSet<T> WrappedSet
    {
        get => _set;
        set => _set = value;
    }

    /// <summary>
    /// Determines whether the set contains the specified item.
    /// </summary>
    public bool Contains(T item) => _set.Contains(item);

    /// <summary>
    /// Copies the items of the set to an array.
    /// </summary>
    public void CopyTo(T[] array) => _set.CopyTo(array);

    /// <summary>
    /// Copies the items of the set to an array starting at the specified index.
    /// </summary>
    public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

    /// <summary>
    /// Determines whether the set is a proper subset of the specified collection.
    /// </summary>
    public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

    /// <summary>
    /// Determines whether the set is a proper superset of the specified collection.
    /// </summary>
    public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

    /// <summary>
    /// Determines whether the set is a subset of the specified collection.
    /// </summary>
    public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

    /// <summary>
    /// Determines whether the set is a superset of the specified collection.
    /// </summary>
    public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

    /// <summary>
    /// Determines whether this set and the specified set share any common items.
    /// </summary>
    public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

    /// <summary>
    /// Determines whether this set and the specified collection contain the same items.
    /// </summary>
    public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

#if !NETSTANDARD2_0

    /// <summary>
    /// Searches the set for the given value and returns the equal value, if any.
    /// </summary>
    public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue) => _set.TryGetValue(equalValue, out actualValue);

#endif

    /// <summary>
    /// Returns an enumerator that iterates through the set.
    /// </summary>
    public HashSet<T>.Enumerator GetEnumerator() => _set.GetEnumerator();

    #region Explicit Interface Implementations

    /// <summary>
    /// Gets a value indicating whether this set is read-only. Always returns <see langword="true"/>.
    /// </summary>
    bool ICollection<T>.IsReadOnly => true;

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Not Supported

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    bool ISet<T>.Add(T item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<T>.Add(T item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<T>.Clear() => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

    #endregion
}