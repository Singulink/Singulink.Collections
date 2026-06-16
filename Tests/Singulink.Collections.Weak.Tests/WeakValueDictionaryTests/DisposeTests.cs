namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class DisposeTests
{
    [TestMethod]
    public void DisposeEmptyDictionarySucceeds()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        Should.NotThrow(() => dictionary.Dispose());
    }

    [TestMethod]
    public void DisposePopulatedDictionarySucceeds()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        dictionary.TryAdd(1, value1);
        dictionary.TryAdd(2, value2);
        dictionary.TryAdd(3, value3);

        // Disposing while entries still hold live values must succeed (these entries have live internal node helpers).
        Should.NotThrow(() => dictionary.Dispose());

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void DoubleDisposeSucceeds()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.Dispose();
        Should.NotThrow(() => dictionary.Dispose());

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryGetValueThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => dictionary.TryGetValue(1, out _));
    }

    [TestMethod]
    public void IndexerGetThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => _ = dictionary[1]);
    }

    [TestMethod]
    public void IndexerSetThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();
        object value = new();

        Should.Throw<ObjectDisposedException>(() => dictionary[1] = value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryAddThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();
        object value = new();

        Should.Throw<ObjectDisposedException>(() => dictionary.TryAdd(1, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();
        object value = new();

        Should.Throw<ObjectDisposedException>(() => dictionary.Add(1, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveKeyThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => dictionary.Remove(1));
    }

    [TestMethod]
    public void RemoveKeyOutValueThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => dictionary.Remove(1, out _));
    }

    [TestMethod]
    public void RemoveKeyValueThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();
        object value = new();

        Should.Throw<ObjectDisposedException>(() => dictionary.Remove(1, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();
        object value = new();

        Should.Throw<ObjectDisposedException>(() => dictionary.Contains(1, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKeyValuePairThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();
        object value = new();

        Should.Throw<ObjectDisposedException>(() => dictionary.Contains(new KeyValuePair<int, object>(1, value)));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKeyThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => dictionary.ContainsKey(1));
    }

    [TestMethod]
    public void ContainsValueThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();
        object value = new();

        Should.Throw<ObjectDisposedException>(() => dictionary.ContainsValue(value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ClearThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => dictionary.Clear());
    }

    [TestMethod]
    public void ComparerThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => _ = dictionary.Comparer);
    }

    [TestMethod]
    public void UnsafeCountThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => _ = dictionary.UnsafeCount);
    }

    [TestMethod]
    public void EnumerationThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() =>
        {
            foreach (var entry in dictionary)
                GC.KeepAlive(entry);
        });
    }

    [TestMethod]
    public void KeysEnumerationThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => dictionary.Keys.ToList());
    }

    [TestMethod]
    public void ValuesEnumerationThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => dictionary.Values.ToList());
    }

#if NET9_0_OR_GREATER
    [TestMethod]
    public void GetAlternateLookupThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => dictionary.GetAlternateLookup<ReadOnlySpan<char>>());
    }

    [TestMethod]
    public void TryGetAlternateLookupThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => dictionary.TryGetAlternateLookup<ReadOnlySpan<char>>(out _));
    }

    [TestMethod]
    public void AlternateLookupTryGetValueThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => lookup.TryGetValue("key".AsSpan(), out _));
    }

    [TestMethod]
    public void AlternateLookupIndexerSetThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        dictionary.Dispose();
        object value = new();

        Should.Throw<ObjectDisposedException>(() => lookup["key".AsSpan()] = value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AlternateLookupTryAddThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        dictionary.Dispose();
        object value = new();

        Should.Throw<ObjectDisposedException>(() => lookup.TryAdd("key".AsSpan(), value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AlternateLookupContainsKeyThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => lookup.ContainsKey("key".AsSpan()));
    }

    [TestMethod]
    public void AlternateLookupRemoveThrowsAfterDispose()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        dictionary.Dispose();

        Should.Throw<ObjectDisposedException>(() => lookup.Remove("key".AsSpan()));
    }
#endif
}
