namespace Singulink.Collections.Internal;

internal sealed class ReadOnlyListDictionaryWrapper<TKey, TValue> :
    ReadOnlyCollectionDictionaryWrapper<TKey, TValue, IReadOnlyList<TValue>, IReadOnlyList<TValue>>,
    IReadOnlyListDictionary<TKey, TValue>
{
    public ReadOnlyListDictionaryWrapper(IReadOnlyCollectionDictionary<TKey, TValue, IReadOnlyList<TValue>> dictionary) : base(dictionary)
    {
    }
}