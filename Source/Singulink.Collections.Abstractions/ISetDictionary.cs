namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to unique sets of values per key.
/// </summary>
public interface ISetDictionary<TKey, TValue> : ICollectionDictionary<TKey, TValue, ISet<TValue>>
{
}