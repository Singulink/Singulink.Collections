namespace Singulink.Collections;

#if NETSTANDARD2_0

internal static class PolyfillCollectionExtensions
{
    public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value) where TKey : notnull
    {
        if (!dictionary.TryGetValue(key, out value))
            return false;

        dictionary.Remove(key);
        return true;
    }

    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            return true;
        }

        return false;
    }
}

#endif