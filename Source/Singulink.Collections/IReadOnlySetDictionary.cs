namespace Singulink.Collections;

/// <summary>
/// Represents a read-only collection of keys mapped to a read-only keyed set of unique values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface IReadOnlySetDictionary<TKey, TValue> : IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedSet<TKey, TValue>>
{
}