using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections.Internal;

/// <summary>
/// Adapts an <see cref="IReadOnlyListDictionary{TKey, TValue}"/> (which a mutable
/// <see cref="IListDictionary{TKey, TValue}"/> implements) so that the result exposes only the read-only surface
/// and cannot be downcast back to the mutable interface.
/// </summary>
internal sealed partial class ReadOnlyListDictionaryAdapter<TKey, TValue> : IReadOnlyListDictionary<TKey, TValue>
{
    private readonly IReadOnlyListDictionary<TKey, TValue> _dictionary;
    private ValueCollectionView? _valueCollections;

    public ReadOnlyListDictionaryAdapter(IReadOnlyListDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary;
    }

    internal IReadOnlyListDictionary<TKey, TValue> WrappedDictionary => _dictionary;

    public IReadOnlyKeyedList<TKey, TValue> this[TKey key] => _dictionary[key];

    public int Count => _dictionary.Count;

    public IReadOnlyCollection<TKey> Keys => _dictionary.Keys;

    public int ValueCount => _dictionary.ValueCount;

    public IReadOnlyCollection<IReadOnlyKeyedList<TKey, TValue>> ValueCollections => _valueCollections ??= new(_dictionary);

    public IReadOnlyCollection<TValue> Values => _dictionary.Values;

    public bool Contains(TKey key, TValue value) => _dictionary.Contains(key, value);

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool ContainsValue(TValue value) => _dictionary.ContainsValue(value);

    public int GetValueCount(TKey key) => _dictionary.GetValueCount(key);

    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out IReadOnlyKeyedList<TKey, TValue> valueCollection)
    {
        bool result = _dictionary.TryGetValues(key, out var c);
        valueCollection = c;
        return result;
    }

    private sealed partial class ValueCollectionView : IReadOnlyCollection<IReadOnlyKeyedList<TKey, TValue>>
    {
        private readonly IReadOnlyListDictionary<TKey, TValue> _dictionary;

        public ValueCollectionView(IReadOnlyListDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public int Count => _dictionary.Count;

        public IEnumerator<IReadOnlyKeyedList<TKey, TValue>> GetEnumerator()
        {
            foreach (var c in _dictionary.ValueCollections)
                yield return c;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
