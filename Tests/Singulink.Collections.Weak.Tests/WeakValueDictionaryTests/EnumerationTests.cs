namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class EnumerationTests
{
    [TestMethod]
    public void EnumerateEmpty()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        dictionary.ToList().ShouldBeEmpty();
    }

    [TestMethod]
    public void EnumerateOneEntry()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.ToList().ShouldBe([new(1, value)]);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void EnumerateManyEntries()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        dictionary.TryAdd(1, value1);
        dictionary.TryAdd(2, value2);
        dictionary.TryAdd(3, value3);

        dictionary.ToList().ShouldBe(
            [new(1, value1), new(2, value2), new(3, value3)],
            ignoreOrder: true);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void NonGenericEnumeratorYieldsEntries()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        var entries = new List<KeyValuePair<int, object>>();

        foreach (KeyValuePair<int, object> entry in (System.Collections.IEnumerable)dictionary)
            entries.Add(entry);

        entries.ShouldBe([new(1, value)]);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void KeysReturnsAllKeys()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        dictionary.TryAdd(1, value1);
        dictionary.TryAdd(2, value2);
        dictionary.TryAdd(3, value3);

        dictionary.Keys.ShouldBe([1, 2, 3], ignoreOrder: true);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void ValuesReturnsAllValues()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        dictionary.TryAdd(1, value1);
        dictionary.TryAdd(2, value2);
        dictionary.TryAdd(3, value3);

        dictionary.Values.ShouldBe([value1, value2, value3], ignoreOrder: true);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void UnsafeCountTracksAddsAndRemoves()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value1 = new();
        object value2 = new();

        dictionary.UnsafeCount.ShouldBe(0);

        dictionary.TryAdd(1, value1);
        dictionary.UnsafeCount.ShouldBe(1);

        dictionary.TryAdd(2, value2);
        dictionary.UnsafeCount.ShouldBe(2);

        dictionary.Remove(1);
        dictionary.UnsafeCount.ShouldBe(1);

        dictionary.Clear();
        dictionary.UnsafeCount.ShouldBe(0);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void EnumerationSkipsCollectedValues()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        var liveValues = new List<object>();

        for (int i = 0; i < 5; i++)
        {
            object value = new();
            liveValues.Add(value);
            dictionary.TryAdd(i, value);
        }

        Helpers.NotInlined(dictionary, (dictionary) =>
        {
            for (int i = 5; i < 10; i++)
                dictionary.TryAdd(i, new object());
        });

        Helpers.ForceGC();

        // Only the entries whose values are still alive should be enumerated.
        dictionary.Select((kvp) => kvp.Value).ShouldBe(liveValues, ignoreOrder: true);
        dictionary.Keys.ShouldBe([0, 1, 2, 3, 4], ignoreOrder: true);

        GC.KeepAlive(liveValues);
        GC.KeepAlive(dictionary);
    }
}
