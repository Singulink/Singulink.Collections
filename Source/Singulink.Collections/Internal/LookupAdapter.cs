namespace Singulink.Collections.Internal;

/// <summary>
/// A live <see cref="ILookup{TKey, TElement}"/> view over a read-only collection dictionary.
/// </summary>
internal sealed partial class LookupAdapter<TKey, TValue, TValueCollection> : ILookup<TKey, TValue>
    where TValueCollection : class, IReadOnlyKeyedCollection<TKey, TValue>
{
    private readonly IReadOnlyCollectionDictionary<TKey, TValue, TValueCollection> _dictionary;

    public LookupAdapter(IReadOnlyCollectionDictionary<TKey, TValue, TValueCollection> dictionary)
    {
        _dictionary = dictionary;
    }

    public int Count => _dictionary.Count;

    public IEnumerable<TValue> this[TKey key] => _dictionary.TryGetValues(key, out var c) ? c : Enumerable.Empty<TValue>();

    public bool Contains(TKey key) => _dictionary.ContainsKey(key);

    public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
    {
        foreach (var c in _dictionary.ValueCollections)
            yield return c;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
