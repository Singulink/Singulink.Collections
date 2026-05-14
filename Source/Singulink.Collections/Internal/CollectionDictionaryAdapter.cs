using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections.Internal;

/// <summary>
/// Adapts an <see cref="ICollectionDictionary{TKey, TValue, TFromCollection}"/> to the 1-arg
/// <see cref="ICollectionDictionary{TKey, TValue}"/> shortcut form (and <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/>).
/// </summary>
internal sealed partial class CollectionDictionaryAdapter<TKey, TValue, TFromCollection> :
    ICollectionDictionary<TKey, TValue>
    where TFromCollection : class, IKeyedCollection<TKey, TValue>
{
    private readonly ICollectionDictionary<TKey, TValue, TFromCollection> _dictionary;
    private ValueCollectionView? _valueCollections;

    public CollectionDictionaryAdapter(ICollectionDictionary<TKey, TValue, TFromCollection> dictionary)
    {
        _dictionary = dictionary;
    }

    internal ICollectionDictionary<TKey, TValue, TFromCollection> WrappedDictionary => _dictionary;

    public IKeyedCollection<TKey, TValue> this[TKey key] => _dictionary[key];

    public int Count => _dictionary.Count;

    public IReadOnlyCollection<TKey> Keys => _dictionary.Keys;

    public int ValueCount => _dictionary.ValueCount;

    public IReadOnlyCollection<IKeyedCollection<TKey, TValue>> ValueCollections => _valueCollections ??= new(_dictionary);

    public IReadOnlyCollection<TValue> Values => _dictionary.Values;

    public bool Contains(TKey key, TValue value) => _dictionary.Contains(key, value);

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool ContainsValue(TValue value) => _dictionary.ContainsValue(value);

    public int GetValueCount(TKey key) => _dictionary.GetValueCount(key);

    public void Clear() => _dictionary.Clear();

    public bool Clear(TKey key) => _dictionary.Clear(key);

    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out IKeyedCollection<TKey, TValue> valueCollection)
    {
        bool result = _dictionary.TryGetValues(key, out var c);
        valueCollection = c;
        return result;
    }

    #region Explicit Interface Implementations

    IReadOnlyKeyedCollection<TKey, TValue> IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedCollection<TKey, TValue>>.this[TKey key] => _dictionary[key];

    IReadOnlyCollection<IReadOnlyKeyedCollection<TKey, TValue>> IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedCollection<TKey, TValue>>.ValueCollections => ValueCollections;

    bool IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedCollection<TKey, TValue>>.TryGetValues(TKey key, [MaybeNullWhen(false)] out IReadOnlyKeyedCollection<TKey, TValue> valueCollection)
    {
        bool result = _dictionary.TryGetValues(key, out var c);
        valueCollection = c;
        return result;
    }

    #endregion

    private sealed partial class ValueCollectionView : IReadOnlyCollection<IKeyedCollection<TKey, TValue>>
    {
        private readonly ICollectionDictionary<TKey, TValue, TFromCollection> _dictionary;

        public ValueCollectionView(ICollectionDictionary<TKey, TValue, TFromCollection> dictionary)
        {
            _dictionary = dictionary;
        }

        public int Count => _dictionary.Count;

        public IEnumerator<IKeyedCollection<TKey, TValue>> GetEnumerator()
        {
            foreach (var c in _dictionary.ValueCollections)
                yield return c;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
