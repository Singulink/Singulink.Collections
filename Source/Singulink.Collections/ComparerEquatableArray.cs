using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE0303 // Simplify collection initialization
#pragma warning disable CA1305 // Specify IFormatProvider

namespace Singulink.Collections;

/// <summary>
/// Provides methods for creating an <see cref="ComparerEquatableArray{T}"/>.
/// </summary>
public static class ComparerEquatableArray
{
    /// <summary>
    /// Returns an instance of the <see cref="ComparerEquatableArray{T}"/> class from the given collection with the default comparer.
    /// </summary>
    /// <remarks>
    /// Note: values of the collection are assumed to be immutable and interchangeable (that is, the standard equality and hash code contracts must be held, but
    /// also must be consistent indefinitely, and the value you read out of the array is only guaranteed to be equal to the one passed in, not necessarily the
    /// same instance or bits).
    /// </remarks>
    public static ComparerEquatableArray<T> Create<T>(ImmutableArray<T> items) => Create((IEqualityComparer<T>)null, items);

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})" />
    public static ComparerEquatableArray<T> Create<T>(IEnumerable<T> items) => Create((IEqualityComparer<T>)null, items);

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})" />
    public static ComparerEquatableArray<T> Create<T>(params ReadOnlySpan<T> items) => Create(null, items);

    /// <inheritdoc cref="Create{T}(ImmutableArray{T})" />
    public static ComparerEquatableArray<T> Create<T>(params T[] items) => Create((IEqualityComparer<T>)null, items);

    /// <summary>
    /// Returns an instance of the <see cref="ComparerEquatableArray{T}"/> class from the given collection with the specified comparer.
    /// </summary>
    /// <remarks>
    /// <para>Note: values of the collection are assumed to be immutable and interchangeable (that is, the standard equality and hash code contracts must be held, but
    /// also must be consistent indefinitely, and the value you read out of the array is only guaranteed to be equal to the one passed in, not necessarily the
    /// same instance or bits).</para>
    /// <para>
    /// Note: the values for the comparer are compared by reference equality, but with <see langword="null" /> considered equivalent to
    /// <see cref="EqualityComparer{T}.Default" />.</para>
    /// </remarks>
    public static ComparerEquatableArray<T> Create<T>(IEqualityComparer<T>? comparer, ImmutableArray<T> items)
    {
        if ((comparer == null || comparer == EqualityComparer<T>.Default) && (items == default || items.Length == 0))
            return ComparerEquatableArray<T>.Empty;

        return new(items, comparer);
    }

    /// <inheritdoc cref="Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> Create<T>(IEqualityComparer<T>? comparer, IEnumerable<T> items)
    {
        // Optimize for common simple cases:

        if (items is ImmutableArray<T> immutableArray)
            return Create(comparer, immutableArray);

        if (items is ComparerEquatableArray<T> comparerEquatableArray)
        {
            if (comparerEquatableArray.Comparer == (comparer ?? EqualityComparer<T>.Default))
                return comparerEquatableArray;
            else
                return Create(comparer, comparerEquatableArray.UnderlyingArray);
        }

        if (items is EquatableArray<T> equatableArray)
        {
            return Create(comparer, equatableArray.UnderlyingArray);
        }

        if (
            (comparer == null || comparer == EqualityComparer<T>.Default) &&
#if NET
            items.TryGetNonEnumeratedCount(out int count) && count == 0)
#else
            items is ICollection<T> { Count: 0 } or ICollection { Count: 0 })
