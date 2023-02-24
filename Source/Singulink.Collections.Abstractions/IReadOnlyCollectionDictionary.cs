using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}"/>
public interface IReadOnlyCollectionDictionary<TKey, TValue> : IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyCollection<TValue>>
{
}

/// <summary>
/// Represents a read-only collection of keys mapped to a read-only collection of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
/// <typeparam name="TValueCollection">The type of the read-only value collection associated with each key.</typeparam>
public interface IReadOnlyCollectionDictionary<TKey, TValue, TValueCollection> : IReadOnlyCollection<KeyValuePair<TKey, TValueCollection>>
    where TValueCollection : class, IReadOnlyCollection<TValue>
{
    /// <summary>
    /// Gets the value collection associated with the specified key. If the key is not found then a new value collection is returned which can be used to
    /// monitor when items are added to the key.
    /// </summary>
    /// <remarks>
    /// <para>Value collections synchronize with their dictionary to always reflect the values associated with their key inside the dictionary.</para>
    /// </remarks>
    TValueCollection this[TKey key] { get; }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.Keys"/>
    IReadOnlyCollection<TKey> Keys { get; }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.ValueCount"/>
    int ValueCount { get; }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.ValueCollections"/>
    IReadOnlyCollection<TValueCollection> ValueCollections { get; }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.Values"/>
    IReadOnlyCollection<TValue> Values { get; }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.Contains(TKey, TValue)"/>
    bool Contains(TKey key, TValue value);

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.ContainsKey(TKey)"/>
    bool ContainsKey(TKey key);

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.ContainsValue(TValue)"/>
    bool ContainsValue(TValue value);

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.GetValueCount(TKey)"/>
    int GetValueCount(TKey key);

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.TryGetValues(TKey, out TValueCollection)"/>
    bool TryGetValues(TKey key, [MaybeNullWhen(false)] out TValueCollection valueCollection);
}