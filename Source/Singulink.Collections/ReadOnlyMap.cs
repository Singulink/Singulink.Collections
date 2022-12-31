using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections;

/// <inheritdoc cref="IReadOnlyMap{TLeft, TRight}"/>
public class ReadOnlyMap<TLeft, TRight> : IMap<TLeft, TRight>, IReadOnlyMap<TLeft, TRight>
    where TLeft : notnull
    where TRight : notnull
{
    private readonly IMap<TLeft, TRight> _map;
    private ReadOnlyMap<TRight, TLeft>? _reverseMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyMap{TLeft, TRight}"/> class.
    /// </summary>
    public ReadOnlyMap(IMap<TLeft, TRight> map)
    {
        _map = map;
    }

    /// <inheritdoc cref="IReadOnlyMap{TLeft, TRight}.this[TLeft]"/>
    public TRight this[TLeft leftValue] => _map[leftValue];

    /// <inheritdoc cref="Map{TLeft, TRight}.Count"/>
    public int Count => _map.Count;

    /// <inheritdoc cref="Map{TLeft, TRight}.LeftValues"/>
    public IReadOnlyCollection<TLeft> LeftValues => _map.LeftValues;

    /// <inheritdoc cref="Map{TLeft, TRight}.Reverse"/>
    public IReadOnlyMap<TRight, TLeft> Reverse => _reverseMap ??= new(_map.Reverse);

    /// <inheritdoc cref="Map{TLeft, TRight}.RightValues"/>
    public IReadOnlyCollection<TRight> RightValues => _map.RightValues;

    /// <inheritdoc cref="Map{TLeft, TRight}.Contains(TLeft, TRight)"/>
    public bool Contains(TLeft leftValue, TRight rightValue) => _map.Contains(leftValue, rightValue);

    /// <inheritdoc cref="Map{TLeft, TRight}.ContainsLeft(TLeft)"/>
    public bool ContainsLeft(TLeft leftValue) => _map.ContainsLeft(leftValue);

    /// <inheritdoc cref="Map{TLeft, TRight}.ContainsRight(TRight)"/>
    public bool ContainsRight(TRight rightValue) => _map.ContainsRight(rightValue);

    /// <inheritdoc cref="Map{TLeft, TRight}.TryGetLeftValue(TRight, out TLeft)"/>
    public bool TryGetLeftValue(TRight rightValue, [MaybeNullWhen(false)] out TLeft leftValue) => _map.TryGetLeftValue(rightValue, out leftValue);

    /// <inheritdoc cref="Map{TLeft, TRight}.TryGetRightValue(TLeft, out TRight)"/>
    public bool TryGetRightValue(TLeft leftValue, [MaybeNullWhen(false)] out TRight rightValue) => _map.TryGetRightValue(leftValue, out rightValue);

    /// <inheritdoc cref="Map{TLeft, TRight}.GetEnumerator"/>
    public IEnumerator<KeyValuePair<TLeft, TRight>> GetEnumerator() => _map.GetEnumerator();

    #region Explicit Interface Implemenetations

    /// <inheritdoc cref="this[TLeft]"/>
    TRight IMap<TLeft, TRight>.this[TLeft leftValue]
    {
        get => this[leftValue];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets a value indicating whether this map is read-only. Always returns <see langword="true"/>.
    /// </summary>
    bool ICollection<KeyValuePair<TLeft, TRight>>.IsReadOnly => true;

    /// <inheritdoc cref="Reverse"/>
    IMap<TRight, TLeft> IMap<TLeft, TRight>.Reverse => _reverseMap ??= new(_map.Reverse);

    /// <inheritdoc cref="Contains(TLeft, TRight)"/>
    bool ICollection<KeyValuePair<TLeft, TRight>>.Contains(KeyValuePair<TLeft, TRight> item) => Contains(item.Key, item.Value);

    /// <summary>
    /// Copies the left and right value pairs to an array starting at the specified array index.
    /// </summary>
    public void CopyTo(KeyValuePair<TLeft, TRight>[] array, int arrayIndex) => _map.CopyTo(array, arrayIndex);

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Not Supported

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void IMap<TLeft, TRight>.Add(TLeft leftValue, TRight rightValue) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<KeyValuePair<TLeft, TRight>>.Add(KeyValuePair<TLeft, TRight> item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<KeyValuePair<TLeft, TRight>>.Clear() => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    bool IMap<TLeft, TRight>.Remove(TLeft leftValue, TRight rightValue) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    bool ICollection<KeyValuePair<TLeft, TRight>>.Remove(KeyValuePair<TLeft, TRight> item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    bool IMap<TLeft, TRight>.RemoveLeft(TLeft leftValue) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    bool IMap<TLeft, TRight>.RemoveRight(TRight rightValue) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void IMap<TLeft, TRight>.Set(TLeft leftValue, TRight rightValue) => throw new NotSupportedException();

    #endregion
}