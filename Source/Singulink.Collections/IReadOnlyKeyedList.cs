namespace Singulink.Collections;

/// <summary>
/// Represents a read-only list of values associated with a key.
/// </summary>
/// <typeparam name="TKey">The type of the key associated with the list.</typeparam>
/// <typeparam name="TValue">The type of the values in the list.</typeparam>
public interface IReadOnlyKeyedList<out TKey, out TValue> : IReadOnlyKeyedCollection<TKey, TValue>, IReadOnlyList<TValue>
{
}
