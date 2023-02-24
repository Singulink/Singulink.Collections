using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <inheritdoc cref="IMap{TLeft, TRight}"/>
public class Map<TLeft, TRight> : IMap<TLeft, TRight>, IReadOnlyMap<TLeft, TRight>, IDictionary<TLeft, TRight>, IReadOnlyDictionary<TLeft, TRight>
    where TLeft : notnull
    where TRight : notnull
{
    private readonly Dictionary<TLeft, TRight> _leftSide;
    private readonly Dictionary<TRight, TLeft> _rightSide;

    private Map<TRight, TLeft>? _reverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TLeft, TRight}"/> class.
    /// </summary>
    public Map() : this(0, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TLeft, TRight}"/> class with the specified capacity.
    /// </summary>
    public Map(int capacity) : this(capacity, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TLeft, TRight}"/> class with the specified value comparers.
    /// </summary>
    public Map(IEqualityComparer<TLeft>? leftComparer, IEqualityComparer<TRight>? rightComparer) : this(0, leftComparer, rightComparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TLeft, TRight}"/> class with the specified capacity and value comparers.
    /// </summary>
    public Map(int capacity, IEqualityComparer<TLeft>? leftComparer, IEqualityComparer<TRight>? rightComparer)
    {
        _leftSide = new Dictionary<TLeft, TRight>(capacity, leftComparer);
        _rightSide = new Dictionary<TRight, TLeft>(capacity, rightComparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TLeft, TRight}"/> class. Trusted Private ctor used only for creating the reverse map.
    /// </summary>
    private Map(Map<TRight, TLeft> reverse, Dictionary<TLeft, TRight> leftSide, Dictionary<TRight, TLeft> rightSide)
    {
        _reverse = reverse;
        _leftSide = leftSide;
        _rightSide = rightSide;
    }

    /// <inheritdoc cref="IMap{TLeft, TRight}.this[TLeft]"/>
    public TRight this[TLeft leftValue]
    {
        get => _leftSide[leftValue];
        set {
            if (_rightSide.TryGetValue(value, out var existingLeftValue))
            {
                if (!_leftSide.Comparer.Equals(leftValue, existingLeftValue))
                    Throw.Arg("Duplicate right value in the map.");

                return;
            }

            if (_leftSide.TryGetValue(leftValue, out var existingRightValue))
                _rightSide.Remove(existingRightValue);

            _leftSide[leftValue] = value;
            _rightSide.Add(value, leftValue);
        }
    }

    /// <summary>
    /// Gets the number of mappings contained in the map.
    /// </summary>
    public int Count => _leftSide.Count;

    /// <inheritdoc cref="IMap{TLeft, TRight}.LeftValues"/>
    public Dictionary<TLeft, TRight>.KeyCollection LeftValues => _leftSide.Keys;

    /// <inheritdoc cref="IMap{TLeft, TRight}.Reverse"/>
    public Map<TRight, TLeft> Reverse => _reverse ??= new Map<TRight, TLeft>(this, _rightSide, _leftSide);

    /// <inheritdoc cref="IMap{TLeft, TRight}.RightValues"/>
    public Dictionary<TRight, TLeft>.KeyCollection RightValues => _rightSide.Keys;

    /// <inheritdoc cref="IMap{TLeft, TRight}.Add(TLeft, TRight)"/>
    public void Add(TLeft leftValue, TRight rightValue)
    {
        if (!_leftSide.TryAdd(leftValue, rightValue))
            throw new ArgumentException("Duplicate left value in the map.", nameof(leftValue));

        if (!_rightSide.TryAdd(rightValue, leftValue))
        {
            _leftSide.Remove(leftValue);
            throw new ArgumentException("Duplicate right value in the map.", nameof(rightValue));
        }
    }

    /// <summary>
    /// Removes all mappings from the map.
    /// </summary>
    public void Clear()
    {
        _leftSide.Clear();
        _rightSide.Clear();
    }

    /// <inheritdoc cref="IMap{TLeft, TRight}.Contains(TLeft, TRight)"/>
    public bool Contains(TLeft leftValue, TRight rightValue)
    {
        return _leftSide.TryGetValue(leftValue, out var existingRightValue) && _rightSide.Comparer.Equals(existingRightValue, rightValue);
    }

    /// <inheritdoc cref="IMap{TLeft, TRight}.ContainsLeft(TLeft)"/>
    public bool ContainsLeft(TLeft leftValue) => _leftSide.ContainsKey(leftValue);

    /// <inheritdoc cref="IMap{TLeft, TRight}.ContainsRight(TRight)"/>
    public bool ContainsRight(TRight rightValue) => _rightSide.ContainsKey(rightValue);

    /// <inheritdoc cref="IMap{TLeft, TRight}.Remove(TLeft, TRight)"/>
    public bool Remove(TLeft leftValue, TRight rightValue)
    {
        if (!Contains(leftValue, rightValue))
            return false;

        _leftSide.Remove(leftValue);
        _rightSide.Remove(rightValue);

        return true;
    }

    /// <inheritdoc cref="IMap{TLeft, TRight}.RemoveLeft(TLeft)"/>
    public bool RemoveLeft(TLeft leftValue)
    {
        if (_leftSide.Remove(leftValue, out var rightValue))
        {
            bool result = _rightSide.Remove(rightValue);
            Debug.Assert(result, "right map side remove failure");

            return true;
        }

        return false;
    }

    /// <inheritdoc cref="IMap{TLeft, TRight}.RemoveRight(TRight)"/>
    public bool RemoveRight(TRight rightValue)
    {
        if (_rightSide.Remove(rightValue, out var leftValue))
        {
            bool result = _leftSide.Remove(leftValue);
            Debug.Assert(result, "left map side remove failure");

            return true;
        }

        return false;
    }

    /// <inheritdoc cref="IMap{TLeft, TRight}.Set(TLeft, TRight)"/>
    public void Set(TLeft leftValue, TRight rightValue)
    {
        if (_leftSide.Remove(leftValue, out var existingRightValue))
            _rightSide.Remove(existingRightValue);

        if (_rightSide.Remove(rightValue, out var existingLeftValue))
            _leftSide.Remove(existingLeftValue);

        _leftSide.Add(leftValue, rightValue);
        _rightSide.Add(rightValue, leftValue);
    }

    /// <inheritdoc cref="IMap{TLeft, TRight}.TryGetLeftValue(TRight, out TLeft)"/>
    public bool TryGetLeftValue(TRight rightValue, [MaybeNullWhen(false)] out TLeft leftValue) => _rightSide.TryGetValue(rightValue, out leftValue);

    /// <inheritdoc cref="IMap{TLeft, TRight}.TryGetRightValue(TLeft, out TRight)"/>
    public bool TryGetRightValue(TLeft leftValue, [MaybeNullWhen(false)] out TRight rightValue) => _leftSide.TryGetValue(leftValue, out rightValue);

    /// <summary>
    /// Returns an enumerator that iterates through the left and right value pairs.
    /// </summary>
    public Dictionary<TLeft, TRight>.Enumerator GetEnumerator() => _leftSide.GetEnumerator();

#if !NETSTANDARD2_0

    /// <summary>
    /// Ensures that this map can hold up to a specified number of entries without any further expansion of its backing storage.
    /// </summary>
    /// <param name="capacity">The number of entries.</param>
    /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than 0.</exception>
    public int EnsureCapacity(int capacity)
    {
        _leftSide.EnsureCapacity(capacity);
        return _rightSide.EnsureCapacity(capacity);
    }

    /// <summary>
    /// Sets the capacity of this map to what it would be if it had been originally initialized with all its entries.
    /// </summary>
    public void TrimExcess()
    {
        _leftSide.TrimExcess();
        _rightSide.TrimExcess();
    }

    /// <summary>
    /// Sets the capacity of this map to hold up a specified number of entries without any further expansion of its backing storage.
    /// </summary>
    /// <param name="capacity">The new capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than the number of entries in the map.</exception>
    public void TrimExcess(int capacity)
    {
        _leftSide.TrimExcess(capacity);
        _rightSide.TrimExcess(capacity);
    }

#endif

    #region Explicit Interface Implementations

    /// <summary>
    /// Gets a value indicating whether the collection is read-only. Always returns <see langword="false"/>.
    /// </summary>
    bool ICollection<KeyValuePair<TLeft, TRight>>.IsReadOnly => false;

    /// <inheritdoc cref="LeftValues"/>
    IReadOnlyCollection<TLeft> IMap<TLeft, TRight>.LeftValues => LeftValues;

    /// <inheritdoc cref="LeftValues"/>
    IReadOnlyCollection<TLeft> IReadOnlyMap<TLeft, TRight>.LeftValues => LeftValues;

    /// <inheritdoc cref="LeftValues"/>
    ICollection<TLeft> IDictionary<TLeft, TRight>.Keys => LeftValues;

    /// <inheritdoc cref="LeftValues"/>
    IEnumerable<TLeft> IReadOnlyDictionary<TLeft, TRight>.Keys => LeftValues;

    /// <inheritdoc cref="RightValues"/>
    IReadOnlyCollection<TRight> IMap<TLeft, TRight>.RightValues => RightValues;

    /// <inheritdoc cref="RightValues"/>
    IReadOnlyCollection<TRight> IReadOnlyMap<TLeft, TRight>.RightValues => RightValues;

    /// <inheritdoc cref="RightValues"/>
    IEnumerable<TRight> IReadOnlyDictionary<TLeft, TRight>.Values => RightValues;

    /// <inheritdoc cref="RightValues"/>
    ICollection<TRight> IDictionary<TLeft, TRight>.Values => RightValues;

    /// <inheritdoc cref="Reverse"/>
    IReadOnlyMap<TRight, TLeft> IReadOnlyMap<TLeft, TRight>.Reverse => Reverse;

    /// <inheritdoc cref="Reverse"/>
    IMap<TRight, TLeft> IMap<TLeft, TRight>.Reverse => Reverse;

    /// <inheritdoc cref="ContainsLeft(TLeft)"/>
    bool IDictionary<TLeft, TRight>.ContainsKey(TLeft key) => ContainsLeft(key);

    /// <inheritdoc cref="RemoveLeft(TLeft)"/>
    bool IDictionary<TLeft, TRight>.Remove(TLeft key) => RemoveLeft(key);

    /// <inheritdoc cref="TryGetRightValue(TLeft, out TRight)"/>
    bool IDictionary<TLeft, TRight>.TryGetValue(TLeft key, out TRight value) => TryGetRightValue(key, out value!);

    /// <inheritdoc cref="Add(TLeft, TRight)"/>
    void ICollection<KeyValuePair<TLeft, TRight>>.Add(KeyValuePair<TLeft, TRight> item) => Add(item.Key, item.Value);

    /// <inheritdoc cref="Contains(TLeft, TRight)"/>
    bool ICollection<KeyValuePair<TLeft, TRight>>.Contains(KeyValuePair<TLeft, TRight> item) => Contains(item.Key, item.Value);

    /// <summary>
    /// Copies the left and right value pairs to an array starting at the specified array index.
    /// </summary>
    void ICollection<KeyValuePair<TLeft, TRight>>.CopyTo(KeyValuePair<TLeft, TRight>[] array, int arrayIndex)
    {
        ICollection<KeyValuePair<TLeft, TRight>> leftCollection = _leftSide;
        leftCollection.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc cref="Remove(TLeft, TRight)"/>
    bool ICollection<KeyValuePair<TLeft, TRight>>.Remove(KeyValuePair<TLeft, TRight> item) => Remove(item.Key, item.Value);

    /// <inheritdoc cref="ContainsLeft(TLeft)"/>
    bool IReadOnlyDictionary<TLeft, TRight>.ContainsKey(TLeft key) => ContainsLeft(key);

    /// <inheritdoc cref="TryGetRightValue(TLeft, out TRight)"/>
    bool IReadOnlyDictionary<TLeft, TRight>.TryGetValue(TLeft key, out TRight value) => TryGetRightValue(key, out value!);

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator<KeyValuePair<TLeft, TRight>> IEnumerable<KeyValuePair<TLeft, TRight>>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}