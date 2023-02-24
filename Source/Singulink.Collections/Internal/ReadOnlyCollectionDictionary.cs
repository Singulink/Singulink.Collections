using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Singulink.Collections.Utilities;

namespace Singulink.Collections.Internal;

#pragma warning disable SA1402 // File may only contain a single type

internal sealed class ReadOnlyCollectionDictionary<TKey, TValue, TValueCollection> :
    ReadOnlyCollectionDictionary<TKey, TValue, TValueCollection, IReadOnlyCollection<TValue>>,
    IReadOnlyCollectionDictionary<TKey, TValue>
    where TValueCollection : class, ICollection<TValue>
{
    public ReadOnlyCollectionDictionary(ICollectionDictionary<TKey, TValue, TValueCollection> dictionary) : base(dictionary)
    {
    }
}

internal abstract class ReadOnlyCollectionDictionary<TKey, TValue, TValueCollectionIn, TValueCollectionOut> :
    IReadOnlyCollectionDictionary<TKey, TValue, TValueCollectionOut>,
    IReadOnlyDictionary<TKey, TValueCollectionOut>,
    ICollection<KeyValuePair<TKey, TValueCollectionOut>>
    where TValueCollectionIn : class, ICollection<TValue>
    where TValueCollectionOut : class, IReadOnlyCollection<TValue>
{
    private readonly ICollectionDictionary<TKey, TValue, TValueCollectionIn> _dictionary;
    private ValueCollectionCollection? _valueCollections;

    protected ReadOnlyCollectionDictionary(ICollectionDictionary<TKey, TValue, TValueCollectionIn> dictionary)
    {
        _dictionary = dictionary;
    }

    public TValueCollectionOut this[TKey key] => ToOutCollection(_dictionary[key]);

    public int Count => _dictionary.Count;

    public IReadOnlyCollection<TKey> Keys => _dictionary.Keys;

    public int ValueCount => _dictionary.ValueCount;

    public IReadOnlyCollection<TValueCollectionOut> ValueCollections => _valueCollections ??= new(_dictionary);

    public IReadOnlyCollection<TValue> Values => _dictionary.Values;

    internal ICollectionDictionary<TKey, TValue, TValueCollectionIn> WrappedDictionary => _dictionary;

    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out TValueCollectionOut valueCollection)
    {
        if (_dictionary.TryGetValues(key, out var c))
        {
            valueCollection = ToOutCollection(c);
            return true;
        }
        else
        {
            valueCollection = null;
            return false;
        }
    }

    public bool Contains(TKey key, TValue value) => _dictionary.Contains(key, value);

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool ContainsValue(TValue value) => _dictionary.ContainsValue(value);

    public int GetValueCount(TKey key) => _dictionary.GetValueCount(key);

    public IEnumerator<KeyValuePair<TKey, TValueCollectionOut>> GetEnumerator()
    {
        foreach (var kvp in _dictionary)
            yield return new(kvp.Key, ToOutCollection(kvp.Value));
    }

    private static TValueCollectionOut ToOutCollection(TValueCollectionIn inCollection)
    {
        if (inCollection is IReadOnlyCollectionProvider<TValue> provider)
            return (TValueCollectionOut)provider.GetReadOnlyCollection();

        return (TValueCollectionOut)(object)inCollection;
    }

    #region Explicit Interface Implementations

    TValueCollectionOut IReadOnlyDictionary<TKey, TValueCollectionOut>.this[TKey key] => this[key];

    bool ICollection<KeyValuePair<TKey, TValueCollectionOut>>.IsReadOnly => true;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValueCollectionOut>.Keys => Keys;

    IEnumerable<TValueCollectionOut> IReadOnlyDictionary<TKey, TValueCollectionOut>.Values => ValueCollections;

    void ICollection<KeyValuePair<TKey, TValueCollectionOut>>.CopyTo(KeyValuePair<TKey, TValueCollectionOut>[] array, int arrayIndex)
    {
        CollectionCopy.CheckParams(Count, array, arrayIndex);

        foreach (var kvp in _dictionary)
            array[arrayIndex++] = new(kvp.Key, ToOutCollection(kvp.Value));
    }

    bool ICollection<KeyValuePair<TKey, TValueCollectionOut>>.Contains(KeyValuePair<TKey, TValueCollectionOut> item)
    {
        return _dictionary.TryGetValues(item.Key, out var c) && c.Equals(item.Value);
    }

    bool IReadOnlyDictionary<TKey, TValueCollectionOut>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValueCollectionOut value) => TryGetValues(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Not Supported

    void ICollection<KeyValuePair<TKey, TValueCollectionOut>>.Add(KeyValuePair<TKey, TValueCollectionOut> item)
    {
        throw new NotSupportedException();
    }

    void ICollection<KeyValuePair<TKey, TValueCollectionOut>>.Clear()
    {
        throw new NotSupportedException();
    }

    bool ICollection<KeyValuePair<TKey, TValueCollectionOut>>.Remove(KeyValuePair<TKey, TValueCollectionOut> item)
    {
        throw new NotSupportedException();
    }

    #endregion

    internal sealed class ValueCollectionCollection : ICollection<TValueCollectionOut>, IReadOnlyCollection<TValueCollectionOut>
    {
        private readonly ICollectionDictionary<TKey, TValue, TValueCollectionIn> _dictionary;

        internal ValueCollectionCollection(ICollectionDictionary<TKey, TValue, TValueCollectionIn> dictionary)
        {
            _dictionary = dictionary;
        }

        public int Count => _dictionary.Count;

        public bool IsReadOnly => true;

        public bool Contains(TValueCollectionOut item) => item is TValueCollectionIn c && _dictionary.ValueCollections.Contains(c);

        public void CopyTo(TValueCollectionOut[] array, int arrayIndex)
        {
            CollectionCopy.CheckParams(Count, array, arrayIndex);

            foreach (var valueCollection in _dictionary.ValueCollections)
                array[arrayIndex++] = ToOutCollection(valueCollection);
        }

        public IEnumerator<TValueCollectionOut> GetEnumerator()
        {
            foreach (var c in _dictionary.ValueCollections)
                yield return ToOutCollection(c);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region Not Supported

        public void Add(TValueCollectionOut item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Remove(TValueCollectionOut item) => throw new NotSupportedException();

        #endregion
    }
}