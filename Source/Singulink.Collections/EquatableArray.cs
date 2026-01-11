using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE0303 // Simplify collection initialization
#pragma warning disable CA1305 // Specify IFormatProvider

namespace Singulink.Collections;

/// <summary>
/// Provides methods for creating an <see cref="EquatableArray{T}"/>.
/// </summary>
public static class EquatableArray
{
    /// <summary>
    /// Returns an instance of the <see cref="EquatableArray{T}"/> class from the given collection.
    /// </summary>
    /// <remarks>
    /// Note: values of the collection are assumed to be immutable and interchangeable (that is, the standard equality and hash code contracts must be held, but
    /// also must be consistent indefinitely, and the value you read out of the array is only guaranteed to be equal to the one passed in, not necessarily the
    /// same instance or bits).
    /// </remarks>
    public static EquatableArray<T> Create<T>(ImmutableArray<T> items)
    {
        if (items == default || items.Length == 0)
            return EquatableArray<T>.Empty;

        return new(items);
    }

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> Create<T>(IEnumerable<T> items)
    {
        // Optimize for common simple cases:

        if (items is ImmutableArray<T> immutableArray)
            return Create(immutableArray);

        if (items is EquatableArray<T> equatableArray)
            return equatableArray;

        if (items is ComparerEquatableArray<T> comparerEquatableArray)
            return Create(comparerEquatableArray.UnderlyingArray);

#if NET
        if (items.TryGetNonEnumeratedCount(out int count) && count == 0)
#else
        if (items is ICollection<T> { Count: 0 } or ICollection { Count: 0 })
#endif
            return EquatableArray<T>.Empty;

        // Otherwise, just create from the full collection:

        return new EquatableArray<T>(ImmutableArray.CreateRange(items));
    }

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> Create<T>(params ReadOnlySpan<T> items)
    {
        if (items.Length == 0)
            return EquatableArray<T>.Empty;

#if NET
        var array = GC.AllocateUninitializedArray<T>(items.Length);
#else
        var array = new T[items.Length];
#endif

        items.CopyTo(array);

#if NET8_0_OR_GREATER
        var immutableArray = ImmutableCollectionsMarshal.AsImmutableArray(array);
#else
        var immutableArray = Unsafe.As<T[], ImmutableArray<T>>(ref array);
#endif

        return new EquatableArray<T>(immutableArray);
    }

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> Create<T>(params T[] items) => Create((ReadOnlySpan<T>)items);
}

/// <summary>
/// An immutable array wrapper that provides value-based equality semantics.
/// </summary>
/// <remarks>
/// Note: values of the array are assumed to be immutable and interchangeable (that is, the standard equality and hash code contracts must be held, but also
/// must be consistent indefinitely, and the value you read out of the array is only guaranteed to be equal to the one passed in, not necessarily the same
/// instance or bits).
/// </remarks>
[CollectionBuilder(typeof(EquatableArray), "Create")]
public sealed class EquatableArray<T> : IReadOnlyList<T>, IList<T>, IEquatable<EquatableArray<T>>, IFormattable
{
    // See EquatableArrayImpl for explanation.
    private static nint _staticCreateIndex;

    // Impl field:
    private EquatableArrayImpl<T> _impl;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquatableArray{T}"/> class from the given immutable array.
    /// </summary>
    /// <remarks>
    /// Note: values of the array are assumed to be immutable and interchangeable (that is, the standard equality and hash code contracts must be held, but also
    /// must be consistent indefinitely, and the value you read out of the array is only guaranteed to be equal to the one passed in, not necessarily the same
    /// instance or bits). Note: this constructor does not de-duplicate against existing instances when created (however, it can still later) - it is a quick
    /// constructor that just copies the provided value directly, other than ensuring non-<see langword="null" /> - use methods on <see cref="EquatableArray" />
    /// to get de-duplication.
    /// </remarks>
    public EquatableArray(ImmutableArray<T> array)
    {
        _impl = new(array, ref _staticCreateIndex);
    }

