#if NET9_0_OR_GREATER

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Singulink.Collections.Utilities;

namespace Singulink.Collections;

/// <content>
/// Contains the alternate lookup implementations for Map.
/// </content>
partial class Map<TLeft, TRight>
{
    /// <summary>
    /// Gets an alternate lookup that can be used to perform operations on this map using <typeparamref name="TAlternateLeft"/> instead of <typeparamref
    /// name="TLeft"/> and <typeparamref name="TAlternateRight"/> instead of <typeparamref name="TRight"/>.
    /// </summary>
    public AlternateLookup<TAlternateLeft, TAlternateRight> GetAlternateLookup<TAlternateLeft, TAlternateRight>()
        where TAlternateLeft : notnull, allows ref struct
        where TAlternateRight : notnull, allows ref struct
    {
        return new AlternateLookup<TAlternateLeft, TAlternateRight>(
            this,
            _leftSide.GetAlternateLookup<TAlternateLeft>(),
            _rightSide.GetAlternateLookup<TAlternateRight>());
    }

    /// <summary>
    /// Gets an alternate lookup that can be used to perform operations on this map using <typeparamref name="TAlternateLeft"/> instead of <typeparamref
    /// name="TLeft"/>.
    /// </summary>
    public AlternateLeftLookup<TAlternateLeft> GetAlternateLeftLookup<TAlternateLeft>()
        where TAlternateLeft : notnull, allows ref struct
    {
        return new AlternateLeftLookup<TAlternateLeft>(this, _leftSide.GetAlternateLookup<TAlternateLeft>());
    }

    /// <inheritdoc cref="GetAlternateLookup{TAlternateLeft, TAlternateRight}"/>
    public bool TryGetAlternateLookup<TAlternateLeft, TAlternateRight>(
        [MaybeNullWhen(false)] out AlternateLookup<TAlternateLeft, TAlternateRight> lookup)
        where TAlternateLeft : notnull, allows ref struct
        where TAlternateRight : notnull, allows ref struct
    {
        if (_leftSide.TryGetAlternateLookup<TAlternateLeft>(out var altLeftSide) &&
            _rightSide.TryGetAlternateLookup<TAlternateRight>(out var altRightSide))
        {
            lookup = new AlternateLookup<TAlternateLeft, TAlternateRight>(this, altLeftSide, altRightSide);
            return true;
        }

        lookup = default;
        return false;
    }

    /// <inheritdoc cref="GetAlternateLeftLookup{TAlternateLeft}"/>
    public bool TryGetAlternateLeftLookup<TAlternateLeft>(
        [MaybeNullWhen(false)] out AlternateLeftLookup<TAlternateLeft> lookup)
        where TAlternateLeft : notnull, allows ref struct
    {
        if (_leftSide.TryGetAlternateLookup<TAlternateLeft>(out var altLeftSide))
        {
            lookup = new AlternateLeftLookup<TAlternateLeft>(this, altLeftSide);
            return true;
        }

        lookup = default;
        return false;
    }

