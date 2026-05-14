using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Singulink.Collections;

/// <summary>
/// Provides extension methods for collections.
/// </summary>
public static class CollectionExtensions
{
    // ConcurrentDictionary extensions

    /// <summary>
    /// Wraps a <see cref="ConcurrentDictionary{TKey, TValue}"/> in an <see cref="IEnumerable{T}"/> that is safe to pass to consumers which may otherwise
    /// corrupt or fail when the dictionary is modified concurrently during enumeration.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to wrap.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> that enumerates the key-value pairs in the dictionary.</returns>
    /// <remarks>
    /// <para>
    /// While directly enumerating a <see cref="ConcurrentDictionary{TKey, TValue}"/> is safe even if it is modified concurrently, many BCL methods that operate
    /// on <see cref="IEnumerable{T}"/> attempt to cast it to <see cref="ICollection{T}"/> (which <see cref="ConcurrentDictionary{TKey, TValue}"/> implements)
    /// as an optimization. That optimization can cause silent corruption or exceptions if the dictionary is modified concurrently.</para>
    /// <para>
    /// Wrapping the dictionary with this method hides the <see cref="ICollection{T}"/> implementation so it can be safely passed to any method that accepts an
    /// <see cref="IEnumerable{T}"/>, such as LINQ operators, collection constructors, etc., without risking corruption or exceptions due to concurrent
    /// modifications.</para>
    /// </remarks>
    public static IEnumerable<KeyValuePair<TKey, TValue>> AsSafeEnumerable<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        foreach (var kvp in dictionary)
            yield return kvp;
    }

    // EquatableArray extensions

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> value) => EquatableArray.Create(value);

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> value) => EquatableArray.Create(value);

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this ReadOnlySpan<T> value) => EquatableArray.Create(value);

    /// <inheritdoc cref="EquatableArray.Create{T}(ImmutableArray{T})" />
    public static EquatableArray<T> ToEquatableArray<T>(this T[] value) => EquatableArray.Create(value);

    // ComparerEquatableArray extensions

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
