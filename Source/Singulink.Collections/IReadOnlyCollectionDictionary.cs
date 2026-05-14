using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Represents a read-only collection of keys mapped to a read-only keyed collection of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface IReadOnlyCollectionDictionary<TKey, TValue> : IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedCollection<TKey, TValue>>
{
}

/// <summary>
/// Represents a read-only collection of keys mapped to a read-only keyed collection of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
/// <typeparam name="TValueCollection">The type of the keyed value collection associated with each key.</typeparam>
public interface IReadOnlyCollectionDictionary<TKey, TValue, TValueCollection>
    where TValueCollection : class, IReadOnlyKeyedCollection<TKey, TValue>
{
    /// <summary>
    /// Gets the keyed value collection associated with the specified key. If the key is not found then a new value collection is returned which can be used
    /// to monitor when items are added to the key.
    /// </summary>
    /// <remarks>
    /// <para>Value collections stay synchronized with their dictionary to always reflect the values associated with their key inside the dictionary.</para>
    /// </remarks>
    TValueCollection this[TKey key] { get; }

    /// <summary>
    /// Gets the number of keys and associated value collections in the dictionary.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    IReadOnlyCollection<TKey> Keys { get; }

    /// <summary>
    /// Gets the total number of values in the dictionary across all keys.
    /// </summary>
    int ValueCount { get; }

    /// <summary>
    /// Gets a collection containing the keyed value collections in the dictionary.
    /// </summary>
    IReadOnlyCollection<TValueCollection> ValueCollections { get; }

    /// <summary>
    /// Gets a collection containing all the values across all the keys in the dictionary.
    /// </summary>
    IReadOnlyCollection<TValue> Values { get; }

    /// <summary>
    /// Determines whether the dictionary contains the specified key associated with the specified value.
    /// </summary>
    /// <returns><see langword="true"/> if the dictionary contains the specified key associated with the specified value, otherwise <see
    /// langword="false"/>.</returns>
    bool Contains(TKey key, TValue value);

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <returns><see langword="true"/> if the dictionary contains the specified key, otherwise <see langword="false"/>.</returns>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Determines whether any of the value collections in the dictionary contain the specified value.
    /// </summary>
    /// <returns><see langword="true"/> if any value collection in the dictionary contains the specified value, otherwise <see langword="false"/>.</returns>
    bool ContainsValue(TValue value);

    /// <summary>
    /// Gets the number of values associated with the specified key, or zero if the key is not present.
    /// </summary>
    int GetValueCount(TKey key);

    /// <summary>
    /// Gets the keyed value collection associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value collection to get.</param>
    /// <param name="valueCollection">When this method returns, contains the keyed value collection associated with the specified key if the key was found;
    /// otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the dictionary contains the specified key, otherwise <see langword="false"/>.</returns>
    bool TryGetValues(TKey key, [MaybeNullWhen(false)] out TValueCollection valueCollection);
}