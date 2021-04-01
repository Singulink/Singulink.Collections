using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Singulink.Collections
{
    /// <summary>
    /// Represents a collection of two types of values that map between each other.
    /// </summary>
    public class Map<TLeft, TRight>
        where TLeft : notnull
        where TRight : notnull
    {
        public Map() : this(0, null, null) { }

        public Map(int capacity) : this(capacity, null, null) { }

        public Map(IEqualityComparer<TLeft>? leftComparer = null, IEqualityComparer<TRight>? rightComparer = null) : this(0, leftComparer, rightComparer) { }

        public Map(int capacity, IEqualityComparer<TLeft>? leftComparer = null, IEqualityComparer<TRight>? rightComparer = null)
        {
            var leftToRight = new Dictionary<TLeft, TRight>(capacity, leftComparer ?? EqualityComparer<TLeft>.Default);
            var rightToLeft = new Dictionary<TRight, TLeft>(capacity, rightComparer ?? EqualityComparer<TRight>.Default);

            Left = new MapSide<TLeft, TRight>(leftToRight, rightToLeft);
            Right = new MapSide<TRight, TLeft>(rightToLeft, leftToRight);
        }

        /// <summary>
        /// Gets the left side of the map which performs lookups from left values to right values.
        /// </summary>
        public MapSide<TLeft, TRight> Left { get; }

        /// <summary>
        /// Gets the right side of the map which performs lookups from the right values to left values.
        /// </summary>
        public MapSide<TRight, TLeft> Right { get; }

        /// <summary>
        /// Gets the number of mappings contained in the <see cref="Map{TLeft, TRight}"/>.
        /// </summary>
        public int Count => Left.Lookup.Count;

        /// <summary>
        /// Gets a value indicating if the map contains an association between the specified left and right value.
        /// </summary>
        public bool Contains(TLeft leftValue, TRight rightValue) => Left.TryGetValue(leftValue, out var storedRightValue) && Right.Lookup.Comparer.Equals(storedRightValue, rightValue);

        /// <summary>
        /// Adds an association to map between the specified left and right value.
        /// </summary>
        public void Add(TLeft leftValue, TRight rightValue)
        {
            if (Left.Contains(leftValue))
                throw new ArgumentException("Duplicate left value.", nameof(leftValue));

            try {
                Right.Lookup.Add(rightValue, leftValue);
            }
            catch (ArgumentException) {
                throw new ArgumentException("Duplicate right value.", nameof(rightValue));
            }

            Left.Lookup.Add(leftValue, rightValue);
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
    }
}
