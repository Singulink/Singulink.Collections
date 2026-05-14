namespace Singulink.Collections;

/// <summary>
/// Represents a read-only collection of keys mapped to a read-only keyed list of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface IReadOnlyListDictionary<TKey, TValue> : IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedList<TKey, TValue>>
{
}