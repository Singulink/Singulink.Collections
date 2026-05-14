namespace Singulink.Collections;

/// <summary>
/// Represents a read-only set of unique values associated with a key.
/// </summary>
/// <typeparam name="TKey">The type of the key associated with the set.</typeparam>
/// <typeparam name="TValue">The type of the values in the set.</typeparam>
public interface IReadOnlyKeyedSet<out TKey, TValue> : IReadOnlyKeyedCollection<TKey, TValue>, IReadOnlySet<TValue>
{
}
