using System.Linq;

namespace Singulink.Collections;

/// <summary>
/// Represents a read-only collection of values associated with a key.
/// </summary>
/// <typeparam name="TKey">The type of the key associated with the collection.</typeparam>
/// <typeparam name="TValue">The type of the values in the collection.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IGrouping{TKey, TElement}"/> with the additional <see cref="IReadOnlyCollection{T}.Count"/> property, allowing keyed
/// collections to be used directly with LINQ operators that produce or consume <see cref="IGrouping{TKey, TElement}"/> sequences (such as <see
/// cref="Enumerable.GroupBy{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey})"/> and <see cref="Enumerable.ToLookup{TSource, TKey}(IEnumerable{TSource},
/// Func{TSource, TKey})"/>).
/// </para>
/// </remarks>
public interface IReadOnlyKeyedCollection<out TKey, out TValue> : IGrouping<TKey, TValue>, IReadOnlyCollection<TValue>
{
}
