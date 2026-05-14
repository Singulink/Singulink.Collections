using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections.Internal;

/// <summary>
/// Adapts an <see cref="IReadOnlySetDictionary{TKey, TValue}"/> (which a mutable
/// <see cref="ISetDictionary{TKey, TValue}"/> implements) so that the result exposes only the read-only surface
/// and cannot be downcast back to the mutable interface.
/// </summary>
internal sealed partial class ReadOnlySetDictionaryAdapter<TKey, TValue> : IReadOnlySetDictionary<TKey, TValue>
{
    private readonly IReadOnlySetDictionary<TKey, TValue> _dictionary;
    private ValueCollectionView? _valueCollections;

    public ReadOnlySetDictionaryAdapter(IReadOnlySetDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary;
    }

    internal IReadOnlySetDictionary<TKey, TValue> WrappedDictionary => _dictionary;

    public IReadOnlyKeyedSet<TKey, TValue> this[TKey key] => _dictionary[key];

    public int Count => _dictionary.Count;

    public IReadOnlyCollection<TKey> Keys => _dictionary.Keys;

    public int ValueCount => _dictionary.ValueCount;

    public IReadOnlyCollection<IReadOnlyKeyedSet<TKey, TValue>> ValueCollections => _valueCollections ??= new(_dictionary);

    public IReadOnlyCollection<TValue> Values => _dictionary.Values;

    public bool Contains(TKey key, TValue value) => _dictionary.Contains(key, value);

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool ContainsValue(TValue value) => _dictionary.ContainsValue(value);

    public int GetValueCount(TKey key) => _dictionary.GetValueCount(key);

    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out IReadOnlyKeyedSet<TKey, TValue> valueCollection)
    {
        bool result = _dictionary.TryGetValues(key, out var c);
        valueCollection = c;
        return result;
    }

    private sealed partial class ValueCollectionView : IReadOnlyCollection<IReadOnlyKeyedSet<TKey, TValue>>
    {
        private readonly IReadOnlySetDictionary<TKey, TValue> _dictionary;

        public ValueCollectionView(IReadOnlySetDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public int Count => _dictionary.Count;

        public IEnumerator<IReadOnlyKeyedSet<TKey, TValue>> GetEnumerator()
        {
            foreach (var c in _dictionary.ValueCollections)
                yield return c;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
