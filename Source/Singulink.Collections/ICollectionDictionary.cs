using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to a keyed collection of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface ICollectionDictionary<TKey, TValue>
    : ICollectionDictionary<TKey, TValue, IKeyedCollection<TKey, TValue>>,
      IReadOnlyCollectionDictionary<TKey, TValue>
{
    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.this[TKey]"/>
    new IKeyedCollection<TKey, TValue> this[TKey key] { get; }

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.ValueCollections"/>
    new IReadOnlyCollection<IKeyedCollection<TKey, TValue>> ValueCollections { get; }

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.TryGetValues(TKey, out TValueCollection)"/>
    new bool TryGetValues(TKey key, [MaybeNullWhen(false)] out IKeyedCollection<TKey, TValue> valueCollection);
}

/// <summary>
/// Represents a collection of keys mapped to a keyed collection of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
/// <typeparam name="TValueCollection">The type of the keyed value collection associated with each key.</typeparam>
public interface ICollectionDictionary<TKey, TValue, TValueCollection>
    : IReadOnlyCollectionDictionary<TKey, TValue, TValueCollection>
    where TValueCollection : class, IKeyedCollection<TKey, TValue>
{
    /// <summary>
    /// Gets the keyed value collection associated with the specified key. If the key is not found then a new value collection is returned which can be used
    /// to add values to the key or to monitor when items are added to the key.
    /// </summary>
    /// <remarks>
    /// <para>Empty value collections, such as new collections returned using this indexer when the key is not found, are not part of the dictionary until
    /// items are added to them. When a value collection becomes empty again, it is removed from the dictionary. Value collections stay synchronized with
    /// their dictionary to always reflect the values associated with their key inside the dictionary.</para>
    /// </remarks>
    new TValueCollection this[TKey key] { get; }

    /// <summary>
    /// Removes all keys and values from the dictionary and associated value collections.
    /// </summary>
    void Clear();

    /// <summary>
    /// Removes the values in the collection associated with the specified key and removes the key from the dictionary.
    /// </summary>
    /// <returns><see langword="true"/> if the key was found and removed, otherwise <see langword="false"/>.</returns>
    bool Clear(TKey key);
}