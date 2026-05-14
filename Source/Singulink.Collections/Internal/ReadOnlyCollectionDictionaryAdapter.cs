using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections.Internal;

/// <summary>
/// Adapts any <see cref="IReadOnlyCollectionDictionary{TKey, TValue, TFromCollection}"/> to the 1-arg
/// <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/> shortcut form.
/// </summary>
internal sealed partial class ReadOnlyCollectionDictionaryAdapter<TKey, TValue, TFromCollection> :
    IReadOnlyCollectionDictionary<TKey, TValue>
    where TFromCollection : class, IReadOnlyKeyedCollection<TKey, TValue>
{
    private readonly IReadOnlyCollectionDictionary<TKey, TValue, TFromCollection> _dictionary;
    private ValueCollectionView? _valueCollections;

    public ReadOnlyCollectionDictionaryAdapter(IReadOnlyCollectionDictionary<TKey, TValue, TFromCollection> dictionary)
    {
        _dictionary = dictionary;
    }

    internal IReadOnlyCollectionDictionary<TKey, TValue, TFromCollection> WrappedDictionary => _dictionary;

    public IReadOnlyKeyedCollection<TKey, TValue> this[TKey key] => _dictionary[key];

    public int Count => _dictionary.Count;

    public IReadOnlyCollection<TKey> Keys => _dictionary.Keys;

    public int ValueCount => _dictionary.ValueCount;

    public IReadOnlyCollection<IReadOnlyKeyedCollection<TKey, TValue>> ValueCollections => _valueCollections ??= new(_dictionary);

    public IReadOnlyCollection<TValue> Values => _dictionary.Values;

    public bool Contains(TKey key, TValue value) => _dictionary.Contains(key, value);

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool ContainsValue(TValue value) => _dictionary.ContainsValue(value);

    public int GetValueCount(TKey key) => _dictionary.GetValueCount(key);

    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out IReadOnlyKeyedCollection<TKey, TValue> valueCollection)
    {
        bool result = _dictionary.TryGetValues(key, out var c);
        valueCollection = c;
        return result;
    }

    private sealed partial class ValueCollectionView : IReadOnlyCollection<IReadOnlyKeyedCollection<TKey, TValue>>
    {
        private readonly IReadOnlyCollectionDictionary<TKey, TValue, TFromCollection> _dictionary;

        public ValueCollectionView(IReadOnlyCollectionDictionary<TKey, TValue, TFromCollection> dictionary)
        {
            _dictionary = dictionary;
        }

        public int Count => _dictionary.Count;

        public IEnumerator<IReadOnlyKeyedCollection<TKey, TValue>> GetEnumerator()
        {
            foreach (var c in _dictionary.ValueCollections)
                yield return c;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