#endif
        {
            return ComparerEquatableArray<T>.Empty;
        }

        // Otherwise, just create from the full collection:
        return new ComparerEquatableArray<T>(ImmutableArray.CreateRange(items), comparer);
    }

    /// <inheritdoc cref="Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> Create<T>(IEqualityComparer<T>? comparer, params ReadOnlySpan<T> items)
    {
        if (items.Length == 0 && (comparer == null || comparer == EqualityComparer<T>.Default))
            return ComparerEquatableArray<T>.Empty;

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

        return new ComparerEquatableArray<T>(immutableArray, comparer);
    }

    /// <inheritdoc cref="Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> Create<T>(IEqualityComparer<T>? comparer, params T[] items) => Create(comparer, (ReadOnlySpan<T>)items);

    /// <inheritdoc cref="Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> ToComparerEquatableArray<T>(this ImmutableArray<T> value, IEqualityComparer<T>? comparer = null) => Create(comparer, value);

    /// <inheritdoc cref="Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> value, IEqualityComparer<T>? comparer = null) => Create(comparer, value);

    /// <inheritdoc cref="Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> ToEquatableArray<T>(this ReadOnlySpan<T> value, IEqualityComparer<T>? comparer = null) => Create(comparer, value);
}

/// <summary>
/// An immutable array wrapper that provides value-based equality semantics with a custom comparer.
/// </summary>
/// <remarks>
/// Note: values of the array are assumed to be immutable and interchangeable (that is, the standard equality and hash code contracts must be held, but also
/// must be consistent indefinitely, and the value you read out of the array is only guaranteed to be equal to the one passed in, not necessarily the same
/// instance or bits).
/// </remarks>
[CollectionBuilder(typeof(ComparerEquatableArray), "Create")]
public sealed class ComparerEquatableArray<T> : IReadOnlyList<T>, IList<T>, IEquatable<ComparerEquatableArray<T>>, IFormattable
{
    // See EquatableArrayImpl for explanation.
    private static nint _staticCreateIndex;

    // Comparer field:
    private readonly IEqualityComparer<T>? _comparer;

    // Impl field:
    private EquatableArrayImpl<T> _impl;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComparerEquatableArray{T}"/> class from the given immutable array and comparer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note: values of the array are assumed to be immutable and interchangeable (that is, the standard equality and hash code contracts must be held, but also
    /// must be consistent indefinitely, and the value you read out of the array is only guaranteed to be equal to the one passed in, not necessarily the same
    /// instance or bits). Note: this constructor does not de-duplicate against existing instances when created (however, it can still later) - it is a quick
    /// constructor that just copies the provided value directly, other than ensuring non-<see langword="null" /> - use methods on
    /// <see cref="ComparerEquatableArray" /> to get de-duplication.</para>
    /// <para>
    /// Note: the values for the comparer are compared by reference equality, but with <see langword="null" /> considered equivalent to
    /// <see cref="EqualityComparer{T}.Default" />.</para>
    /// </remarks>
    public ComparerEquatableArray(ImmutableArray<T> array, IEqualityComparer<T>? comparer = null)
    {
        _impl = new(array, ref _staticCreateIndex);
        _comparer = comparer == EqualityComparer<T>.Default ? null : comparer;
    }

