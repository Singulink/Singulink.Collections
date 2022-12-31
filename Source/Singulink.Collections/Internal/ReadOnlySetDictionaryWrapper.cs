namespace Singulink.Collections.Internal;

internal sealed class ReadOnlySetDictionaryWrapper<TKey, TValue> :
    ReadOnlyCollectionDictionaryWrapper<TKey, TValue, IReadOnlySet<TValue>, IReadOnlySet<TValue>>,
    IReadOnlySetDictionary<TKey, TValue>
{
    public ReadOnlySetDictionaryWrapper(IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlySet<TValue>> dictionary) : base(dictionary)
    {
    }
}