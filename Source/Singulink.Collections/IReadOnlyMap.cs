using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Represents a read-only bidirectional one-to-one mapping between two sets of values. Values on each side of the map must be unique within their respective
/// side.
/// </summary>
/// <typeparam name="TLeft">The type of values on the left side of the map.</typeparam>
/// <typeparam name="TRight">The type of values on the right side of the map.</typeparam>
public interface IReadOnlyMap<TLeft, TRight> : IReadOnlyCollection<KeyValuePair<TLeft, TRight>>
{
    /// <summary>
    /// Gets the right value associated with the specified left value.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The specified left value was not found in the map.</exception>
    TRight this[TLeft leftValue] { get; }

    /// <summary>
    /// Gets a collection containing the values on the left side of the map.
    /// </summary>
    IReadOnlyCollection<TLeft> LeftValues { get; }

    /// <summary>
    /// Gets a view of this map with the left and right sides swapped.
    /// </summary>
    IReadOnlyMap<TRight, TLeft> Reverse { get; }

    /// <summary>
    /// Gets a collection containing the values on the right side of the map.
    /// </summary>
    IReadOnlyCollection<TRight> RightValues { get; }

    /// <summary>
    /// Determines whether the map contains an association between the specified left and right values.
    /// </summary>
    /// <returns><see langword="true"/> if the specified left value is associated with the specified right value, otherwise <see langword="false"/>.</returns>
    bool Contains(TLeft leftValue, TRight rightValue);

    /// <summary>
    /// Determines whether the map contains the specified left value.
    /// </summary>
    /// <param name="leftValue">The left value to locate.</param>
    /// <returns><see langword="true"/> if the map contains the specified left value, otherwise <see langword="false"/>.</returns>
    bool ContainsLeft(TLeft leftValue);

    /// <summary>
    /// Determines whether the map contains the specified right value.
    /// </summary>
    /// <param name="rightValue">The right value to locate.</param>
    /// <returns><see langword="true"/> if the map contains the specified right value, otherwise <see langword="false"/>.</returns>
    bool ContainsRight(TRight rightValue);

    /// <summary>
    /// Attempts to get the left value associated with the specified right value.
    /// </summary>
    /// <param name="rightValue">The right value to look up.</param>
    /// <param name="leftValue">When this method returns, contains the associated left value if the right value was found; otherwise, the default value for
    /// <typeparamref name="TLeft"/>.</param>
    /// <returns><see langword="true"/> if the map contains the specified right value, otherwise <see langword="false"/>.</returns>
    bool TryGetLeftValue(TRight rightValue, [MaybeNullWhen(false)] out TLeft leftValue);

    /// <summary>
    /// Attempts to get the right value associated with the specified left value.
    /// </summary>
    /// <param name="leftValue">The left value to look up.</param>
    /// <param name="rightValue">When this method returns, contains the associated right value if the left value was found; otherwise, the default value for
    /// <typeparamref name="TRight"/>.</param>
    /// <returns><see langword="true"/> if the map contains the specified left value, otherwise <see langword="false"/>.</returns>
    bool TryGetRightValue(TLeft leftValue, [MaybeNullWhen(false)] out TRight rightValue);
}