using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to a collection of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface ICollectionDictionary<TKey, TValue> : ICollectionDictionary<TKey, TValue, ICollection<TValue>>
{
}

/// <summary>
/// Represents a collection of keys mapped to a collection of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
/// <typeparam name="TValueCollection">The type of the value collection associated with each key.</typeparam>
public interface ICollectionDictionary<TKey, TValue, TValueCollection> : IReadOnlyCollection<KeyValuePair<TKey, TValueCollection>>
    where TValueCollection : class, ICollection<TValue>
{
    /// <summary>
    /// Gets the value collection associated with the specified key. If the key is not found then a new value collection is returned which can be used to add
    /// values to the key or to monitor when items are added to the key.
    /// </summary>
    /// <remarks>
    /// <para>Empty value collections, such as new collections returned using this indexer when the key is not found, are not part of the dictionary until items are
    /// added to them. When the value collection becomes empty again, it is removed from the dictionary. Value collections stay synchronized with their
    /// dictionary to always reflect the values associated with their key inside the dictionary.</para>
    /// </remarks>
    TValueCollection this[TKey key] { get; }

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    IReadOnlyCollection<TKey> Keys { get; }

    /// <summary>
    /// Gets the number of values in the dictionary across all keys.
    /// </summary>
    int ValueCount { get; }

    /// <summary>
    /// Gets a collection containing the value collections in the dictionary.
    /// </summary>
    IReadOnlyCollection<TValueCollection> ValueCollections { get; }

    /// <summary>
    /// Gets a collection containing all the values in the dictionary across all the keys.
    /// </summary>
    IReadOnlyCollection<TValue> Values { get; }

    /// <summary>
    /// Clears all keys and values from the dictionary and associated value collections.
    /// </summary>
    void Clear();

    /// <summary>
    /// Clears the values in the collection associated with the specified key and removes the key from the dictionary.
    /// </summary>
    bool Clear(TKey key);

    /// <summary>
    /// Returns a value indicating whether the specified key and associated value are present in the dictionary.
    /// </summary>
    bool Contains(TKey key, TValue value);

    /// <summary>
    /// Returns a value indicating whether the dictionary contains the specified key.
    /// </summary>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Returns a value indicating whether any of the value collections in the dictionary contain the specified value.
    /// </summary>
    bool ContainsValue(TValue value);

    /// <summary>
    /// Gets the number of values in the dictionary associated with the specified key or 0 if the key is not present.
    /// </summary>
    int GetValueCount(TKey key);

    /// <summary>
    /// Gets the value collection for the specified key or <see langword="null"/> if the key was not found.
    /// </summary>
    /// <returns>A value indicating whether the key was found.</returns>
    bool TryGetValues(TKey key, [MaybeNullWhen(false)] out TValueCollection valueCollection);
}