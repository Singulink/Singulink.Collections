using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Singulink.Collections
{
    /// <summary>
    /// Represents a collection of two types of values that map between each other in a bidirectional one-to-one relationship. Values on each side of the map
    /// must be unique on their respective side.
    /// </summary>
    public class Map<TLeft, TRight> : IDictionary<TLeft, TRight>, IReadOnlyDictionary<TLeft, TRight>
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
        public Map(IEqualityComparer<TLeft>? leftComparer = null, IEqualityComparer<TRight>? rightComparer = null) : this(0, leftComparer, rightComparer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Map{TLeft, TRight}"/> class with the specified capacity and value comparers.
        /// </summary>
        public Map(int capacity, IEqualityComparer<TLeft>? leftComparer = null, IEqualityComparer<TRight>? rightComparer = null)
        {
            _leftSide = new Dictionary<TLeft, TRight>(capacity, leftComparer);
            _rightSide = new Dictionary<TRight, TLeft>(capacity, rightComparer);
        }

        // Private ctor used for creating the reverse map.
        private Map(Map<TRight, TLeft> reverse, Dictionary<TLeft, TRight> leftSide, Dictionary<TRight, TLeft> rightSide)
        {
            _reverse = reverse;
            _leftSide = leftSide;
            _rightSide = rightSide;
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <exception cref="ArgumentNullException">The specified left or right value was null.</exception>
        /// <exception cref="KeyNotFoundException">The key was not found.</exception>
        /// <exception cref="ArgumentException">The right value being set is a duplicate value on another mapping.</exception>
        public TRight this[TLeft leftValue] {
            get => _leftSide[leftValue];
            set {
                if (_rightSide.TryGetValue(value, out var existingLeftValue)) {
                    if (_leftSide.Comparer.Equals(leftValue, existingLeftValue))
                        return;

                    throw new ArgumentException("Duplicate right value in the map.");
                }

                _leftSide[leftValue] = value;
                _rightSide.Add(value, leftValue);
            }
        }

        /// <summary>
        /// Gets the number of mappings contained in the map.
        /// </summary>
        public int Count => _leftSide.Count;

        /// <summary>
        /// Gets the values on the left side of the map.
        /// </summary>
        public Dictionary<TLeft, TRight>.KeyCollection LeftValues => _leftSide.Keys;

        /// <summary>
        /// Gets the values on the right side of the map.
        /// </summary>
        public Dictionary<TRight, TLeft>.KeyCollection RightValues => _rightSide.Keys;

        /// <summary>
        /// Gets the reverse map where the left and right side are flipped.
        /// </summary>
        public Map<TRight, TLeft> Reverse => _reverse ??= new Map<TRight, TLeft>(this, _rightSide, _leftSide);

        /// <summary>
        /// Adds an association to map between the specified left and right value.
        /// </summary>
        /// <exception cref="ArgumentNullException">The specified left or right value was null.</exception>
        public void Add(TLeft leftValue, TRight rightValue)
        {
            if (!_leftSide.TryAdd(leftValue, rightValue))
                throw new ArgumentException("Duplicate left value in the map.", nameof(leftValue));

            if (!_rightSide.TryAdd(rightValue, leftValue)) {
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

        /// <summary>
        /// Gets a value indicating if the map contains an association between the specified left and right value.
        /// </summary>
        /// <exception cref="ArgumentNullException">The specified left or right value was null.</exception>
        public bool Contains(TLeft leftValue, TRight rightValue)
        {
            return _leftSide.TryGetValue(leftValue, out var existingRightValue) && _rightSide.Comparer.Equals(existingRightValue, rightValue);
        }

        /// <summary>
        /// Determines whether this map contains the specified left value.
        /// </summary>
        /// <param name="leftValue">The left value to locate.</param>
        /// <returns><see langword="true"/> if this map contains the specified left value, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified left value was null.</exception>
        public bool ContainsLeft(TLeft leftValue) => _leftSide.ContainsKey(leftValue);

        /// <summary>
        /// Determines whether this map contains the specified right value.
        /// </summary>
        /// <param name="rightValue">The right value to locate.</param>
        /// <returns><see langword="true"/> if this map contains the specified right value, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified right value was null.</exception>
        public bool ContainsRight(TRight rightValue) => _rightSide.ContainsKey(rightValue);

        /// <summary>
        /// Ensures that this map can hold up to a specified number of entries without any further expansion of its backing storage.
        /// </summary>
        /// <param name="capacity">The number of entries.</param>
        /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than 0.</exception>
        public void EnsureCapacity(int capacity)
        {
            _leftSide.EnsureCapacity(capacity);
            _rightSide.EnsureCapacity(capacity);
        }

        /// <summary>
        /// Removes an association from the map between the specified left and right value if they currently map to each other and returns true if removal was
        /// successful. If the left and right values do not map to each other then no changes are made and <see langword="false"/> is returned.
        /// </summary>
        /// <exception cref="ArgumentNullException">The specified left or right value was null.</exception>
        public bool Remove(TLeft leftValue, TRight rightValue)
        {
            if (!Contains(leftValue, rightValue))
                return false;

            _leftSide.Remove(leftValue);
            _rightSide.Remove(rightValue);

            return true;
        }

        /// <summary>
        /// Removes an association from the map given the specified left value.
        /// </summary>
        /// <returns><see langword="true"/> if the association with the given left value is successfully found and removed, otherwise <see
        /// langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified left value was null.</exception>
        public bool RemoveLeft(TLeft leftValue)
        {
            if (_leftSide.Remove(leftValue, out var rightValue)) {
                bool result = _rightSide.Remove(rightValue);
                Debug.Assert(result, "right map side remove failure");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes an association from the map given the specified right value.
        /// </summary>
        /// <returns><see langword="true"/> if the association with the given right value is successfully found and removed, otherwise <see
        /// langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified right value was null.</exception>
        public bool RemoveRight(TRight rightValue)
        {
            if (_rightSide.Remove(rightValue, out var leftValue)) {
                bool result = _leftSide.Remove(leftValue);
                Debug.Assert(result, "left map side remove failure");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets an association on the map between the specified left and right value, overriding any previous associations these values had.
        /// </summary>
        /// <exception cref="ArgumentNullException">The specified left or right value was null.</exception>
        public void Set(TLeft leftValue, TRight rightValue)
        {
            if (_leftSide.Remove(leftValue, out var existingRightValue))
                _rightSide.Remove(existingRightValue);

            if (_rightSide.Remove(rightValue, out var existingLeftValue))
                _leftSide.Remove(existingLeftValue);

            _leftSide.Add(leftValue, rightValue);
            _rightSide.Add(rightValue, leftValue);
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

        /// <summary>
        /// Gets the right value associated with the specified left value.
        /// </summary>
        /// <param name="leftValue">The left value to look up in the map.</param>
        /// <param name="rightValue">When this method returns, contains the right value associated with the specified left value, if the left value is found;
        /// otherwise, the default value for the type of the right value parameter.</param>
        /// <returns><see langword="true"/> if the map contains the specified left value, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified left value was null.</exception>
        public bool TryGetRightValue(TLeft leftValue, [MaybeNullWhen(false)] out TRight rightValue) => _leftSide.TryGetValue(leftValue, out rightValue);

        /// <summary>
        /// Gets the right value associated with the specified left value.
        /// </summary>
        /// <param name="rightValue">The right value to look up in the map.</param>
        /// <param name="leftValue">When this method returns, contains the left value associated with the specified right value, if the right value is found;
        /// otherwise, the default value for the type of the left value parameter.</param>
        /// <returns><see langword="true"/> if the map contains the specified right value, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified right value was null.</exception>
        public bool TryGetLeftValue(TRight rightValue, [MaybeNullWhen(false)] out TLeft leftValue) => _rightSide.TryGetValue(rightValue, out leftValue);

        /// <summary>
        /// Returns an enumerator that iterates through the keys and values on this map side.
        /// </summary>
        public Dictionary<TLeft, TRight>.Enumerator GetEnumerator() => _leftSide.GetEnumerator();

        #region Explicit Interface Implementations

        /// <inheritdoc/>
        ICollection<TLeft> IDictionary<TLeft, TRight>.Keys => LeftValues;

        /// <inheritdoc/>
        ICollection<TRight> IDictionary<TLeft, TRight>.Values => RightValues;

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TLeft, TRight>>.IsReadOnly => false;

        /// <inheritdoc/>
        IEnumerable<TLeft> IReadOnlyDictionary<TLeft, TRight>.Keys => LeftValues;

        /// <inheritdoc/>
        IEnumerable<TRight> IReadOnlyDictionary<TLeft, TRight>.Values => RightValues;

        /// <inheritdoc/>
        bool IDictionary<TLeft, TRight>.ContainsKey(TLeft key) => ContainsLeft(key);

        /// <inheritdoc/>
        bool IDictionary<TLeft, TRight>.Remove(TLeft key) => RemoveLeft(key);

        /// <inheritdoc/>
        bool IDictionary<TLeft, TRight>.TryGetValue(TLeft key, out TRight value) => TryGetRightValue(key, out value!);

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TLeft, TRight>>.Add(KeyValuePair<TLeft, TRight> item) => Add(item.Key, item.Value);

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TLeft, TRight>>.Contains(KeyValuePair<TLeft, TRight> item) => Contains(item.Key, item.Value);

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TLeft, TRight>>.CopyTo(KeyValuePair<TLeft, TRight>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TLeft, TRight>>)_leftSide).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TLeft, TRight>>.Remove(KeyValuePair<TLeft, TRight> item) => Remove(item.Key, item.Value);

        /// <inheritdoc/>
        IEnumerator<KeyValuePair<TLeft, TRight>> IEnumerable<KeyValuePair<TLeft, TRight>>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        bool IReadOnlyDictionary<TLeft, TRight>.ContainsKey(TLeft key) => ContainsLeft(key);

        /// <inheritdoc/>
        bool IReadOnlyDictionary<TLeft, TRight>.TryGetValue(TLeft key, out TRight value) => TryGetRightValue(key, out value!);

        #endregion
    }
}