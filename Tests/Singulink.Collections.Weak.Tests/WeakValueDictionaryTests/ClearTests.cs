namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class ClearTests
{
    [TestMethod]
    public void ClearEmptyDictionary()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        dictionary.Clear();

        dictionary.UnsafeCount.ShouldBe(0);
        dictionary.ToList().ShouldBeEmpty();
    }

    [TestMethod]
    public void ClearOneEntry()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.Clear();

        dictionary.UnsafeCount.ShouldBe(0);
        dictionary.ContainsKey(1).ShouldBeFalse();
        dictionary.ToList().ShouldBeEmpty();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ClearManyEntries()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        var values = Enumerable.Range(0, 100).Select((_) => new object()).ToList();

        for (int i = 0; i < values.Count; i++)
            dictionary.TryAdd(i, values[i]);

        dictionary.Clear();

        dictionary.UnsafeCount.ShouldBe(0);
        dictionary.ToList().ShouldBeEmpty();

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void ClearTwice()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        var values = Enumerable.Range(0, 10).Select((_) => new object()).ToList();

        for (int i = 0; i < values.Count; i++)
            dictionary.TryAdd(i, values[i]);

        dictionary.Clear();
        dictionary.UnsafeCount.ShouldBe(0);
        dictionary.ToList().ShouldBeEmpty();

        dictionary.Clear(); // Should be safe to call again
        dictionary.UnsafeCount.ShouldBe(0);
        dictionary.ToList().ShouldBeEmpty();

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void ClearThenAddNewEntries()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        var oldValues = Enumerable.Range(0, 10).Select((_) => new object()).ToList();

        for (int i = 0; i < oldValues.Count; i++)
            dictionary.TryAdd(i, oldValues[i]);

        dictionary.Clear();
        dictionary.UnsafeCount.ShouldBe(0);
        dictionary.ToList().ShouldBeEmpty();

        object newValue1 = new();
        object newValue2 = new();
        dictionary.TryAdd(1, newValue1);
        dictionary.TryAdd(2, newValue2);

        dictionary.UnsafeCount.ShouldBe(2);
        dictionary.ToList().ShouldBe([new(1, newValue1), new(2, newValue2)], ignoreOrder: true);

        GC.KeepAlive(oldValues);
        GC.KeepAlive(newValue1);
        GC.KeepAlive(newValue2);
    }
}
