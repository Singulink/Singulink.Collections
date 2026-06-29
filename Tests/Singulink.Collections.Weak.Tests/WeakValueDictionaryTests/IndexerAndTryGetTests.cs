namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class IndexerAndTryGetTests
{
    [TestMethod]
    public void IndexerGetReturnsValue()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary[1].ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IndexerGetMissingKeyThrowsKeyNotFound()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        Should.Throw<KeyNotFoundException>(() => _ = dictionary[1]);
    }

    [TestMethod]
    public void IndexerSetInsertsNewEntry()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();

        dictionary[1] = value;

        dictionary.UnsafeCount.ShouldBe(1);
        dictionary[1].ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IndexerSetOverwritesExistingEntry()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object oldValue = new();
        object newValue = new();
        dictionary[1] = oldValue;

        dictionary[1] = newValue;

        dictionary.UnsafeCount.ShouldBe(1);
        dictionary[1].ShouldBeSameAs(newValue);
        dictionary.ContainsValue(oldValue).ShouldBeFalse();

        GC.KeepAlive(oldValue);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryGetValuePresent()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryGetValueMissingReturnsFalseAndNull()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        dictionary.TryGetValue(1, out object? current).ShouldBeFalse();
        current.ShouldBeNull();
    }

    [TestMethod]
    public void TryGetValueAfterValueCollectedReturnsFalse()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        var valueRef = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object value = new();
            dictionary.TryAdd(1, value);
            return new WeakReference<object>(value);
        });

        Helpers.ForceGC();

        // Precondition: the value must actually have been collected for this test to be meaningful.
        valueRef.TryGetTarget(out _).ShouldBeFalse();

        dictionary.TryGetValue(1, out object? current).ShouldBeFalse();
        current.ShouldBeNull();

        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void IndexerGetNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        Should.Throw<ArgumentNullException>(() => _ = dictionary[null!]);
    }

    [TestMethod]
    public void IndexerSetNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();

        Should.Throw<ArgumentNullException>(() => dictionary[null!] = value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IndexerSetNullValueThrows()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        Should.Throw<ArgumentNullException>(() => dictionary[1] = null!);
    }

    [TestMethod]
    public void TryGetValueNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        Should.Throw<ArgumentNullException>(() => dictionary.TryGetValue(null!, out _));
    }
}
