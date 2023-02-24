namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to a list of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface IListDictionary<TKey, TValue> : ICollectionDictionary<TKey, TValue, IList<TValue>>
{
}