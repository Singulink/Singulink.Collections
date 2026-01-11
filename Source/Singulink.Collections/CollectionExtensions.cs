using System.Collections.Immutable;

namespace Singulink.Collections;

/// <summary>
/// Provides extension methods for collections.
/// </summary>
public static class CollectionExtensions
{
    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> value) => EquatableArray.Create(value);

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> value) => EquatableArray.Create(value);

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this ReadOnlySpan<T> value) => EquatableArray.Create(value);

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this T[] value) => EquatableArray.Create(value);
}