    /// <summary>
    /// Provides an alternate lookup that can be used to perform operations on a <see cref="Map{TLeft, TRight}"/> using <typeparamref name="TAlternateLeft"/>
    /// instead of <typeparamref name="TLeft"/> and <typeparamref name="TAlternateRight"/> instead of <typeparamref name="TRight"/>.
    /// </summary>
    public readonly struct AlternateLookup<TAlternateLeft, TAlternateRight>
        where TAlternateLeft : notnull, allows ref struct
        where TAlternateRight : notnull, allows ref struct
    {
        private readonly Map<TLeft, TRight> _map;
        private readonly Dictionary<TLeft, TRight>.AlternateLookup<TAlternateLeft> _altLeftSide;
        private readonly Dictionary<TRight, TLeft>.AlternateLookup<TAlternateRight> _altRightSide;

        internal AlternateLookup(
            Map<TLeft, TRight> map,
            Dictionary<TLeft, TRight>.AlternateLookup<TAlternateLeft> altLeftSide,
            Dictionary<TRight, TLeft>.AlternateLookup<TAlternateRight> altRightSide)
        {
            _map = map;
            _altLeftSide = altLeftSide;
            _altRightSide = altRightSide;
        }

        /// <summary>
        /// Gets or sets the right value associated with the specified alternate left value.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The property is being retrieved and the left value was not found.</exception>
        /// <exception cref="ArgumentException">The property is being set and the right value provided is already assigned to a different left
        /// value.</exception>
        public TRight this[TAlternateLeft leftValue]
        {
            get => _altLeftSide[leftValue];
            set {
                if (_altRightSide.Dictionary.TryGetValue(value, out var existingLeftValue))
                {
                    if (!LeftComparer.Equals(leftValue, existingLeftValue))
                        Throw.Arg("Duplicate right value in the map.");

                    return;
                }

                if (_altLeftSide.TryGetValue(leftValue, out var actualLeftValue, out var existingRightValue))
                    _altRightSide.Dictionary.Remove(existingRightValue);
                else
                    actualLeftValue = LeftComparer.Create(leftValue);

                _altLeftSide.Dictionary[actualLeftValue] = value;
                _altRightSide.Dictionary.Add(value, actualLeftValue);
            }
        }

        /// <summary>
        /// Gets the alternate equality comparer used for left values.
        /// </summary>
        public IAlternateEqualityComparer<TAlternateLeft, TLeft> LeftComparer =>
            Unsafe.As<IAlternateEqualityComparer<TAlternateLeft, TLeft>>(_altLeftSide.Dictionary.Comparer);

        /// <summary>
        /// Gets the underlying map associated with this alternate lookup.
        /// </summary>
        public Map<TLeft, TRight> Map => _map;

        /// <summary>
        /// Gets the reverse alternate lookup where the left and right side are flipped.
        /// </summary>
        public Map<TRight, TLeft>.AlternateLookup<TAlternateRight, TAlternateLeft> Reverse => new(_map.Reverse, _altRightSide, _altLeftSide);

        /// <summary>
        /// Gets the alternate equality comparer used for right values.
        /// </summary>
        public IAlternateEqualityComparer<TAlternateRight, TRight> RightComparer =>
            Unsafe.As<IAlternateEqualityComparer<TAlternateRight, TRight>>(_altRightSide.Dictionary.Comparer);

        /// <summary>
        /// Gets a value indicating if the map contains an association between the specified alternate left and alternate right value.
        /// </summary>
        public bool Contains(TAlternateLeft leftValue, TAlternateRight rightValue) =>
            _altLeftSide.TryGetValue(leftValue, out TRight right) && RightComparer.Equals(rightValue, right);

        /// <summary>
        /// Gets a value indicating if the map contains an association between the specified alternate left and alternate right value.
        /// </summary>
        public bool Contains(
            TAlternateLeft leftValue,
            TAlternateRight rightValue,
            [MaybeNullWhen(false)] out TLeft actualLeftValue,
            [MaybeNullWhen(false)] out TRight actualRightValue)
        {
            return _altLeftSide.TryGetValue(leftValue, out actualLeftValue, out actualRightValue) && RightComparer.Equals(rightValue, actualRightValue);
        }

        /// <summary>
        /// Determines whether the map contains the specified alternate left value.
        /// </summary>
        public bool ContainsLeft(TAlternateLeft leftValue) => _altLeftSide.ContainsKey(leftValue);

        /// <summary>
        /// Determines whether the map contains the specified alternate left value.
        /// </summary>
        public bool ContainsLeft(TAlternateLeft leftValue, [MaybeNullWhen(false)] out TLeft actualLeftValue) =>
            _altLeftSide.TryGetValue(leftValue, out actualLeftValue, out _);

        /// <summary>
        /// Determines whether the map contains the specified alternate right value.
        /// </summary>
        public bool ContainsRight(TAlternateRight rightValue) => _altRightSide.ContainsKey(rightValue);

        /// <summary>
        /// Determines whether the map contains the specified alternate right value.
        /// </summary>
        public bool ContainsRight(TAlternateRight rightValue, [MaybeNullWhen(false)] out TRight actualRightValue) =>
            _altRightSide.TryGetValue(rightValue, out actualRightValue, out _);

        /// <inheritdoc cref="Remove(TAlternateLeft, TAlternateRight, out TLeft, out TRight)"/>
        public bool Remove(TAlternateLeft leftValue, TAlternateRight rightValue) => Remove(leftValue, rightValue, out _, out _);

        /// <summary>
        /// Removes an association from the map between the specified alternate left and alternate right value if they currently map to each other.
        /// </summary>
        /// <returns><see langword="true"/> if removal was successful, otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// If the left and right values do not map to each other then no changes are made to the map and the removal does not succeed.
        /// </remarks>
        public bool Remove(
            TAlternateLeft leftValue,
            TAlternateRight rightValue,
            [MaybeNullWhen(false)] out TLeft actualLeftValue,
            [MaybeNullWhen(false)] out TRight actualRightValue)
        {
            if (!Contains(leftValue, rightValue))
            {
                actualLeftValue = default;
                actualRightValue = default;

                return false;
            }

            _altLeftSide.Remove(leftValue, out actualLeftValue, out actualRightValue);
            Debug.Assert(actualLeftValue is not null && actualRightValue is not null, "Unexpected removal failure");
            _altRightSide.Dictionary.Remove(actualRightValue);

            return true;
        }

        /// <summary>
        /// Removes the mapping with the specified alternate left value.
        /// </summary>
        public bool RemoveLeft(TAlternateLeft leftValue)
        {
            if (_altLeftSide.Remove(leftValue, out _, out TRight rightValue))
            {
                bool result = _altRightSide.Dictionary.Remove(rightValue);
                Debug.Assert(result, "Right side removal failed after left side removal via alternate key.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the mapping with the specified alternate left value.
        /// </summary>
        public bool RemoveLeft(TAlternateLeft leftKey, [MaybeNullWhen(false)] out TLeft actualLeftKey, [MaybeNullWhen(false)] out TRight rightValue)
        {
            if (_altLeftSide.Remove(leftKey, out actualLeftKey, out rightValue))
            {
                bool result = _altRightSide.Dictionary.Remove(rightValue);
                Debug.Assert(result, "Right side removal failed after left side removal via alternate key.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the mapping with the specified alternate right value.
        /// </summary>
        public bool RemoveRight(TAlternateRight rightKey) => _altRightSide.Remove(rightKey, out _, out _);

        /// <summary>
        /// Removes the mapping with the specified alternate right value.
        /// </summary>
        public bool RemoveRight(TAlternateRight rightKey, [MaybeNullWhen(false)] out TRight actualRightKey, [MaybeNullWhen(false)] out TLeft leftValue)
        {
            if (_altRightSide.Remove(rightKey, out actualRightKey, out leftValue))
            {
                bool result = _altLeftSide.Dictionary.Remove(leftValue);
                Debug.Assert(result, "Left side removal failed after right side removal via alternate key.");
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="Set(TAlternateLeft, TAlternateRight, out TLeft, out TRight)"/>
        public void Set(TAlternateLeft leftValue, TAlternateRight rightValue) => Set(leftValue, rightValue, out _, out _);

        /// <inheritdoc cref="Set(TAlternateLeft, TRight, out TLeft)"/>
        public void Set(TAlternateLeft leftValue, TRight rightValue) => Set(leftValue, rightValue, out _);

        /// <inheritdoc cref="Set(TLeft, TAlternateRight, out TRight)"/>
        public void Set(TLeft leftValue, TAlternateRight rightValue) => Set(leftValue, rightValue, out _);

        /// <summary>
        /// Sets an association in the map between the specified alternate left and alternate right value, overriding any existing associations these values
        /// had.
        /// </summary>
        /// <remarks>
        /// This method is functionally equivalent to removing any existing associations for the left and right values and then adding the new association, so
        /// it is guaranteed to succeed.
        /// </remarks>
        public void Set(TAlternateLeft leftValue, TAlternateRight rightValue, out TLeft actualLeftValue, out TRight actualRightValue)
        {
            if (_altLeftSide.Remove(leftValue, out actualLeftValue!, out var existingRightValue))
                _altRightSide.Dictionary.Remove(existingRightValue);
            else
                actualLeftValue = LeftComparer.Create(leftValue);

            if (_altRightSide.Remove(rightValue, out actualRightValue!, out var existingLeftValue))
                _altLeftSide.Dictionary.Remove(existingLeftValue);
            else
                actualRightValue = RightComparer.Create(rightValue);

            _altLeftSide.Dictionary.Add(actualLeftValue, actualRightValue);
            _altRightSide.Dictionary.Add(actualRightValue, actualLeftValue);
        }

        /// <summary>
        /// Sets an association in the map between the specified alternate left value and right value, overriding any existing associations these values had.
        /// </summary>
        /// <remarks>
        /// This method is functionally equivalent to removing any existing associations for the left and right values and then adding the new association, so
        /// it is guaranteed to succeed.
        /// </remarks>
        public void Set(TAlternateLeft leftValue, TRight rightValue, out TLeft actualLeftValue)
        {
            if (_altLeftSide.Remove(leftValue, out actualLeftValue!, out var existingRightValue))
                _altRightSide.Dictionary.Remove(existingRightValue);
            else
                actualLeftValue = LeftComparer.Create(leftValue);

            if (_altRightSide.Dictionary.Remove(rightValue, out var existingLeftValue))
                _altLeftSide.Dictionary.Remove(existingLeftValue);

            _altLeftSide.Dictionary.Add(actualLeftValue, rightValue);
            _altRightSide.Dictionary.Add(rightValue, actualLeftValue);
        }

        /// <summary>
        /// Sets an association in the map between the specified left value and alternate right value, overriding any existing associations these values had.
        /// </summary>
        /// <remarks>
        /// This method is functionally equivalent to removing any existing associations for the left and right values and then adding the new association, so
        /// it is guaranteed to succeed.
        /// </remarks>
        public void Set(TLeft leftValue, TAlternateRight rightValue, out TRight actualRightValue)
        {
            if (_altLeftSide.Dictionary.Remove(leftValue, out var existingRightValue))
                _altRightSide.Dictionary.Remove(existingRightValue);

            if (_altRightSide.Remove(rightValue, out actualRightValue!, out var existingLeftValue))
                _altLeftSide.Dictionary.Remove(existingLeftValue);
            else
                actualRightValue = RightComparer.Create(rightValue);

            _altLeftSide.Dictionary.Add(leftValue, actualRightValue);
            _altRightSide.Dictionary.Add(actualRightValue, leftValue);
        }

        /// <inheritdoc cref="TryAdd(TAlternateLeft, TAlternateRight, out TLeft, out TRight)"/>
        public bool TryAdd(TAlternateLeft leftValue, TAlternateRight rightValue) => TryAdd(leftValue, rightValue, out _, out _);

        /// <inheritdoc cref="TryAdd(TAlternateLeft, TRight, out TLeft)"/>
        public bool TryAdd(TAlternateLeft leftValue, TRight rightValue) => TryAdd(leftValue, rightValue, out _);

        /// <inheritdoc cref="TryAdd(TLeft, TAlternateRight, out TRight)"/>
        public bool TryAdd(TLeft leftValue, TAlternateRight rightValue) => TryAdd(leftValue, rightValue, out _);

        /// <summary>
        /// Attempts to add an association to map between the specified alternate left value and alternate right value.
        /// </summary>
        /// <returns><see langword="true"/> if the association was added, otherwise <see langword="false"/> if the left or right value was already present in
        /// the map.</returns>
        public bool TryAdd(
            TAlternateLeft leftValue,
            TAlternateRight rightValue,
            [MaybeNullWhen(false)] out TLeft actualLeftValue,
            [MaybeNullWhen(false)] out TRight actualRightValue)
        {
            if (_altLeftSide.ContainsKey(leftValue) || _altRightSide.ContainsKey(rightValue))
            {
                actualLeftValue = default;
                actualRightValue = default;

                return false;
            }

            actualLeftValue = LeftComparer.Create(leftValue);
            actualRightValue = RightComparer.Create(rightValue);

            _altLeftSide.Dictionary.Add(actualLeftValue, actualRightValue);
            _altRightSide.Dictionary.Add(actualRightValue, actualLeftValue);

            return true;
        }

        /// <summary>
        /// Attempts to add an association to map between the specified alternate left value and right value.
        /// </summary>
        /// <returns><see langword="true"/> if the association was added, otherwise <see langword="false"/> if the left or right value was already present in
        /// the map.</returns>
        public bool TryAdd(TAlternateLeft leftValue, TRight rightValue, [MaybeNullWhen(false)] out TLeft actualLeftValue)
        {
            if (_altLeftSide.ContainsKey(leftValue) || !_altRightSide.Dictionary.ContainsKey(rightValue))
            {
                actualLeftValue = default;
                return false;
            }

            actualLeftValue = LeftComparer.Create(leftValue);

            _altLeftSide.Dictionary.Add(actualLeftValue, rightValue);
            _altRightSide.Dictionary.Add(rightValue, actualLeftValue);

            return true;
        }

        /// <summary>
        /// Attempts to add an association to map between the specified left value and alternate right value.
        /// </summary>
        /// <returns><see langword="true"/> if the association was added, otherwise <see langword="false"/> if the left or right value was already present in
        /// the map.</returns>
        public bool TryAdd(TLeft leftValue, TAlternateRight rightValue, [MaybeNullWhen(false)] out TRight actualRightValue)
        {
            if (_altLeftSide.Dictionary.ContainsKey(leftValue) || _altRightSide.ContainsKey(rightValue))
            {
                actualRightValue = default;
                return false;
            }

            actualRightValue = RightComparer.Create(rightValue);

            _altLeftSide.Dictionary.Add(leftValue, actualRightValue);
            _altRightSide.Dictionary.Add(actualRightValue, leftValue);

            return true;
        }

        /// <inheritdoc cref="TryGetRightValue(TAlternateLeft, out TLeft, out TRight)"/>
        public bool TryGetRightValue(TAlternateLeft leftValue, [MaybeNullWhen(false)] out TRight rightValue) =>
            _altLeftSide.TryGetValue(leftValue, out rightValue);

        /// <summary>
        /// Gets the right value associated with the specified alternate left value.
        /// </summary>
        public bool TryGetRightValue(TAlternateLeft leftValue, [MaybeNullWhen(false)] out TLeft actualLeftValue, [MaybeNullWhen(false)] out TRight rightValue) =>
            _altLeftSide.TryGetValue(leftValue, out actualLeftValue, out rightValue);

        /// <inheritdoc cref="TryGetLeftValue(TAlternateRight, out TRight, out TLeft)"/>
        public bool TryGetLeftValue(TAlternateRight rightValue, [MaybeNullWhen(false)] out TLeft leftValue) =>
            _altRightSide.TryGetValue(rightValue, out leftValue);

        /// <summary>
        /// Gets the left value associated with the specified alternate right value.
        /// </summary>
        public bool TryGetLeftValue(TAlternateRight rightValue, [MaybeNullWhen(false)] out TRight actualRightValue, [MaybeNullWhen(false)] out TLeft leftValue) =>
            _altRightSide.TryGetValue(rightValue, out actualRightValue, out leftValue);
    }

    /// <summary>
    /// Provides a type that can be used to perform operations on a <see cref="Map{TLeft, TRight}"/> using <typeparamref name="TAlternateLeft"/> instead of
    /// <typeparamref name="TLeft"/>.
    /// </summary>
    public readonly struct AlternateLeftLookup<TAlternateLeft> where TAlternateLeft : notnull, allows ref struct
    {
        private readonly Map<TLeft, TRight> _map;
        private readonly Dictionary<TLeft, TRight>.AlternateLookup<TAlternateLeft> _altLeftSide;

        internal AlternateLeftLookup(Map<TLeft, TRight> map, Dictionary<TLeft, TRight>.AlternateLookup<TAlternateLeft> altLeftSide)
        {
            _map = map;
            _altLeftSide = altLeftSide;
        }

        /// <summary>
        /// Gets or sets the right value associated with the specified alternate left value.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The property is being retrieved and the left value was not found.</exception>
        /// <exception cref="ArgumentException">The property is being set and the right value provided is already assigned to a different left
        /// value.</exception>
        public TRight this[TAlternateLeft leftValue]
        {
            get => _altLeftSide[leftValue];
            set {
                if (_map._rightSide.TryGetValue(value, out var existingLeftValue))
                {
                    if (!LeftComparer.Equals(leftValue, existingLeftValue))
                        Throw.Arg("Duplicate right value in the map.");

                    return;
                }

                if (_altLeftSide.TryGetValue(leftValue, out var actualLeftValue, out var existingRightValue))
                    _map._rightSide.Remove(existingRightValue);
                else
                    actualLeftValue = LeftComparer.Create(leftValue);

                _altLeftSide.Dictionary[actualLeftValue] = value;
                _map._rightSide.Add(value, actualLeftValue);
            }
        }

        /// <summary>
        /// Gets the alternate equality comparer used for left values.
        /// </summary>
        public IAlternateEqualityComparer<TAlternateLeft, TLeft> LeftComparer =>
            Unsafe.As<IAlternateEqualityComparer<TAlternateLeft, TLeft>>(_altLeftSide.Dictionary.Comparer);

        /// <summary>
        /// Gets the map associated with this alternate lookup.
        /// </summary>
        public Map<TLeft, TRight> Map => _map;

        /// <summary>
        /// Gets a value indicating if the map contains an association between the specified alternate left value and right value.
        /// </summary>
        public bool Contains(TAlternateLeft leftValue, TRight rightValue) =>
            _altLeftSide.TryGetValue(leftValue, out TRight right) && _map._rightSide.Comparer.Equals(rightValue, right);

        /// <summary>
        /// Gets a value indicating if the map contains an association between the specified alternate left value and right value.
        /// </summary>
        public bool Contains(TAlternateLeft leftValue, TRight rightValue, [MaybeNullWhen(false)] out TLeft actualLeftValue) =>
            _altLeftSide.TryGetValue(leftValue, out actualLeftValue, out TRight right) && _map._rightSide.Comparer.Equals(rightValue, right);

        /// <summary>
        /// Determines whether the map contains the specified alternate left value.
        /// </summary>
        public bool ContainsLeft(TAlternateLeft leftValue) => _altLeftSide.ContainsKey(leftValue);

        /// <summary>
        /// Determines whether the map contains the specified alternate left value.
        /// </summary>
        public bool ContainsLeft(TAlternateLeft leftValue, [MaybeNullWhen(false)] out TLeft actualLeftValue) =>
            _altLeftSide.TryGetValue(leftValue, out actualLeftValue, out _);

        /// <inheritdoc cref="Remove(TAlternateLeft, TRight, out TLeft)"/>
        public bool Remove(TAlternateLeft leftValue, TRight rightValue) => Remove(leftValue, rightValue, out _);

        /// <summary>
        /// Removes an association from the map between the specified alternate left and alternate right value if they currently map to each other.
        /// </summary>
        /// <returns><see langword="true"/> if removal was successful, otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// If the left and right values do not map to each other then no changes are made to the map and the removal does not succeed.
        /// </remarks>
        public bool Remove(
            TAlternateLeft leftValue,
            TRight rightValue,
            [MaybeNullWhen(false)] out TLeft actualLeftValue)
        {
            if (!Contains(leftValue, rightValue, out actualLeftValue))
                return false;

            _map._leftSide.Remove(actualLeftValue);
            _map._rightSide.Remove(rightValue);

            return true;
        }

        /// <summary>
        /// Removes the mapping with the specified alternate left value.
        /// </summary>
        public bool RemoveLeft(TAlternateLeft leftValue)
        {
            if (_altLeftSide.Remove(leftValue, out _, out TRight rightValue))
            {
                bool result = _map._rightSide.Remove(rightValue);
                Debug.Assert(result, "Right side removal failed after left side removal via alternate key.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the mapping with the specified alternate left value.
        /// </summary>
        public bool RemoveLeft(TAlternateLeft leftKey, [MaybeNullWhen(false)] out TLeft actualLeftKey, [MaybeNullWhen(false)] out TRight rightValue)
        {
            if (_altLeftSide.Remove(leftKey, out actualLeftKey, out rightValue))
            {
                bool result = _map._rightSide.Remove(rightValue);
                Debug.Assert(result, "Right side removal failed after left side removal via alternate key.");
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="Set(TAlternateLeft, TRight, out TLeft)"/>
        public void Set(TAlternateLeft leftValue, TRight rightValue) => Set(leftValue, rightValue, out _);

        /// <summary>
        /// Sets an association in the map between the specified alternate left value and right value, overriding any existing associations these values had.
        /// </summary>
        /// <remarks>
        /// This method is functionally equivalent to removing any existing associations for the left and right values and then adding the new association, so
        /// it is guaranteed to succeed.
        /// </remarks>
        public void Set(TAlternateLeft leftValue, TRight rightValue, out TLeft actualLeftValue)
        {
            if (_altLeftSide.Remove(leftValue, out actualLeftValue!, out var existingRightValue))
                _map._rightSide.Remove(existingRightValue);
            else
                actualLeftValue = LeftComparer.Create(leftValue);

            if (_map._rightSide.Remove(rightValue, out var existingLeftValue))
                _altLeftSide.Dictionary.Remove(existingLeftValue);

            _altLeftSide.Dictionary.Add(actualLeftValue, rightValue);
            _map._rightSide.Add(rightValue, actualLeftValue);
        }

        /// <inheritdoc cref="TryAdd(TAlternateLeft, TRight, out TLeft)"/>
        public bool TryAdd(TAlternateLeft leftValue, TRight rightValue) => TryAdd(leftValue, rightValue, out _);

        /// <summary>
        /// Attempts to add an association to map between the specified alternate left value and right value.
        /// </summary>
        /// <returns><see langword="true"/> if the association was added, otherwise <see langword="false"/> if the left or right value was already present in
        /// the map.</returns>
        public bool TryAdd(TAlternateLeft leftValue, TRight rightValue, [MaybeNullWhen(false)] out TLeft actualLeftValue)
        {
            if (_altLeftSide.ContainsKey(leftValue) || !_map._rightSide.ContainsKey(rightValue))
            {
                actualLeftValue = default;
                return false;
            }

            actualLeftValue = LeftComparer.Create(leftValue);

            _altLeftSide.Dictionary.Add(actualLeftValue, rightValue);
            _map._rightSide.Add(rightValue, actualLeftValue);

            return true;
        }

        /// <inheritdoc cref="TryGetRightValue(TAlternateLeft, out TLeft, out TRight)"/>
        public bool TryGetRightValue(TAlternateLeft leftValue, [MaybeNullWhen(false)] out TRight rightValue) =>
            _altLeftSide.TryGetValue(leftValue, out rightValue);

        /// <summary>
        /// Gets the right value associated with the specified alternate left value.
        /// </summary>
        public bool TryGetRightValue(TAlternateLeft leftValue, [MaybeNullWhen(false)] out TLeft actualLeftValue, [MaybeNullWhen(false)] out TRight rightValue) =>
            _altLeftSide.TryGetValue(leftValue, out actualLeftValue, out rightValue);
    }
}

#endif