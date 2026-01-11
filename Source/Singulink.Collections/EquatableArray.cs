using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable IDE0303 // Simplify collection initialization

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
    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> value) => Create(value);

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> value) => Create(value);

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this ReadOnlySpan<T> value) => Create(value);
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
    // Stores the hash code, or 0 if not yet calculated.
    // On 64-bit systems, the hash code has 1UL << 32 or'd, which allows the 0 hash code also - whereas on 32-bit 0 is replaced with 1.
    // Note: this field must be accessed carefully to ensure its thread-safety.
    private nint _hashCode;

    // Stores the array - never null.
    // Note: we store the raw array so we can perform volatile operations on it for de-duplicating.
    private T[] _array;

    // Helper to store our (approximate) creation index for de-duplicating wisely:
    // Note: this is not thread-safe, but that's okay - it's just an approximation to help with de-duplication preferring older instances.
    private static nint _staticCreateIndex;
    private nint _createIndex = _staticCreateIndex++;

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
        _array = array == default ? [] : ImmutableCollectionsMarshal.AsArray(array)!;
    }

    /// <summary>
    /// Gets the shared empty instance of the <see cref="EquatableArray{T}"/> class.
    /// </summary>
    public static EquatableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);

    /// <summary>
    /// Gets the underlying immutable array (note: the underlying array is not guaranteed to be the same instance, nor the values have the same instance or
    /// bits as what was originally passed in).
    /// </summary>
    public ImmutableArray<T> UnderlyingArray => ImmutableCollectionsMarshal.AsImmutableArray(_array);

    /// <summary>
    /// Gets a hash code for the equatable array, which is based on value equality, and is cached.
    /// </summary>
    public override int GetHashCode()
    {
        // Check if we have already calculated the hash code - if so, return it:
        nint value = Volatile.Read(ref _hashCode);
        if (value != 0)
            return (int)value;

        // Otherwise, calculate it:
        return CalculateHashCode();
    }

    // Helper to calculate the hash code & store it in a thread-safe way:
    private int CalculateHashCode()
    {
        HashCode hc = default;
        hc.Add(_array.Length);
        foreach (T item in _array)
            hc.Add(item);
        int result = hc.ToHashCode();
        if (IntPtr.Size < 8)
            result += (result == 0) ? 1 : 0;
        Volatile.Write(ref _hashCode, ((nint)1 << 32) | (nint)(nuint)(uint)result);
        return result;
    }

    // Helper to de-duplicate this instance against a new primary instance:
    private void Deduplicate(EquatableArray<T> newPrimary)
    {
        // Note: none of this needs volatile, as it's all immutable anyway, so we just do a best-effort / low-cost de-duplication:
        _array = newPrimary._array;
        _createIndex = newPrimary._createIndex;
    }

    /// <inheritdoc cref="Equals(object?)" />
    public bool Equals([NotNullWhen(true)] EquatableArray<T>? other)
    {
        // Check simple cases:
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        T[] otherArray = other._array;
        T[] array = _array;
        if (array == otherArray)
            return true;
        if (array.Length != otherArray.Length)
            return false;

        // Compare hash codes first for speed (this will catch 99.99+% of non-equal cases):
        if (GetHashCode() != other.GetHashCode())
            return false;

        // Check if all elements are equal:
        for (int i = 0; i < array.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(array[i], otherArray[i]))
                return false;
        }

        // Equal - perform de-duplication if needed (if create index values happen to be equal, we use the hash code of the array instance to break the tie):
        if (_createIndex < other._createIndex)
        {
            other.Deduplicate(this);
        }
        else if (_createIndex > other._createIndex || RuntimeHelpers.GetHashCode(array) > RuntimeHelpers.GetHashCode(otherArray))
        {
            Deduplicate(other);
        }
        else
        {
            other.Deduplicate(this);
        }

        // Return equal:
        return true;
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
    public int Length => _array.Length;

    /// <summary>
    /// Gets a value indicating whether the array is empty.
    /// </summary>
    public bool IsEmpty => _array.Length == 0;

    /// <summary>
    /// Gets the element at the specified index in the equatable array.
    /// </summary>
    public ref readonly T this[int index] => ref _array[index];

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
        set => throw new NotSupportedException("Collection is read-only.");
    }

    /// <inheritdoc />
    T IReadOnlyList<T>.this[int index] => this[index];

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)ImmutableCollectionsMarshal.AsImmutableArray(_array)).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    /// <inheritdoc />
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException("Collection is read-only.");

    /// <inheritdoc />
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException("Collection is read-only.");

    /// <inheritdoc />
    void ICollection<T>.Add(T item) => throw new NotSupportedException("Collection is read-only.");

    /// <inheritdoc />
    void ICollection<T>.Clear() => throw new NotSupportedException("Collection is read-only.");

    /// <inheritdoc />
    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex);

    /// <inheritdoc />
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only.");

    /// <inheritdoc />
    public override string ToString() => ToString(null, null);

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
    public string ToString(IFormatProvider? formatProvider) => ToString(null, formatProvider);

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
    public string ToString(string? elementFormat) => ToString(elementFormat, null);

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
    public string ToString(string? elementFormat, IFormatProvider? formatProvider)
    {
        StringBuilder sb = new();

#if NET
        sb.Append(formatProvider, $"EquatableArray<{typeof(T).Name}>[{_array.Length}] {{ ");
#else
        sb.Append("EquatableArray<");
        sb.Append(typeof(T).Name);
        sb.Append(">[");
        sb.Append(_array.Length.ToString(formatProvider));
        sb.Append("] { ");
#endif

        var array = _array;
        if (array.Length == 0)
        {
            sb.Append('}');
        }
        else
        {
            if (array[0] is IFormattable f0)
            {
                sb.Append(f0.ToString(elementFormat, formatProvider));
            }
            else
            {
                sb.Append(array[0]);
            }

            sb.EnsureCapacity(sb.Length + (2 * array.Length) + 2);

            for (int i = 1; i < array.Length; i++)
            {
                sb.Append(", ");
                if (array[i] is IFormattable fi)
                {
                    sb.Append(fi.ToString(elementFormat, formatProvider));
                }
                else
                {
                    sb.Append(array[i]);
                }
            }

            sb.Append(" }");
        }

        return sb.ToString();
    }

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(format, formatProvider);

    /// <summary>
    /// An equatable array enumerator.
    /// </summary>
    public struct Enumerator
    {
        private ImmutableArray<T>.Enumerator _impl;

        internal Enumerator(EquatableArray<T> array)
        {
            _impl = array.UnderlyingArray.GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator's current element.
        /// </summary>
        public T Current => _impl.Current;

        /// <summary>
        /// Advances the enumerator to the next element of the equatable array, or returns <see langword="false" /> if the end of the collection is reached.
        /// </summary>
        public bool MoveNext() => _impl.MoveNext();
    }

    /// <summary>
    /// Creates a new read-only memory region over this equatable array.
    /// </summary>
    public ReadOnlyMemory<T> AsMemory() => new(_array);

    /// <summary>
    /// Creates a new read-only span over this equatable array.
    /// </summary>
    public ReadOnlySpan<T> AsSpan() => new(_array);

    /// <summary>
    /// Creates a new read-only span over the specified portion of this equatable array.
    /// </summary>
    public ReadOnlySpan<T> AsSpan(int start, int length) => new(_array, start, length);

    /// <summary>
    /// Creates a new read-only span over the specified portion of this equatable array.
    /// </summary>
    public ReadOnlySpan<T> AsSpan(Range range) => range.GetOffsetAndLength(Length) switch {
        var (start, length) => new(_array, start, length),
    };

    /// <summary>
    /// Determines whether the equatable array contains the specified item.
    /// </summary>
    public bool Contains(T item) => UnderlyingArray.Contains(item);

    /// <summary>
    /// Determines whether the equatable array contains the specified item.
    /// </summary>
    public bool Contains(T item, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.Contains(item, equalityComparer);

    /// <summary>
    /// Copies the elements of the equatable array to the specified destination array.
    /// </summary>
    public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length) => UnderlyingArray.CopyTo(sourceIndex, destination, destinationIndex, length);

    /// <summary>
    /// Copies the elements of the equatable array to the specified destination array.
    /// </summary>
    public void CopyTo(T[] destination, int destinationIndex) => UnderlyingArray.CopyTo(destination, destinationIndex);

    /// <summary>
    /// Copies the elements of the equatable array to the specified destination span.
    /// </summary>
    public void CopyTo(Span<T> destination)
    {
#if NET8_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        UnderlyingArray.CopyTo(destination);
#else
        new ReadOnlySpan<T>(_array).CopyTo(destination);
#endif
    }

    /// <summary>
    /// Copies the elements of the equatable array to the specified destination array.
    /// </summary>
    public void CopyTo(T[] destination) => UnderlyingArray.CopyTo(destination);

    /// <summary>
    /// Gets an enumerator for the equatable array.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Returns the first index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int IndexOf(T item) => UnderlyingArray.IndexOf(item);

    /// <summary>
    /// Returns the first index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int IndexOf(T item, int startIndex) => UnderlyingArray.IndexOf(item, startIndex);

    /// <summary>
    /// Returns the first index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int IndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.IndexOf(item, startIndex, equalityComparer);

    /// <summary>
    /// Returns the first index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int IndexOf(T item, int startIndex, int count) => UnderlyingArray.IndexOf(item, startIndex, count);

    /// <summary>
    /// Returns the first index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.IndexOf(item, startIndex, count, equalityComparer);

    /// <summary>
    /// Returns the last index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int LastIndexOf(T item) => UnderlyingArray.LastIndexOf(item);

    /// <summary>
    /// Returns the last index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int LastIndexOf(T item, int startIndex) => UnderlyingArray.LastIndexOf(item, startIndex);

    /// <summary>
    /// Returns the last index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int LastIndexOf(T item, int startIndex, int count) => UnderlyingArray.LastIndexOf(item, startIndex, count);

    /// <summary>
    /// Returns the last index of the specified item in the equatable array, or -1 if not found.
    /// </summary>
    public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.LastIndexOf(item, startIndex, count, equalityComparer);

    /// <summary>
    /// Creates a slice of the equatable array from the specified start index with the specified length.
    /// </summary>
    public EquatableArray<T> Slice(int start, int length)
    {
#if NET8_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        return EquatableArray.Create(UnderlyingArray.Slice(start, length));
#else
        // Do the bounds check:
        var items = _array;
        items.AsSpan(start, length);

        // Create the new result array:

        var array = new T[length];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = items[start + i];
        }

        return EquatableArray.Create(ImmutableCollectionsMarshal.AsImmutableArray(array));
#endif
    }
}