    /// <summary>
    /// Gets the shared empty instance of the <see cref="ComparerEquatableArray{T}"/> class with the default comparer.
    /// </summary>
    public static ComparerEquatableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);

    /// <inheritdoc cref="EquatableArray{T}.UnderlyingArray" />
    public ImmutableArray<T> UnderlyingArray => _impl.UnderlyingArray;

    /// <summary>
    /// Gets the comparer used for value-based equality.
    /// </summary>
    public IEqualityComparer<T> Comparer => _comparer ?? EqualityComparer<T>.Default;

    /// <inheritdoc cref="EquatableArray{T}.UnderlyingArray" />
    public override int GetHashCode() => _impl.GetHashCode(_comparer);

    /// <inheritdoc cref="Equals(object?)" />
    public bool Equals([NotNullWhen(true)] ComparerEquatableArray<T>? other)
    {
        // Check simple cases:
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (_comparer != other._comparer)
            return false;

        // Call into impl:
        return _impl.Equals(ref other._impl, _comparer);
    }

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><see langword="true" /> if the specified object is equal to the current object; otherwise, <see langword="false" />.</returns>
    /// <remarks>Returns <see langword="false" /> if the two instances of <see cref="ComparerEquatableArray{T}"/> have different comparers.</remarks>
    public override bool Equals(object? obj) => Equals(obj as ComparerEquatableArray<T>);

    /// <summary>
    /// Value-based equality operator.
    /// </summary>
    /// <remarks>Returns <see langword="false" /> if the two instances of <see cref="ComparerEquatableArray{T}"/> have different comparers.</remarks>
    public static bool operator ==(ComparerEquatableArray<T>? left, ComparerEquatableArray<T>? right) => Equals(left, right);

    /// <summary>
    /// Value-based inequality operator.
    /// </summary>
    /// <remarks>Returns <see langword="false" /> if the two instances of <see cref="ComparerEquatableArray{T}"/> have different comparers.</remarks>
    public static bool operator !=(ComparerEquatableArray<T>? left, ComparerEquatableArray<T>? right) => !(left == right);

    /// <inheritdoc cref="EquatableArray{T}.Length" />
    public int Length => _impl.Length;

    /// <inheritdoc cref="EquatableArray{T}.IsEmpty" />
    public bool IsEmpty => _impl.IsEmpty;

    /// <inheritdoc cref="EquatableArray{T}.this[int]" />
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

    /// <inheritdoc cref="EquatableArray{T}.ToString(IFormatProvider?)" />
    public string ToString(IFormatProvider? formatProvider) => _impl.ToString(formatProvider);

    /// <inheritdoc cref="EquatableArray{T}.ToString(string?)" />
    public string ToString(string? elementFormat) => _impl.ToString(elementFormat);

    /// <inheritdoc cref="EquatableArray{T}.ToString(string?, IFormatProvider?)" />
    public string ToString(string? elementFormat, IFormatProvider? formatProvider) => _impl.ToString(elementFormat, formatProvider);

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(format, formatProvider);

    /// <inheritdoc cref="EquatableArray{T}.Enumerator" />
    public struct Enumerator
    {
        private EquatableArrayImpl<T>.Enumerator _impl;

        internal Enumerator(ComparerEquatableArray<T> array)
        {
            _impl = array._impl.GetEnumerator();
        }

        /// <inheritdoc cref="EquatableArray{T}.Enumerator.Current" />
        public readonly T Current => _impl.Current;

        /// <inheritdoc cref="EquatableArray{T}.Enumerator.MoveNext()" />
        public bool MoveNext() => _impl.MoveNext();
    }

    /// <inheritdoc cref="EquatableArray{T}.AsMemory()" />
    public ReadOnlyMemory<T> AsMemory() => _impl.AsMemory();

    /// <inheritdoc cref="EquatableArray{T}.AsSpan()" />
    public ReadOnlySpan<T> AsSpan() => _impl.AsSpan();

    /// <inheritdoc cref="EquatableArray{T}.AsSpan(int, int)" />
    public ReadOnlySpan<T> AsSpan(int start, int length) => _impl.AsSpan(start, length);

    /// <inheritdoc cref="EquatableArray{T}.AsSpan(Range)" />
    public ReadOnlySpan<T> AsSpan(Range range) => _impl.AsSpan(range);

    /// <inheritdoc cref="EquatableArray{T}.Contains(T)" />
    public bool Contains(T item) => _impl.Contains(item, _comparer);

    /// <inheritdoc cref="EquatableArray{T}.Contains(T, IEqualityComparer{T}?)" />
    public bool Contains(T item, IEqualityComparer<T>? equalityComparer) => _impl.Contains(item, equalityComparer ?? _comparer);

    /// <inheritdoc cref="EquatableArray{T}.CopyTo(Span{T})" />
    public void CopyTo(Span<T> destination) => _impl.CopyTo(destination);

    /// <inheritdoc cref="EquatableArray{T}.CopyTo(T[])" />
    public void CopyTo(T[] destination) => _impl.CopyTo(destination);

    /// <inheritdoc cref="EquatableArray{T}.CopyTo(T[], int)" />
    public void CopyTo(T[] destination, int destinationIndex) => _impl.CopyTo(destination, destinationIndex);

    /// <inheritdoc cref="EquatableArray{T}.CopyTo(int, T[], int, int)" />
    public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length) => _impl.CopyTo(sourceIndex, destination, destinationIndex, length);

    /// <inheritdoc cref="EquatableArray{T}.GetEnumerator()" />
    public Enumerator GetEnumerator() => new(this);

    /// <inheritdoc cref="EquatableArray{T}.IndexOf(T)" />
    public int IndexOf(T item) => _impl.IndexOf(item, _comparer);

    /// <inheritdoc cref="EquatableArray{T}.IndexOf(T, IEqualityComparer{T}?)" />
    public int IndexOf(T item, IEqualityComparer<T>? equalityComparer) => _impl.IndexOf(item, equalityComparer ?? _comparer);

    /// <inheritdoc cref="EquatableArray{T}.IndexOf(T, int)" />
    public int IndexOf(T item, int startIndex) => _impl.IndexOf(item, startIndex, _comparer);

    /// <inheritdoc cref="EquatableArray{T}.IndexOf(T, int, IEqualityComparer{T}?)" />
    public int IndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer) => _impl.IndexOf(item, startIndex, equalityComparer ?? _comparer);

    /// <inheritdoc cref="EquatableArray{T}.IndexOf(T, int, int)" />
    public int IndexOf(T item, int startIndex, int count) => _impl.IndexOf(item, startIndex, count, _comparer);

    /// <inheritdoc cref="EquatableArray{T}.IndexOf(T, int, int, IEqualityComparer{T}?)" />
    public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => _impl.IndexOf(item, startIndex, count, equalityComparer ?? _comparer);

    /// <inheritdoc cref="EquatableArray{T}.LastIndexOf(T)" />
    public int LastIndexOf(T item) => _impl.LastIndexOf(item, _comparer);

    /// <inheritdoc cref="EquatableArray{T}.LastIndexOf(T, IEqualityComparer{T}?)" />
    public int LastIndexOf(T item, IEqualityComparer<T>? equalityComparer) => _impl.LastIndexOf(item, equalityComparer ?? _comparer);

    /// <inheritdoc cref="EquatableArray{T}.LastIndexOf(T, int)" />
    public int LastIndexOf(T item, int startIndex) => _impl.LastIndexOf(item, startIndex, _comparer);

    /// <inheritdoc cref="EquatableArray{T}.LastIndexOf(T, int, IEqualityComparer{T}?)" />
    public int LastIndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer) => _impl.LastIndexOf(item, startIndex, equalityComparer ?? _comparer);

    /// <inheritdoc cref="EquatableArray{T}.LastIndexOf(T, int, int)" />
    public int LastIndexOf(T item, int startIndex, int count) => _impl.LastIndexOf(item, startIndex, count, _comparer);

    /// <inheritdoc cref="EquatableArray{T}.LastIndexOf(T, int, int, IEqualityComparer{T}?)" />
    public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => _impl.LastIndexOf(item, startIndex, count, equalityComparer ?? _comparer);

    /// <inheritdoc cref="EquatableArray{T}.Slice(int, int)" />
    public ComparerEquatableArray<T> Slice(int start, int length) => _impl.Slice(start, length, out bool identical) switch
    {
        _ when identical => this,
        var x => ComparerEquatableArray.Create(_comparer, x),
    };

    /// <summary>
    /// Returns an <see cref="ComparerEquatableArray{T}"/> instance with the specified comparer over the current values.
    /// </summary>
    public ComparerEquatableArray<T> WithComparer(IEqualityComparer<T>? comparer)
    {
        if (comparer == EqualityComparer<T>.Default)
            comparer = null;

        if (comparer == _comparer)
            return this;

        return ComparerEquatableArray.Create(comparer, UnderlyingArray);
    }
}
