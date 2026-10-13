using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0303 // Simplify collection initialization
#pragma warning disable CA1305 // Specify IFormatProvider

namespace Singulink.Collections;

/// <summary>
/// Provides methods for creating an <see cref="EquatableArray{T}"/>.
/// </summary>
public static class EquatableArray
{
    /// <summary>
    /// Returns an <see cref="EquatableArray{T}"/> containing the specified items.
    /// </summary>
    /// <remarks>
    /// <para>The values in the array are assumed to be immutable and interchangeable. Equal values must stay equal and keep the same hash code indefinitely,
    /// and a value read from the array is only guaranteed to be equal to the one that was passed in, not to be the same instance.</para>
    /// </remarks>
    public static EquatableArray<T> Create<T>(ImmutableArray<T> items)
    {
        if (items == default || items.Length == 0)
            return EquatableArray<T>.Empty;

        return new(items);
    }

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})"/>
    public static EquatableArray<T> Create<T>(IEnumerable<T> items)
    {
        if (items is EquatableArray<T> equatableArray)
            return equatableArray;

        if (items is ComparerEquatableArray<T> comparerEquatableArray)
            return Create(comparerEquatableArray.UnderlyingArray);

        var array = ImmutableArray.CreateRange(items);
        return array.Length is 0 ? EquatableArray<T>.Empty : new EquatableArray<T>(array);
    }

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})"/>
    public static EquatableArray<T> Create<T>(params ReadOnlySpan<T> items)
    {
        if (items.Length is 0)
            return EquatableArray<T>.Empty;

        return new EquatableArray<T>(ImmutableArray.Create(items));
    }

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})"/>
    public static EquatableArray<T> Create<T>(params T[] items) => Create((ReadOnlySpan<T>)items);

    /// <summary>
    /// Returns the shared empty instance of the <see cref="EquatableArray{T}"/> class.
    /// </summary>
    public static EquatableArray<T> Empty<T>() => EquatableArray<T>.Empty;
}

/// <summary>
/// Represents an immutable array with value-based equality semantics.
/// </summary>
/// <remarks>
/// <para>The values in the array are assumed to be immutable and interchangeable. Equal values must stay equal and keep the same hash code indefinitely,
/// and a value read from the array is only guaranteed to be equal to the one that was passed in, not to be the same instance.</para>
/// </remarks>
[CollectionBuilder(typeof(EquatableArray), "Create")]
public sealed class EquatableArray<T> : IReadOnlyList<T>, IList<T>, IEquatable<EquatableArray<T>>, IFormattable
{
    // See EquatableArrayImpl for explanation.
    private static nint _staticCreateIndex;

    // Impl field:
    private EquatableArrayImpl<T> _impl;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquatableArray{T}"/> class that wraps the specified immutable array.
    /// </summary>
    /// <remarks>
    /// <para>The values in the array are assumed to be immutable and interchangeable. Equal values must stay equal and keep the same hash code indefinitely,
    /// and a value read from the array is only guaranteed to be equal to the one that was passed in, not to be the same instance.</para>
    /// <para>This constructor wraps the array directly without de-duplicating it against existing instances, although the instance may still be de-duplicated
    /// later. Use the methods on <see cref="EquatableArray"/> to create de-duplicated instances.</para>
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

    /// <inheritdoc cref="Equals(object?)"/>
    public bool Equals([NotNullWhen(true)] EquatableArray<T>? other)
    {
        // Check simple cases:

        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        // Call into impl
        return _impl.Equals(ref other._impl);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as EquatableArray<T>);

    /// <summary>
    /// Determines whether two arrays are equal.
    /// </summary>
    public static bool operator ==(EquatableArray<T>? left, EquatableArray<T>? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two arrays are not equal.
    /// </summary>
    public static bool operator !=(EquatableArray<T>? left, EquatableArray<T>? right) => !(left == right);

    /// <summary>
    /// Gets the number of elements in the array.
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

    /// <inheritdoc/>
    int ICollection<T>.Count => Length;

    /// <inheritdoc/>
    int IReadOnlyCollection<T>.Count => Length;

    /// <inheritdoc/>
    bool ICollection<T>.IsReadOnly => true;

    /// <inheritdoc/>
    T IList<T>.this[int index]
    {
        get => this[index];
        set => EquatableArrayImpl.ThrowReadOnlyException();
    }

    /// <inheritdoc/>
    T IReadOnlyList<T>.this[int index] => this[index];

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _impl.IEnumerableTGetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    /// <inheritdoc/>
    void IList<T>.Insert(int index, T item) => EquatableArrayImpl.ThrowReadOnlyException();

    /// <inheritdoc/>
    void IList<T>.RemoveAt(int index) => EquatableArrayImpl.ThrowReadOnlyException();

    /// <inheritdoc/>
    void ICollection<T>.Add(T item) => EquatableArrayImpl.ThrowReadOnlyException();

    /// <inheritdoc/>
    void ICollection<T>.Clear() => EquatableArrayImpl.ThrowReadOnlyException();

    /// <inheritdoc/>
    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    bool ICollection<T>.Remove(T item) => EquatableArrayImpl.ThrowReadOnlyException<bool>();

    /// <inheritdoc/>
    public override string ToString() => _impl.ToString();

    /// <summary>
    /// Returns a string representation of the array using the specified format provider to format its elements.
    /// </summary>
    public string ToString(IFormatProvider? formatProvider) => _impl.ToString(formatProvider);

    /// <summary>
    /// Returns a string representation of the array using the specified format to format its elements.
    /// </summary>
    public string ToString(string? elementFormat) => _impl.ToString(elementFormat);

    /// <summary>
    /// Returns a string representation of the array using the specified format and format provider to format its elements.
    /// </summary>
    public string ToString(string? elementFormat, IFormatProvider? formatProvider) => _impl.ToString(elementFormat, formatProvider);

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)"/>
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(format, formatProvider);

