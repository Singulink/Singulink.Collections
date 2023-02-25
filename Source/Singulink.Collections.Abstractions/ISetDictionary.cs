namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to unique sets of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface ISetDictionary<TKey, TValue> : ICollectionDictionary<TKey, TValue, ISet<TValue>>
{
}