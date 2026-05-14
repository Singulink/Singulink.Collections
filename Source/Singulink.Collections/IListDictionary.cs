using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to a keyed list of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public interface IListDictionary<TKey, TValue>
    : ICollectionDictionary<TKey, TValue, IKeyedList<TKey, TValue>>,
      IReadOnlyListDictionary<TKey, TValue>
{
    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.this[TKey]"/>
    new IKeyedList<TKey, TValue> this[TKey key] { get; }

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.ValueCollections"/>
    new IReadOnlyCollection<IKeyedList<TKey, TValue>> ValueCollections { get; }

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.TryGetValues(TKey, out TValueCollection)"/>
    new bool TryGetValues(TKey key, [MaybeNullWhen(false)] out IKeyedList<TKey, TValue> valueCollection);
}