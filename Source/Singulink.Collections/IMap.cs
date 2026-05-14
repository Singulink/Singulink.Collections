namespace Singulink.Collections;

/// <summary>
/// Represents a bidirectional one-to-one mapping between two sets of values. Values on each side of the map must be unique within their respective side.
/// </summary>
/// <typeparam name="TLeft">The type of values on the left side of the map.</typeparam>
/// <typeparam name="TRight">The type of values on the right side of the map.</typeparam>
public interface IMap<TLeft, TRight> : IReadOnlyMap<TLeft, TRight>, ICollection<KeyValuePair<TLeft, TRight>>
{
    /// <summary>
    /// Gets or sets the right value associated with the specified left value.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The property is being retrieved and the specified left value was not found.</exception>
    /// <exception cref="ArgumentException">The property is being set and the specified right value is already associated with a different left value.</exception>
    new TRight this[TLeft leftValue] { get; set; }

    // Note: Count is redeclared here to prevent ambiguity errors when using the Count property from an IMap reference, since both IReadOnlyCollection and
    // ICollection declare a Count property.

    /// <summary>
    /// Gets the number of associations in the map.
    /// </summary>
    new int Count { get; }

    /// <inheritdoc cref="IReadOnlyMap{TLeft, TRight}.Reverse"/>
    new IMap<TRight, TLeft> Reverse { get; }

    /// <summary>
    /// Adds an association between the specified left and right values to the map.
    /// </summary>
    /// <exception cref="ArgumentException">The specified left or right value is already present in the map.</exception>
    void Add(TLeft leftValue, TRight rightValue);

    /// <summary>
    /// Removes the association between the specified left and right values from the map if they are currently associated with each other.
    /// </summary>
    /// <returns><see langword="true"/> if the association was found and removed, otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// If the specified left and right values are not associated with each other then no changes are made to the map and the method returns <see
    /// langword="false"/>.
    /// </remarks>
    bool Remove(TLeft leftValue, TRight rightValue);

    /// <summary>
    /// Removes the association containing the specified left value from the map.
    /// </summary>
    /// <returns><see langword="true"/> if the association was found and removed, otherwise <see langword="false"/>.</returns>
    bool RemoveLeft(TLeft leftValue);

    /// <summary>
    /// Removes the association containing the specified right value from the map.
    /// </summary>
    /// <returns><see langword="true"/> if the association was found and removed, otherwise <see langword="false"/>.</returns>
    bool RemoveRight(TRight rightValue);

    /// <summary>
    /// Sets an association between the specified left and right values, overriding any existing associations involving either value.
    /// </summary>
    /// <remarks>
    /// This method is functionally equivalent to removing any existing associations involving the specified left or right values and then adding the new
    /// association, so it is guaranteed to succeed.
    /// </remarks>
#pragma warning disable CA1716 // Identifiers should not match keywords
    void Set(TLeft leftValue, TRight rightValue);
#pragma warning restore CA1716

    /// <summary>
    /// Attempts to add an association between the specified left and right values to the map.
    /// </summary>
    /// <returns><see langword="true"/> if the association was added, or <see langword="false"/> if the left or right value was already present in the
    /// map.</returns>
    bool TryAdd(TLeft leftValue, TRight rightValue);
}