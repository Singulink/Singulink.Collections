using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Singulink.Collections.Utilities;

namespace Singulink.Collections.Internal;

#pragma warning disable SA1402 // File may only contain a single type

internal sealed class ReadOnlyCollectionDictionaryWrapper<TKey, TValue, TValueCollection> :
    ReadOnlyCollectionDictionaryWrapper<TKey, TValue, TValueCollection, IReadOnlyCollection<TValue>>,
    IReadOnlyCollectionDictionary<TKey, TValue>
    where TValueCollection : class, IReadOnlyCollection<TValue>
{
    public ReadOnlyCollectionDictionaryWrapper(IReadOnlyCollectionDictionary<TKey, TValue, TValueCollection> dictionary) : base(dictionary)
    {
    }
}

internal abstract class ReadOnlyCollectionDictionaryWrapper<TKey, TValue, TValueCollectionIn, TValueCollectionOut> :
    IReadOnlyCollectionDictionary<TKey, TValue, TValueCollectionOut>,
    IReadOnlyDictionary<TKey, TValueCollectionOut>,
    ICollection<KeyValuePair<TKey, TValueCollectionOut>>
    where TValueCollectionIn : class, TValueCollectionOut
    where TValueCollectionOut : class, IReadOnlyCollection<TValue>
{
    private readonly IReadOnlyCollectionDictionary<TKey, TValue, TValueCollectionIn> _dictionary;

    public ReadOnlyCollectionDictionaryWrapper(IReadOnlyCollectionDictionary<TKey, TValue, TValueCollectionIn> dictionary)
    {
        _dictionary = dictionary;
    }

    public TValueCollectionOut this[TKey key] => _dictionary[key];

    public int Count => _dictionary.Count;

    public IReadOnlyCollection<TKey> Keys => _dictionary.Keys;

    public int ValueCount => _dictionary.ValueCount;

    public IReadOnlyCollection<TValueCollectionOut> ValueCollections => _dictionary.ValueCollections;

    public IReadOnlyCollection<TValue> Values => _dictionary.Values;

    internal IReadOnlyCollectionDictionary<TKey, TValue, TValueCollectionIn> WrappedDictionary => _dictionary;

    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out TValueCollectionOut valueCollection)
    {
        bool result = _dictionary.TryGetValues(key, out var c);
        valueCollection = c;
        return result;
    }

    public bool Contains(TKey key, TValue value) => _dictionary.Contains(key, value);

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool ContainsValue(TValue value) => _dictionary.ContainsValue(value);

    public int GetValueCount(TKey key) => _dictionary.GetValueCount(key);

    public IEnumerator<KeyValuePair<TKey, TValueCollectionOut>> GetEnumerator()
    {
        foreach (var kvp in _dictionary)
            yield return new(kvp.Key, kvp.Value);
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
            array[arrayIndex++] = new(kvp.Key, kvp.Value);
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
}