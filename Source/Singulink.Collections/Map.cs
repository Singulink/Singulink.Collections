using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Singulink.Collections
{
    /// <summary>
    /// Represents a collection of two types of values that map between each other in a one-to-one relationship. Values on each side of the map must be unique
    /// on their respective side.
    /// </summary>
    public class Map<TLeft, TRight>
        where TLeft : notnull
        where TRight : notnull
    {
        /// <summary>
        /// Gets the left side of the map which performs lookups from left values to right values.
        /// </summary>
        public MapSide<TLeft, TRight> Left { get; }

        /// <summary>
        /// Gets the right side of the map which performs lookups from right values to left values.
        /// </summary>
        public MapSide<TRight, TLeft> Right { get; }

        /// <summary>
        /// Gets the number of mappings contained in the <see cref="Map{TLeft, TRight}"/>.
        /// </summary>
        public int Count => Left.Lookup.Count;

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
            var leftToRight = new Dictionary<TLeft, TRight>(capacity, leftComparer ?? EqualityComparer<TLeft>.Default);
            var rightToLeft = new Dictionary<TRight, TLeft>(capacity, rightComparer ?? EqualityComparer<TRight>.Default);

            Left = new MapSide<TLeft, TRight>(leftToRight, rightToLeft);
            Right = new MapSide<TRight, TLeft>(rightToLeft, leftToRight);
        }

        /// <summary>
        /// Adds an association to map between the specified left and right value.
        /// </summary>
        public void Add(TLeft leftValue, TRight rightValue)
        {
            if (!Left.Lookup.TryAdd(leftValue, rightValue))
                throw new ArgumentException("Duplicate left value in the map.", nameof(leftValue));

            if (!Right.Lookup.TryAdd(rightValue, leftValue)) {
                Left.Lookup.Remove(leftValue);
                throw new ArgumentException("Duplicate right value in the map.", nameof(rightValue));
            }
        }

        /// <summary>
        /// Removes all mappings from the map.
        /// </summary>
        public void Clear()
        {
            Left.Lookup.Clear();
            Right.Lookup.Clear();
        }

        /// <summary>
        /// Gets a value indicating if the map contains an association between the specified left and right value.
        /// </summary>
        public bool Contains(TLeft leftValue, TRight rightValue)
        {
            return Left.TryGetValue(leftValue, out var existingRightValue) && Right.Lookup.Comparer.Equals(existingRightValue, rightValue);
        }

        /// <summary>
        /// Ensures that this map can hold up to a specified number of entries without any further expansion of its backing storage.
        /// </summary>
        /// <param name="capacity">The number of entries.</param>
        /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than 0.</exception>
        public void EnsureCapacity(int capacity)
        {
            Left.Lookup.EnsureCapacity(capacity);
            Right.Lookup.EnsureCapacity(capacity);
        }

        /// <summary>
        /// Removes an association from the map between the specified left and right value if they currently map to each other and returns true if removal was
        /// successful. If the left and right values do not map to each other then no changes are made and a false result is returned.
        /// </summary>
        public bool Remove(TLeft leftValue, TRight rightValue)
        {
            if (!Contains(leftValue, rightValue))
                return false;

            bool removeResult = Left.Remove(leftValue);
            Debug.Assert(removeResult, "expected successful removal");

            return true;
        }

        /// <summary>
        /// Sets an association from the map between the specified left and right value, overriding any previous associations these values had.
        /// </summary>
        public void Set(TLeft leftValue, TRight rightValue)
        {
            Left.Remove(leftValue);
            Right.Remove(rightValue);

            Left.Lookup.Add(leftValue, rightValue);
            Right.Lookup.Add(rightValue, leftValue);
        }

        /// <summary>
        /// Sets the capacity of this map to what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            Left.Lookup.TrimExcess();
            Right.Lookup.TrimExcess();
        }

        /// <summary>
        /// Sets the capacity of this map to hold up a specified number of entries without any further expansion of its backing storage.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">Capacity specified is less than the number of entries in the map.</exception>
        public void TrimExcess(int capacity)
        {
            Left.Lookup.TrimExcess(capacity);
            Right.Lookup.TrimExcess(capacity);
        }
    }
}