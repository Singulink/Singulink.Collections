using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <summary>
/// Represents a collection of keys mapped to a hash set of unique values per key.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public partial class HashSetDictionary<TKey, TValue> :
    ISetDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, ISet<TValue>>,
    ICollection<KeyValuePair<TKey, ISet<TValue>>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, ValueSet> _lookup;
    private readonly IEqualityComparer<TValue>? _valueComparer;

    private KeyCollection? _keys;
    private ValueSetCollection? _valueSets;
    private ValueCollection? _values;
    private int _valueCount;
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashSetDictionary{TKey, TValue}"/> class.
    /// </summary>
    public HashSetDictionary() : this(0, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashSetDictionary{TKey, TValue}"/> class with the specified initial capacity for key/value set pairs.
    /// </summary>
    public HashSetDictionary(int capacity) : this(capacity, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashSetDictionary{TKey, TValue}"/> class that uses the specified key and value comparers.
    /// </summary>
    public HashSetDictionary(IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null) : this(0, keyComparer, valueComparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashSetDictionary{TKey, TValue}"/> class with the specified initial capacity for key/value set pairs,
    /// and uses the specified key and value comparers.
    /// </summary>
    public HashSetDictionary(int capacity, IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null)
    {
        _lookup = new(capacity, keyComparer);
        _valueComparer = valueComparer;
    }

    /// <summary>
    /// Gets the value list associated with the specified key. If the key is not found then a new value list is returned which can be used to add values to the
    /// key or to monitor when items are added to the key.
    /// </summary>
    /// <remarks>
    /// <para>Empty value sets, such as new sets returned using this indexer when the key is not found, are not part of the dictionary until items are added to them.
    /// When the value set becomes empty again, it is removed from the dictionary. Value sets stay synchronized with their dictionary to always reflect the
    /// values associated with their key inside the dictionary.</para>
    /// </remarks>
    public ValueSet this[TKey key]
    {
        get {
            if (_lookup.TryGetValue(key, out var valueSet))
            {
                DebugValid(valueSet);
                return valueSet;
            }

            return new ValueSet(this, key);
        }
    }

    /// <summary>
    /// Gets the number of keys and associated value sets in the dictionary.
    /// </summary>
    public int Count => _lookup.Count;

    /// <summary>
    /// Gets the comparer that is used to determine equality of the keys.
    /// </summary>
    public IEqualityComparer<TKey> KeyComparer => _lookup.Comparer;

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.Keys"/>
    public KeyCollection Keys => _keys ??= new KeyCollection(this);

    /// <summary>
    /// Gets the comparer that is used to determine equality of the values.
    /// </summary>
    public IEqualityComparer<TValue> ValueComparer => _valueComparer ?? EqualityComparer<TValue>.Default;

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.ValueCount"/>
    public int ValueCount => _valueCount;

    /// <summary>
    /// Gets a collection containing the value sets in the dictionary.
    /// </summary>
    public ValueSetCollection ValueSets => _valueSets ??= new ValueSetCollection(this);

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.Values"/>
    public ValueCollection Values => _values ??= new ValueCollection(this);

    /// <summary>
    /// Clears all keys and values from the dictionary and associated value sets.
    /// </summary>
    public void Clear()
    {
        _version++;

        if (_valueCount > 0)
        {
            foreach (var valueSet in _lookup.Values)
            {
                DebugValid(valueSet);
                valueSet.LastSet.Clear();
            }

            _lookup.Clear();
            _valueCount = 0;
        }

        DebugValueCount();
    }

    /// <summary>
    /// Clears the values in the set associated with the specified key and removes the key from the dictionary.
    /// </summary>
    public bool Clear(TKey key)
    {
        if (_lookup.TryGetValue(key, out var valueSet))
        {
            DebugValid(valueSet);
            var set = valueSet.LastSet;

            _version++;
            _valueCount -= set.Count;

            set.Clear();
            _lookup.Remove(key);

            DebugValueCount();
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.Contains(TKey, TValue)"/>
    public bool Contains(TKey key, TValue value)
    {
        if (_lookup.TryGetValue(key, out var valueSet))
        {
            DebugValid(valueSet);
            return valueSet.LastSet.Contains(value);
        }

        return false;
    }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.ContainsKey(TKey)"/>
    public bool ContainsKey(TKey key) => _lookup.ContainsKey(key);

    /// <summary>
    /// Returns a value indicating whether any of the value sets in the dictionary contain the specified value.
    /// </summary>
    public bool ContainsValue(TValue value)
    {
        foreach (var valueSet in _lookup.Values)
        {
            DebugValid(valueSet);

            if (valueSet.Contains(value))
                return true;
        }

        return false;
    }

    /// <inheritdoc cref="ICollectionDictionary{TKey, TValue, TValueCollection}.GetValueCount(TKey)"/>
    public int GetValueCount(TKey key) => _lookup.TryGetValue(key, out var valueSet) ? valueSet.Count : 0;

    /// <summary>
    /// Gets the value set for the specified key or <see langword="null"/> if the key was not found.
    /// </summary>
    /// <returns>A value indicating whether the key was found.</returns>
    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out ValueSet valueSet) => _lookup.TryGetValue(key, out valueSet);

    /// <summary>
    /// Returns an enumerator that iterates through the value sets in the dictionary.
    /// </summary>
    public ValueSetCollection.Enumerator GetEnumerator() => ValueSets.GetEnumerator();

#if !NETSTANDARD2_0

    /// <summary>
    /// Ensures that the dictionary can hold up to a specified number of key/value set pairs without any further expansion of its backing storage.
    /// </summary>
    /// <param name="capacity">The number of key/value set pairs.</param>
    /// <returns>The currect capacity of the dictionary.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than 0.</exception>
    public int EnsureCapacity(int capacity)
    {
        _version++;
        return _lookup.EnsureCapacity(capacity);
    }

    /// <summary>
    /// Sets the key/value set pair capacity of this dictionary to what it would be if it had been originally initialized with all its entries, and
    /// optionally trims all the value sets in the dictionary as well.
    /// </summary>
    /// <param name="trimValueSets"><see langword="true"/> to trim all the value sets as well, or <see langword="false"/> to only trim the
    /// dictionary.</param>
    public void TrimExcess(bool trimValueSets = true)
    {
        _version++;
        _lookup.TrimExcess();

        if (trimValueSets)
        {
            foreach (var valueSet in _lookup.Values)
                valueSet.LastSet.TrimExcess();
        }
    }

    /// <summary>
    /// Sets the key/value set pair capacity of this dictionary to hold up to a specified number of entries without any further expansion of its backing
    /// storage, and optionally trims the capacity of each value set to the actual number of values in the list.
    /// </summary>
    /// <param name="dictionaryCapacity">The new key/value set pair capacity.</param>
    /// <param name="trimValueSets"><see langword="true"/> to trim all the value sets as well, or <see langword="false"/> to only trim the
    /// dictionary.</param>
    /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than the number of entries in the dictionary.</exception>
    public void TrimExcess(int dictionaryCapacity, bool trimValueSets = true)
    {
        _version++;
        _lookup.TrimExcess(dictionaryCapacity);

        if (trimValueSets)
        {
            foreach (var valueSet in _lookup.Values)
                valueSet.LastSet.TrimExcess();
        }
    }

#endif

    [Conditional("DEBUG")]
    private void DebugValueCount()
    {
        Debug.Assert(_valueCount == _lookup.Values.Sum(v => v.Count), "incorrect value count");
    }

    [Conditional("DEBUG")]
    private static void DebugValid(ValueSet valueSet)
    {
        Debug.Assert(valueSet.Count > 0, "empty value set");
    }

    #region Explicit Interface Implementations

    /// <inheritdoc cref="this[TKey]"/>
    ISet<TValue> ICollectionDictionary<TKey, TValue, ISet<TValue>>.this[TKey key] => this[key];

    /// <inheritdoc cref="this[TKey]"/>
    ISet<TValue> IReadOnlyDictionary<TKey, ISet<TValue>>.this[TKey key] => this[key];

    /// <summary>
    /// Gets a value indicating whether this collection is read-only. Although this dictionary is not read-only, this always returns <see langword="true"/> as
    /// only read operations are supported through the <see cref="ICollection{T}"/> interface.
    /// </summary>
    bool ICollection<KeyValuePair<TKey, ISet<TValue>>>.IsReadOnly => true;

    /// <inheritdoc cref="Keys"/>
    IEnumerable<TKey> IReadOnlyDictionary<TKey, ISet<TValue>>.Keys => Keys;

    /// <inheritdoc cref="Keys"/>
    IReadOnlyCollection<TKey> ICollectionDictionary<TKey, TValue, ISet<TValue>>.Keys => Keys;

    /// <inheritdoc cref="ValueSets"/>
    IEnumerable<ISet<TValue>> IReadOnlyDictionary<TKey, ISet<TValue>>.Values => ValueSets;

    /// <inheritdoc cref="ValueSets"/>
    IReadOnlyCollection<ISet<TValue>> ICollectionDictionary<TKey, TValue, ISet<TValue>>.ValueCollections => ValueSets;

    /// <inheritdoc cref="Values"/>
    IReadOnlyCollection<TValue> ICollectionDictionary<TKey, TValue, ISet<TValue>>.Values => Values;

    /// <summary>
    /// Gets a value indicating whether the specified key and value set is present in the dictionary.
    /// </summary>
    bool ICollection<KeyValuePair<TKey, ISet<TValue>>>.Contains(KeyValuePair<TKey, ISet<TValue>> item)
    {
        return TryGetValues(item.Key, out var valueSet) && valueSet.Equals(item.Value);
    }

    /// <summary>
    /// Copies the key and value set pairs to an array starting at the given array index.
    /// </summary>
    void ICollection<KeyValuePair<TKey, ISet<TValue>>>.CopyTo(KeyValuePair<TKey, ISet<TValue>>[] array, int arrayIndex)
    {
        CollectionCopy.CheckParams(Count, array, arrayIndex);

        foreach (var valueSet in this)
            array[arrayIndex++] = new(valueSet.Key, valueSet);
    }

    /// <inheritdoc cref="TryGetValues(TKey, out ValueSet)"/>
    bool ICollectionDictionary<TKey, TValue, ISet<TValue>>.TryGetValues(TKey key, [MaybeNullWhen(false)] out ISet<TValue> valueCollection)
    {
        bool result = TryGetValues(key, out var c);
        valueCollection = c;
        return result;
    }

    /// <inheritdoc cref="TryGetValues(TKey, out ValueSet)"/>
    bool IReadOnlyDictionary<TKey, ISet<TValue>>.TryGetValue(TKey key, [MaybeNullWhen(false)] out ISet<TValue> value)
    {
        bool result = TryGetValues(key, out var v);
        value = v;
        return result;
    }

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator<KeyValuePair<TKey, ISet<TValue>>> IEnumerable<KeyValuePair<TKey, ISet<TValue>>>.GetEnumerator()
    {
        foreach (var valueSet in this)
            yield return new(valueSet.Key, valueSet);
    }

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Not Supported

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<KeyValuePair<TKey, ISet<TValue>>>.Add(KeyValuePair<TKey, ISet<TValue>> item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<KeyValuePair<TKey, ISet<TValue>>>.Clear() => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    bool ICollection<KeyValuePair<TKey, ISet<TValue>>>.Remove(KeyValuePair<TKey, ISet<TValue>> item) => throw new NotSupportedException();

    #endregion
}