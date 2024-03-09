using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Represents a read-only collection of two types of values that map between each other in a bidirectional one-to-one relationship. Values on each side of
/// the map must be unique on their respective side.
/// </summary>
/// <typeparam name="TLeft">The type of values on the left side of the map.</typeparam>
/// <typeparam name="TRight">The type of values on the right side of the map.</typeparam>
public interface IReadOnlyMap<TLeft, TRight> : IReadOnlyCollection<KeyValuePair<TLeft, TRight>>
{
    /// <summary>
    /// Gets the right value associated with the specified left value.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The left value was not found.</exception>
    TRight this[TLeft leftValue] { get; }

    /// <inheritdoc cref="IMap{TLeft, TRight}.LeftValues"/>
    IReadOnlyCollection<TLeft> LeftValues { get; }

    /// <inheritdoc cref="IMap{TLeft, TRight}.Reverse"/>
    IReadOnlyMap<TRight, TLeft> Reverse { get; }

    /// <inheritdoc cref="IMap{TLeft, TRight}.RightValues"/>
    IReadOnlyCollection<TRight> RightValues { get; }

    /// <inheritdoc cref="IMap{TLeft, TRight}.Contains(TLeft, TRight)"/>
    bool Contains(TLeft leftValue, TRight rightValue);

    /// <inheritdoc cref="IMap{TLeft, TRight}.ContainsLeft(TLeft)"/>
    bool ContainsLeft(TLeft leftValue);

    /// <inheritdoc cref="IMap{TLeft, TRight}.ContainsRight(TRight)"/>
    bool ContainsRight(TRight rightValue);

    /// <inheritdoc cref="IMap{TLeft, TRight}.TryGetLeftValue(TRight, out TLeft)"/>
    bool TryGetLeftValue(TRight rightValue, [MaybeNullWhen(false)] out TLeft leftValue);

    /// <inheritdoc cref="IMap{TLeft, TRight}.TryGetRightValue(TLeft, out TRight)"/>
    bool TryGetRightValue(TLeft leftValue, [MaybeNullWhen(false)] out TRight rightValue);
}