using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to a list of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public partial class ListDictionary<TKey, TValue> :
    IListDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, IList<TValue>>,
    ICollection<KeyValuePair<TKey, IList<TValue>>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, ValueList> _lookup;

    private KeyCollection? _keys;
    private ValueListCollection? _valueLists;
    private ValueCollection? _values;
    private int _valueCount;
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListDictionary{TKey, TValue}"/> class.
    /// </summary>
    public ListDictionary() : this(0, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListDictionary{TKey, TValue}"/> class with the specified initial capacity for key/value list pairs.
    /// </summary>
    public ListDictionary(int capacity) : this(capacity, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListDictionary{TKey, TValue}"/> class that uses the specified key comparer.
    /// </summary>
    public ListDictionary(IEqualityComparer<TKey>? keyComparer) : this(0, keyComparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListDictionary{TKey, TValue}"/> class with the specified initial capacity for key/value list pairs,
    /// and uses the specified key comparer.
    /// </summary>
    public ListDictionary(int capacity, IEqualityComparer<TKey>? keyComparer)
    {
        _lookup = new(capacity, keyComparer);
    }

    /// <summary>
    /// Gets the value set associated with the specified key. If the key is not found then a new value set is returned which can be used to add values to the
    /// key or to monitor when items are added to the key.
    /// </summary>
    /// <remarks>
    /// <para>Empty value lists, such as new lists returned using this indexer when the key is not found, are not part of the dictionary until items are added to
    /// them. When the value list becomes empty again, it is removed from the dictionary. Value lists stay synchronized with their dictionary to always reflect
    /// the values associated with their key inside the dictionary.</para>
    /// </remarks>
    public ValueList this[TKey key]
    {
        get {
            if (_lookup.TryGetValue(key, out var valueList))
            {
                DebugValid(valueList);
                return valueList;
            }

            return new ValueList(this, key);
        }
    }

    /// <summary>
    /// Gets the number of keys and associated value lists in the dictionary.
    /// </summary>
    public int Count => _lookup.Count;

    /// <summary>
    /// Gets the comparer that is used to determine equality of the keys.
    /// </summary>
    public IEqualityComparer<TKey> KeyComparer => _lookup.Comparer;

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.Keys"/>
    public KeyCollection Keys => _keys ??= new KeyCollection(this);

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.ValueCount"/>
    public int ValueCount => _valueCount;

    /// <summary>
    /// Gets a collection containing the value lists in the dictionary.
    /// </summary>
    public ValueListCollection ValueLists => _valueLists ??= new ValueListCollection(this);

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.Values"/>
    public ValueCollection Values => _values ??= new ValueCollection(this);

    /// <summary>
    /// Clears all keys and values from the dictionary and associated value lists.
    /// </summary>
    public void Clear()
    {
        _version++;

        if (_valueCount > 0)
        {
            foreach (var valueList in _lookup.Values)
            {
                DebugValid(valueList);
                valueList.LastList.Clear();
            }

            _lookup.Clear();
            _valueCount = 0;
        }

        DebugValueCount();
    }

    /// <summary>
    /// Clears the values in the list associated with the specified key and removes the key from the dictionary.
    /// </summary>
    public bool Clear(TKey key)
    {
        if (_lookup.TryGetValue(key, out var valueList))
        {
            DebugValid(valueList);
            var list = valueList.LastList;

            _version++;
            _valueCount -= list.Count;

            list.Clear();
            _lookup.Remove(key);

            DebugValueCount();
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.Contains(TKey, TValue)"/>
    public bool Contains(TKey key, TValue value)
    {
        if (_lookup.TryGetValue(key, out var valueList))
        {
            DebugValid(valueList);
            return valueList.LastList.Contains(value);
        }

        return false;
    }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.ContainsKey(TKey)"/>
    public bool ContainsKey(TKey key) => _lookup.ContainsKey(key);

    /// <summary>
    /// Returns a value indicating whether any of the value lists in the dictionary contain the specified value.
    /// </summary>
    public bool ContainsValue(TValue value)
    {
        foreach (var valueList in _lookup.Values)
        {
            DebugValid(valueList);

            if (valueList.LastList.Contains(value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the value lists in the dictionary.
    /// </summary>
    public ValueListCollection.Enumerator GetEnumerator() => new(this);

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.GetValueCount(TKey)"/>
    public int GetValueCount(TKey key)
    {
        if (_lookup.TryGetValue(key, out var valueList))
        {
            DebugValid(valueList);
            return valueList.LastList.Count;
        }

        return 0;
    }

    /// <summary>
    /// Gets the values for the specified key.
    /// </summary>
    /// <returns>A value indicating whether the key was found.</returns>
    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out ValueList valueList) => _lookup.TryGetValue(key, out valueList);

#if !NETSTANDARD2_0

    /// <summary>
    /// Ensures that the dictionary can hold up to a specified number of key/value list pairs without any further expansion of its backing storage.
    /// </summary>
    /// <param name="capacity">The number of key/value list pairs.</param>
    /// <returns>The currect capacity of the dictionary.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than 0.</exception>
    public int EnsureCapacity(int capacity)
    {
        _version++;
        return _lookup.EnsureCapacity(capacity);
    }

    /// <summary>
    /// Sets the key/value list pair capacity of this dictionary to what it would be if it had been originally initialized with all its entries, and
    /// optionally sets the capacity of each value list to the actual number of values in the list.
    /// </summary>
    /// <param name="trimValueLists"><see langword="true"/> to trim all the value lists as well, or <see langword="false"/> to only trim the
    /// dictionary.</param>
    public void TrimExcess(bool trimValueLists = true)
    {
        _version++;
        _lookup.TrimExcess();

        if (trimValueLists)
        {
            foreach (var valueList in _lookup.Values)
            {
                DebugValid(valueList);
                valueList.LastList.TrimExcess();
            }
        }
    }

    /// <summary>
    /// Sets the key/value list pair capacity of this dictionary to hold up a specified number of entries without any further expansion of its backing
    /// storage, and optionally trims the capacity of each value list to the actual number of values in the list.
    /// </summary>
    /// <param name="dictionaryCapacity">The new key/value list pair capacity.</param>
    /// <param name="trimValueLists"><see langword="true"/> to trim all the value lists as well, or <see langword="false"/> to only trim the
    /// dictionary.</param>
    /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than the number of entries in the dictionary.</exception>
    public void TrimExcess(int dictionaryCapacity, bool trimValueLists = true)
    {
        _version++;
        _lookup.TrimExcess(dictionaryCapacity);

        if (trimValueLists)
        {
            foreach (var valueList in _lookup.Values)
            {
                DebugValid(valueList);
                valueList.LastList.TrimExcess();
            }
        }
    }

#endif

    [Conditional("DEBUG")]
    private void DebugValueCount()
    {
        Debug.Assert(_valueCount == _lookup.Values.Sum(v => v.Count), "incorrect value count");
    }

    [Conditional("DEBUG")]
    private static void DebugValid(ValueList valueList)
    {
        Debug.Assert(valueList.Count > 0, "empty value list");
    }

    #region Explicit Interface Implementations

    /// <inheritdoc cref="this[TKey]"/>
    IList<TValue> ICollectionDictionary<TKey, TValue, IList<TValue>>.this[TKey key] => this[key];

    /// <inheritdoc cref="this[TKey]"/>
    IList<TValue> IReadOnlyDictionary<TKey, IList<TValue>>.this[TKey key] => this[key];

    /// <summary>
    /// Gets a value indicating whether this collection is read-only. Although this dictionary is not read-only, this always returns <see langword="true"/> as
    /// only read operations are supported through the <see cref="ICollection{T}"/> interface.
    /// </summary>
    bool ICollection<KeyValuePair<TKey, IList<TValue>>>.IsReadOnly => true;

    /// <inheritdoc cref="Keys"/>
    IReadOnlyCollection<TKey> ICollectionDictionary<TKey, TValue, IList<TValue>>.Keys => Keys;

    /// <inheritdoc cref="Keys"/>
    IEnumerable<TKey> IReadOnlyDictionary<TKey, IList<TValue>>.Keys => Keys;

    /// <inheritdoc cref="ValueLists"/>
    IReadOnlyCollection<IList<TValue>> ICollectionDictionary<TKey, TValue, IList<TValue>>.ValueCollections => ValueLists;

    /// <inheritdoc cref="ValueLists"/>
    IEnumerable<IList<TValue>> IReadOnlyDictionary<TKey, IList<TValue>>.Values => ValueLists;

    /// <inheritdoc cref="Values"/>
    IReadOnlyCollection<TValue> ICollectionDictionary<TKey, TValue, IList<TValue>>.Values => Values;

    /// <summary>
    /// Gets a value indicating whether the specified key and value list is present in the dictionary.
    /// </summary>
    bool ICollection<KeyValuePair<TKey, IList<TValue>>>.Contains(KeyValuePair<TKey, IList<TValue>> item)
    {
        return item.Value is ValueList valueList && TryGetValues(item.Key, out var exisingValueList) && valueList == exisingValueList;
    }

    /// <summary>
    /// Copies the key and value list pairs to an array starting at the given array index.
    /// </summary>
    void ICollection<KeyValuePair<TKey, IList<TValue>>>.CopyTo(KeyValuePair<TKey, IList<TValue>>[] array, int arrayIndex)
    {
        CollectionCopy.CheckParams(Count, array, arrayIndex);

        foreach (var valueList in this)
            array[arrayIndex++] = new(valueList.Key, valueList);
    }

    /// <inheritdoc cref="TryGetValues(TKey, out ValueList)"/>
    bool ICollectionDictionary<TKey, TValue, IList<TValue>>.TryGetValues(TKey key, [MaybeNullWhen(false)] out IList<TValue> valueCollection)
    {
        bool result = TryGetValues(key, out var c);
        valueCollection = c;
        return result;
    }

    /// <inheritdoc cref="TryGetValues(TKey, out ValueList)"/>
    bool IReadOnlyDictionary<TKey, IList<TValue>>.TryGetValue(TKey key, [MaybeNullWhen(false)] out IList<TValue> value)
    {
        bool result = TryGetValues(key, out var v);
        value = v;
        return result;
    }

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator<KeyValuePair<TKey, IList<TValue>>> IEnumerable<KeyValuePair<TKey, IList<TValue>>>.GetEnumerator()
    {
        foreach (var valueList in this)
            yield return new(valueList.Key, valueList);
    }

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Not Supported

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<KeyValuePair<TKey, IList<TValue>>>.Add(KeyValuePair<TKey, IList<TValue>> item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<KeyValuePair<TKey, IList<TValue>>>.Clear() => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    bool ICollection<KeyValuePair<TKey, IList<TValue>>>.Remove(KeyValuePair<TKey, IList<TValue>> item) => throw new NotSupportedException();

    #endregion
}