using System.Collections.Immutable;

namespace Singulink.Collections;

/// <summary>
/// Provides extension methods for collections.
/// </summary>
public static class CollectionExtensions
{
    // EquatableArray<T> extensions

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> value) => EquatableArray.Create(value);

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> value) => EquatableArray.Create(value);

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this ReadOnlySpan<T> value) => EquatableArray.Create(value);

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this T[] value) => EquatableArray.Create(value);

    // ComparerEquatableArray<T> extensions

    /// <inheritdoc cref="ComparerEquatableArray.Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> ToComparerEquatableArray<T>(this ImmutableArray<T> value, IEqualityComparer<T>? comparer = null) =>
        ComparerEquatableArray.Create(comparer, value);

    /// <inheritdoc cref="ComparerEquatableArray.Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> ToComparerEquatableArray<T>(this IEnumerable<T> value, IEqualityComparer<T>? comparer = null) =>
        ComparerEquatableArray.Create(comparer, value);

    /// <inheritdoc cref="ComparerEquatableArray.Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> ToComparerEquatableArray<T>(this ReadOnlySpan<T> value, IEqualityComparer<T>? comparer = null) =>
        ComparerEquatableArray.Create(comparer, value);

    /// <inheritdoc cref="ComparerEquatableArray.Create{T}(IEqualityComparer{T}?, ImmutableArray{T})" />
    public static ComparerEquatableArray<T> ToComparerEquatableArray<T>(this T[] value, IEqualityComparer<T>? comparer = null) =>
        ComparerEquatableArray.Create(comparer, value);
}
