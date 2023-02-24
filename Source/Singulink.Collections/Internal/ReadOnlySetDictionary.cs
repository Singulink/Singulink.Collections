namespace Singulink.Collections.Internal;

internal sealed class ReadOnlySetDictionary<TKey, TValue> :
    ReadOnlyCollectionDictionary<TKey, TValue, ISet<TValue>, IReadOnlySet<TValue>>,
    IReadOnlySetDictionary<TKey, TValue>
{
    public ReadOnlySetDictionary(ICollectionDictionary<TKey, TValue, ISet<TValue>> dictionary) : base(dictionary)
    {
    }
}