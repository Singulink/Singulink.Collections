namespace Singulink.Collections.Internal;

internal sealed class ReadOnlyListDictionary<TKey, TValue> :
    ReadOnlyCollectionDictionary<TKey, TValue, IList<TValue>, IReadOnlyList<TValue>>,
    IReadOnlyListDictionary<TKey, TValue>
{
    public ReadOnlyListDictionary(ICollectionDictionary<TKey, TValue, IList<TValue>> dictionary) : base(dictionary)
    {
    }
}