using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to a list of values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public partial class ListDictionary<TKey, TValue> :
    IListDictionary<TKey, TValue>
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

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.Keys"/>
    public KeyCollection Keys => _keys ??= new KeyCollection(this);

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.ValueCount"/>
    public int ValueCount => _valueCount;

    /// <summary>
    /// Gets a collection containing the value lists in the dictionary.
    /// </summary>
    public ValueListCollection ValueLists => _valueLists ??= new ValueListCollection(this);

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.Values"/>
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

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.Contains(TKey, TValue)"/>
    public bool Contains(TKey key, TValue value)
    {
        if (_lookup.TryGetValue(key, out var valueList))
        {
            DebugValid(valueList);
            return valueList.LastList.Contains(value);
        }

        return false;
    }

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.ContainsKey(TKey)"/>
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

    /// <summary>
    /// Creates a snapshot <see cref="ILookup{TKey, TElement}"/> by copying the current contents of the dictionary using its <see cref="KeyComparer"/>.
    /// Subsequent changes to the dictionary are not reflected in the returned lookup.
    /// </summary>
    public ILookup<TKey, TValue> ToLookup() => new Internal.SnapshotLookup<TKey, TValue>(ValueLists, KeyComparer);

    /// <inheritdoc cref="IReadOnlyCollectionDictionary{TKey, TValue, TValueCollection}.GetValueCount(TKey)"/>
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
    /// <returns>The current capacity of the dictionary.</returns>
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

    partial void DebugValueCount();

    static partial void DebugValid(ValueList valueList);

#if DEBUG

    partial void DebugValueCount() => Debug.Assert(_valueCount == _lookup.Values.Sum(v => v.Count), "incorrect value count");

    static partial void DebugValid(ValueList valueList) => Debug.Assert(valueList.Count > 0, "empty value list");

#endif

    #region Explicit Interface Implementations

    /// <inheritdoc cref="this[TKey]"/>
    IKeyedList<TKey, TValue> IListDictionary<TKey, TValue>.this[TKey key] => this[key];

    /// <inheritdoc cref="this[TKey]"/>
    IKeyedList<TKey, TValue> ICollectionDictionary<TKey, TValue, IKeyedList<TKey, TValue>>.this[TKey key] => this[key];

    /// <inheritdoc cref="this[TKey]"/>
    IKeyedList<TKey, TValue> IReadOnlyCollectionDictionary<TKey, TValue, IKeyedList<TKey, TValue>>.this[TKey key] => this[key];

    /// <inheritdoc cref="this[TKey]"/>
    IReadOnlyKeyedList<TKey, TValue> IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedList<TKey, TValue>>.this[TKey key] => this[key];

    /// <inheritdoc cref="Keys"/>
    IReadOnlyCollection<TKey> IReadOnlyCollectionDictionary<TKey, TValue, IKeyedList<TKey, TValue>>.Keys => Keys;

    /// <inheritdoc cref="Keys"/>
    IReadOnlyCollection<TKey> IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedList<TKey, TValue>>.Keys => Keys;

    /// <inheritdoc cref="Values"/>
    IReadOnlyCollection<TValue> IReadOnlyCollectionDictionary<TKey, TValue, IKeyedList<TKey, TValue>>.Values => Values;

    /// <inheritdoc cref="Values"/>
    IReadOnlyCollection<TValue> IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedList<TKey, TValue>>.Values => Values;

    /// <inheritdoc cref="ValueLists"/>
    IReadOnlyCollection<IKeyedList<TKey, TValue>> IListDictionary<TKey, TValue>.ValueCollections => ValueLists;

    /// <inheritdoc />
    IReadOnlyCollection<IKeyedList<TKey, TValue>> IReadOnlyCollectionDictionary<TKey, TValue, IKeyedList<TKey, TValue>>.ValueCollections => ValueLists;

    /// <inheritdoc />
    IReadOnlyCollection<IReadOnlyKeyedList<TKey, TValue>> IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedList<TKey, TValue>>.ValueCollections => ValueLists;

    /// <inheritdoc />
    bool IListDictionary<TKey, TValue>.TryGetValues(TKey key, [MaybeNullWhen(false)] out IKeyedList<TKey, TValue> valueCollection)
    {
        bool result = TryGetValues(key, out var v);
        valueCollection = v;
        return result;
    }

    /// <inheritdoc />
    bool IReadOnlyCollectionDictionary<TKey, TValue, IKeyedList<TKey, TValue>>.TryGetValues(TKey key, [MaybeNullWhen(false)] out IKeyedList<TKey, TValue> valueCollection)
    {
        bool result = TryGetValues(key, out var v);
        valueCollection = v;
        return result;
    }

    /// <inheritdoc />
    bool IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyKeyedList<TKey, TValue>>.TryGetValues(TKey key, [MaybeNullWhen(false)] out IReadOnlyKeyedList<TKey, TValue> valueCollection)
    {
        bool result = TryGetValues(key, out var v);
        valueCollection = v;
        return result;
    }

    #endregion
}