    /// <summary>
    /// Enumerates the elements of an <see cref="EquatableArray{T}"/>.
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
        /// Advances the enumerator to the next element of the equatable array, or returns <see langword="false"/> if the end of the collection is reached.
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

    /// <summary>
    /// Creates a new read-only span over the specified range of this equatable array.
    /// </summary>
    public ReadOnlySpan<T> AsSpan(Range range) => _impl.AsSpan(range);

    /// <summary>
    /// Determines whether the equatable array contains the specified item.
    /// </summary>
    public bool Contains(T item) => _impl.Contains(item);

    /// <inheritdoc cref="Contains(T)"/>
    public bool Contains(T item, IEqualityComparer<T>? equalityComparer) => _impl.Contains(item, equalityComparer);

    /// <summary>
    /// Copies the elements of the equatable array to the specified destination span.
    /// </summary>
    public void CopyTo(Span<T> destination) => _impl.CopyTo(destination);

    /// <summary>
    /// Copies the elements of the equatable array to the specified destination array.
    /// </summary>
    public void CopyTo(T[] destination) => _impl.CopyTo(destination);

    /// <summary>
    /// Copies the elements of the equatable array to the specified destination array starting at the specified index of the destination.
    /// </summary>
    public void CopyTo(T[] destination, int destinationIndex) => _impl.CopyTo(destination, destinationIndex);

    /// <summary>
    /// Copies a range of elements of the equatable array to the specified destination array.
    /// </summary>
    /// <param name="sourceIndex">The zero-based index in the equatable array at which copying begins.</param>
    /// <param name="destination">The array to copy the elements to.</param>
    /// <param name="destinationIndex">The zero-based index in the destination array at which storing begins.</param>
    /// <param name="length">The number of elements to copy.</param>
    public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length) => _impl.CopyTo(sourceIndex, destination, destinationIndex, length);

    /// <summary>
    /// Gets an enumerator for the equatable array.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Returns the first index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int IndexOf(T item) => _impl.IndexOf(item);

    /// <inheritdoc cref="IndexOf(T)"/>
    public int IndexOf(T item, IEqualityComparer<T>? equalityComparer) => _impl.IndexOf(item, equalityComparer);

    /// <inheritdoc cref="IndexOf(T)"/>
    public int IndexOf(T item, int startIndex) => _impl.IndexOf(item, startIndex);

    /// <inheritdoc cref="IndexOf(T)"/>
    public int IndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer) => _impl.IndexOf(item, startIndex, equalityComparer);

    /// <inheritdoc cref="IndexOf(T)"/>
    public int IndexOf(T item, int startIndex, int count) => _impl.IndexOf(item, startIndex, count);

    /// <inheritdoc cref="IndexOf(T)"/>
    public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => _impl.IndexOf(item, startIndex, count, equalityComparer);

    /// <summary>
    /// Returns the last index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int LastIndexOf(T item) => _impl.LastIndexOf(item);

    /// <inheritdoc cref="LastIndexOf(T)"/>
    public int LastIndexOf(T item, IEqualityComparer<T>? equalityComparer) => _impl.LastIndexOf(item, equalityComparer);

    /// <inheritdoc cref="LastIndexOf(T)"/>
    public int LastIndexOf(T item, int startIndex) => _impl.LastIndexOf(item, startIndex);

    /// <inheritdoc cref="LastIndexOf(T)"/>
    public int LastIndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer) => _impl.LastIndexOf(item, startIndex, equalityComparer);

    /// <inheritdoc cref="LastIndexOf(T)"/>
    public int LastIndexOf(T item, int startIndex, int count) => _impl.LastIndexOf(item, startIndex, count);

    /// <inheritdoc cref="LastIndexOf(T)"/>
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
