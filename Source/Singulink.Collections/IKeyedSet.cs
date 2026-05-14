namespace Singulink.Collections;

/// <summary>
/// Represents a set of unique values associated with a key.
/// </summary>
/// <typeparam name="TKey">The type of the key associated with the set.</typeparam>
/// <typeparam name="TValue">The type of the values in the set.</typeparam>
public interface IKeyedSet<TKey, TValue> : IReadOnlyKeyedSet<TKey, TValue>, IKeyedCollection<TKey, TValue>, ISet<TValue>
{
    /// <inheritdoc cref="IKeyedCollection{TKey, TValue}.Count"/>
    new int Count { get; }
}
