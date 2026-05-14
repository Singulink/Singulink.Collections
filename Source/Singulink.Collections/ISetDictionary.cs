using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to a keyed set of unique values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface ISetDictionary<TKey, TValue>
    : ICollectionDictionary<TKey, TValue, IKeyedSet<TKey, TValue>>,
      IReadOnlySetDictionary<TKey, TValue>
{
    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.this[TKey]"/>
    new IKeyedSet<TKey, TValue> this[TKey key] { get; }

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.ValueCollections"/>
    new IReadOnlyCollection<IKeyedSet<TKey, TValue>> ValueCollections { get; }

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.TryGetValues(TKey, out TValueCollection)"/>
    new bool TryGetValues(TKey key, [MaybeNullWhen(false)] out IKeyedSet<TKey, TValue> valueCollection);
}