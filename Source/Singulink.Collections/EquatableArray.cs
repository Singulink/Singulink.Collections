using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE0303 // Simplify collection initialization

namespace Singulink.Collections;

/// <summary>
/// Provides methods for creating an <see cref="EquatableArray{T}"/>.
/// </summary>
public static class EquatableArray
{
    /// <summary>
    /// Returns an instance of the <see cref="EquatableArray{T}"/> class from the given collection.
    /// Note: values of the collection are assumed to be immutable and interchangeable (that is, the standard equality and hash code contracts must be held,
    /// but also must be consistent indefinitely, and the value you read out of the array is only guaranteed to be equal to the one passed in, not necessarily
    /// the same instance or bits).
    /// </summary>
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
