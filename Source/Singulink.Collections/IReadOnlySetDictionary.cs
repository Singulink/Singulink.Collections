namespace Singulink.Collections;

/// <summary>
/// Represents a read-only collection of keys mapped to a unique read-only set of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface IReadOnlySetDictionary<TKey, TValue> : IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlySet<TValue>>
{
}