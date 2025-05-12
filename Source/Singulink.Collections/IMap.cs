using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Represents a collection of two types of values that map between each other in a bidirectional one-to-one relationship. Values on each side of the map
/// must be unique on their respective side.
/// </summary>
/// <typeparam name="TLeft">The type of values on the left side of the map.</typeparam>
/// <typeparam name="TRight">The type of values on the right side of the map.</typeparam>
public interface IMap<TLeft, TRight> : ICollection<KeyValuePair<TLeft, TRight>>
{
    /// <summary>
    /// Gets or sets the right value associated with the specified left value.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The property is being retrieved and the left value was not found.</exception>
    /// <exception cref="ArgumentException">The property is being set and the right value provided is already assigned to a different left value.</exception>
    TRight this[TLeft leftValue] { get; set; }

    /// <summary>
    /// Gets the values on the left side of the map.
    /// </summary>
    IReadOnlyCollection<TLeft> LeftValues { get; }

    /// <summary>
    /// Gets the reverse map where the left and right side are flipped.
    /// </summary>
    IMap<TRight, TLeft> Reverse { get; }

    /// <summary>
    /// Gets the values on the right side of the map.
    /// </summary>
    IReadOnlyCollection<TRight> RightValues { get; }

    /// <summary>
    /// Adds an association to map between the specified left and right value.
    /// </summary>
    void Add(TLeft leftValue, TRight rightValue);

    /// <summary>
    /// Determines whether this map contains an association between the specified left and right value.
    /// </summary>
    bool Contains(TLeft leftValue, TRight rightValue);

    /// <summary>
    /// Determines whether this map contains the specified left value.
    /// </summary>
    /// <param name="leftValue">The left value to locate.</param>
    /// <returns><see langword="true"/> if this map contains the specified left value, otherwise <see langword="false"/>.</returns>
    bool ContainsLeft(TLeft leftValue);

    /// <summary>
    /// Determines whether this map contains the specified right value.
    /// </summary>
    /// <param name="rightValue">The right value to locate.</param>
    /// <returns><see langword="true"/> if this map contains the specified right value, otherwise <see langword="false"/>.</returns>
    bool ContainsRight(TRight rightValue);

    /// <summary>
    /// Removes an association from the map between the specified left and right value if they currently map to each other.
    /// </summary>
    /// <returns><see langword="true"/> if removal was successful, otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// If the left and right values do not map to each other then no changes are made to the map and the removal does not succeed.
    /// </remarks>
    bool Remove(TLeft leftValue, TRight rightValue);

    /// <summary>
    /// Removes an association from the map given the specified left value.
    /// </summary>
    /// <returns><see langword="true"/> if the association with the given left value is successfully found and removed, otherwise <see
    /// langword="false"/>.</returns>
    bool RemoveLeft(TLeft leftValue);

    /// <summary>
    /// Removes an association from the map given the specified right value.
    /// </summary>
    /// <returns><see langword="true"/> if the association with the given right value is successfully found and removed, otherwise <see
    /// langword="false"/>.</returns>
    bool RemoveRight(TRight rightValue);

    /// <summary>
    /// Sets an association in the map between the specified left and right value, overriding any existing associations these values had.
    /// </summary>
    /// <remarks>
    /// This method is functionally equivalent to removing any existing associations for the left and right values and then adding the new association, so it is
    /// guaranteed to succeed.
    /// </remarks>
#pragma warning disable CA1716 // Identifiers should not match keywords
    void Set(TLeft leftValue, TRight rightValue);
#pragma warning restore CA1716 // Identifiers should not match keywords

    /// <summary>
    /// Attempts to add an association to map between the specified left and right value.
    /// </summary>
    /// <returns><see langword="true"/> if the association was added, otherwise <see langword="false"/> if the left or right value was already present in the
    /// map.</returns>
    bool TryAdd(TLeft leftValue, TRight rightValue);

    /// <summary>
    /// Gets the right value associated with the specified left value.
    /// </summary>
    /// <param name="rightValue">The right value to look up in the map.</param>
    /// <param name="leftValue">When this method returns, contains the left value associated with the specified right value, if the right value is found;
    /// otherwise, the default value for the type of the left value parameter.</param>
    /// <returns><see langword="true"/> if the map contains the specified right value, otherwise <see langword="false"/>.</returns>
    bool TryGetLeftValue(TRight rightValue, [MaybeNullWhen(false)] out TLeft leftValue);

    /// <summary>
    /// Gets the right value associated with the specified left value.
    /// </summary>
    /// <param name="leftValue">The left value to look up in the map.</param>
    /// <param name="rightValue">When this method returns, contains the right value associated with the specified left value, if the left value is found;
    /// otherwise, the default value for the type of the right value parameter.</param>
    /// <returns><see langword="true"/> if the map contains the specified left value, otherwise <see langword="false"/>.</returns>
    bool TryGetRightValue(TLeft leftValue, [MaybeNullWhen(false)] out TRight rightValue);
}