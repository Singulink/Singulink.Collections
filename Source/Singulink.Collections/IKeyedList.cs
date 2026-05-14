namespace Singulink.Collections;

/// <summary>
/// Represents a list of values associated with a key.
/// </summary>
/// <typeparam name="TKey">The type of the key associated with the list.</typeparam>
/// <typeparam name="TValue">The type of the values in the list.</typeparam>
public interface IKeyedList<TKey, TValue> : IReadOnlyKeyedList<TKey, TValue>, IKeyedCollection<TKey, TValue>, IList<TValue>
{
    /// <inheritdoc cref="IKeyedCollection{TKey, TValue}.Count"/>
    new int Count { get; }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    new TValue this[int index] { get; set; }
}
