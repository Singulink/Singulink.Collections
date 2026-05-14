namespace Singulink.Collections;

/// <summary>
/// Represents a collection of values associated with a key.
/// </summary>
/// <typeparam name="TKey">The type of the key associated with the collection.</typeparam>
/// <typeparam name="TValue">The type of the values in the collection.</typeparam>
public interface IKeyedCollection<TKey, TValue> : IReadOnlyKeyedCollection<TKey, TValue>, ICollection<TValue>
{
    // Note: Count is redeclared here to prevent ambiguity errors when accessing Count through an IKeyedCollection reference, since both
    // IReadOnlyCollection<TValue> (inherited via IReadOnlyKeyedCollection) and ICollection<TValue> declare a Count property.

    /// <summary>
    /// Gets the number of values in the collection.
    /// </summary>
    new int Count { get; }
}
