namespace Singulink.Collections.Internal;

/// <summary>
/// Snapshot <see cref="ILookup{TKey, TElement}"/> implementation built directly from pre-grouped key/value-collection pairs, avoiding
/// the per-element rehashing that <see cref="Enumerable.ToLookup{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey})"/> performs.
/// </summary>
internal sealed class SnapshotLookup<TKey, TValue> : ILookup<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, Grouping> _groupings;

    public SnapshotLookup(IReadOnlyCollection<IReadOnlyKeyedCollection<TKey, TValue>> source, IEqualityComparer<TKey>? keyComparer = null)
    {
        _groupings = new Dictionary<TKey, Grouping>(source.Count, keyComparer);

        foreach (var c in source)
        {
            var values = new TValue[c.Count];
            int i = 0;

            foreach (var v in c)
                values[i++] = v;

            _groupings.Add(c.Key, new Grouping(c.Key, values));
        }
    }

    public int Count => _groupings.Count;

    public IEnumerable<TValue> this[TKey key] => _groupings.TryGetValue(key, out var g) ? g : Enumerable.Empty<TValue>();

    public bool Contains(TKey key) => _groupings.ContainsKey(key);

    public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
    {
        foreach (var g in _groupings.Values)
            yield return g;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Grouping : IGrouping<TKey, TValue>
    {
        private readonly TValue[] _values;

        public Grouping(TKey key, TValue[] values)
        {
            Key = key;
            _values = values;
        }

        public TKey Key { get; }

        public IEnumerator<TValue> GetEnumerator() => ((IEnumerable<TValue>)_values).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _values.GetEnumerator();
    }
}