    /// <summary>
    /// Gets the shared empty instance of the <see cref="EquatableArray{T}"/> class.
    /// </summary>
    public static EquatableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);

    /// <summary>
    /// Gets the underlying immutable array (note: the underlying array is not guaranteed to be the same instance, nor the values have the same instance or
    /// bits as what was originally passed in).
    /// </summary>
    public ImmutableArray<T> UnderlyingArray => _impl.UnderlyingArray;

    /// <summary>
    /// Gets a hash code for the equatable array, which is based on value equality, and is cached.
    /// </summary>
    public override int GetHashCode() => _impl.GetHashCode();

    /// <inheritdoc cref="Equals(object?)" />
    public bool Equals([NotNullWhen(true)] EquatableArray<T>? other)
    {
        // Check simple cases:
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        // Call into impl:
        return _impl.Equals(ref other._impl);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as EquatableArray<T>);

    /// <summary>
    /// Value-based equality operator.
    /// </summary>
    public static bool operator ==(EquatableArray<T>? left, EquatableArray<T>? right) => Equals(left, right);

    /// <summary>
    /// Value-based inequality operator.
    /// </summary>
    public static bool operator !=(EquatableArray<T>? left, EquatableArray<T>? right) => !(left == right);

    /// <summary>
    /// Gets a value indicating the number of elements in the array.
    /// </summary>
    public int Length => _impl.Length;

    /// <summary>
    /// Gets a value indicating whether the array is empty.
    /// </summary>
    public bool IsEmpty => _impl.IsEmpty;

    /// <summary>
    /// Gets the element at the specified index in the equatable array.
    /// </summary>
    public ref readonly T this[int index] => ref _impl[index];

    /// <inheritdoc />
    int ICollection<T>.Count => Length;

    /// <inheritdoc />
    int IReadOnlyCollection<T>.Count => Length;

    /// <inheritdoc />
    bool ICollection<T>.IsReadOnly => true;

    /// <inheritdoc />
    T IList<T>.this[int index]
    {
        get => this[index];
        set => EquatableArrayImpl.ThrowReadOnlyException();
    }

    /// <inheritdoc />
    T IReadOnlyList<T>.this[int index] => this[index];

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _impl.IEnumerableTGetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    /// <inheritdoc />
    void IList<T>.Insert(int index, T item) => EquatableArrayImpl.ThrowReadOnlyException();

    /// <inheritdoc />
    void IList<T>.RemoveAt(int index) => EquatableArrayImpl.ThrowReadOnlyException();

    /// <inheritdoc />
    void ICollection<T>.Add(T item) => EquatableArrayImpl.ThrowReadOnlyException();

    /// <inheritdoc />
    void ICollection<T>.Clear() => EquatableArrayImpl.ThrowReadOnlyException();

    /// <inheritdoc />
    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex);

    /// <inheritdoc />
    bool ICollection<T>.Remove(T item) => EquatableArrayImpl.ThrowReadOnlyException<bool>();

    /// <inheritdoc />
    public override string ToString() => _impl.ToString();

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
    public string ToString(IFormatProvider? formatProvider) => _impl.ToString(formatProvider);

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
    public string ToString(string? elementFormat) => _impl.ToString(elementFormat);

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
    public string ToString(string? elementFormat, IFormatProvider? formatProvider) => _impl.ToString(elementFormat, formatProvider);

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(format, formatProvider);

    /// <summary>
    /// An equatable array enumerator.
    /// </summary>
    public struct Enumerator
    {
        private EquatableArrayImpl<T>.Enumerator _impl;

        internal Enumerator(EquatableArray<T> array)
        {
            _impl = array._impl.GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator's current element.
        /// </summary>
        public readonly T Current => _impl.Current;

        /// <summary>
        /// Advances the enumerator to the next element of the equatable array, or returns <see langword="false" /> if the end of the collection is reached.
        /// </summary>
        public bool MoveNext() => _impl.MoveNext();
    }

    /// <summary>
    /// Creates a new read-only memory region over this equatable array.
    /// </summary>
    public ReadOnlyMemory<T> AsMemory() => _impl.AsMemory();

    /// <summary>
    /// Creates a new read-only span over this equatable array.
    /// </summary>
    public ReadOnlySpan<T> AsSpan() => _impl.AsSpan();

    /// <summary>
    /// Creates a new read-only span over the specified portion of this equatable array.
    /// </summary>
    public ReadOnlySpan<T> AsSpan(int start, int length) => _impl.AsSpan(start, length);

    /// <inheritdoc cref="AsSpan(int, int)" />
    public ReadOnlySpan<T> AsSpan(Range range) => _impl.AsSpan(range);

    /// <summary>
    /// Determines whether the equatable array contains the specified item.
    /// </summary>
    public bool Contains(T item) => _impl.Contains(item);

    /// <inheritdoc cref="Contains(T)" />
    public bool Contains(T item, IEqualityComparer<T>? equalityComparer) => _impl.Contains(item, equalityComparer);

    /// <summary>
    /// Copies the elements of the equatable array to the specified destination span.
    /// </summary>
    public void CopyTo(Span<T> destination) => _impl.CopyTo(destination);

    /// <inheritdoc cref="CopyTo(Span{T})" />
    public void CopyTo(T[] destination) => _impl.CopyTo(destination);

    /// <inheritdoc cref="CopyTo(Span{T})" />
    public void CopyTo(T[] destination, int destinationIndex) => _impl.CopyTo(destination, destinationIndex);

    /// <inheritdoc cref="CopyTo(Span{T})" />
    public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length) => _impl.CopyTo(sourceIndex, destination, destinationIndex, length);

    /// <summary>
    /// Gets an enumerator for the equatable array.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Returns the first index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int IndexOf(T item) => _impl.IndexOf(item);

    /// <inheritdoc cref="IndexOf(T)" />
    public int IndexOf(T item, IEqualityComparer<T>? equalityComparer) => _impl.IndexOf(item, equalityComparer);

    /// <inheritdoc cref="IndexOf(T)" />
    public int IndexOf(T item, int startIndex) => _impl.IndexOf(item, startIndex);

    /// <inheritdoc cref="IndexOf(T)" />
    public int IndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer) => _impl.IndexOf(item, startIndex, equalityComparer);

    /// <inheritdoc cref="IndexOf(T)" />
    public int IndexOf(T item, int startIndex, int count) => _impl.IndexOf(item, startIndex, count);

    /// <inheritdoc cref="IndexOf(T)" />
    public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => _impl.IndexOf(item, startIndex, count, equalityComparer);

    /// <summary>
    /// Returns the last index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int LastIndexOf(T item) => _impl.LastIndexOf(item);

    /// <inheritdoc cref="LastIndexOf(T)" />
    public int LastIndexOf(T item, IEqualityComparer<T>? equalityComparer) => _impl.LastIndexOf(item, equalityComparer);

    /// <inheritdoc cref="LastIndexOf(T)" />
    public int LastIndexOf(T item, int startIndex) => _impl.LastIndexOf(item, startIndex);

    /// <inheritdoc cref="LastIndexOf(T)" />
    public int LastIndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer) => _impl.LastIndexOf(item, startIndex, equalityComparer);

    /// <inheritdoc cref="LastIndexOf(T)" />
    public int LastIndexOf(T item, int startIndex, int count) => _impl.LastIndexOf(item, startIndex, count);

    /// <inheritdoc cref="LastIndexOf(T)" />
    public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => _impl.LastIndexOf(item, startIndex, count, equalityComparer);

    /// <summary>
    /// Creates a slice of the equatable array from the specified start index with the specified length.
    /// </summary>
    public EquatableArray<T> Slice(int start, int length) => _impl.Slice(start, length, out bool identical) switch
    {
        _ when identical => this,
        var x => EquatableArray.Create(x),
    };
}
