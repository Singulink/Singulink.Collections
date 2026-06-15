namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class ComparerTests
{
    [TestMethod]
    public void DefaultComparerReturnsDefault()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        dictionary.Comparer.ShouldBeSameAs(EqualityComparer<string>.Default);
    }

    [TestMethod]
    public void ComparerPropertyReturnsProvidedComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var dictionary = new WeakValueDictionary<string, object>(comparer);

        dictionary.Comparer.ShouldBeSameAs(comparer);
    }

    [TestMethod]
    public void CustomComparerUsedForKeyLookups()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object value = new();
        dictionary.TryAdd("key", value);

        dictionary.ContainsKey("KEY").ShouldBeTrue();
        dictionary.TryGetValue("KEY", out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void CustomComparerAffectsDuplicateDetection()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object value1 = new();
        object value2 = new();

        dictionary.TryAdd("key", value1).ShouldBeTrue();
        dictionary.TryAdd("KEY", value2).ShouldBeFalse();

        dictionary.TryGetValue("key", out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void CustomComparerUsedForRemoval()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object value = new();
        dictionary.TryAdd("key", value);

        dictionary.Remove("KEY").ShouldBeTrue();
        dictionary.ContainsKey("key").ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void CustomComparerUsedForIndexerGet()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object value = new();
        dictionary.TryAdd("key", value);

        dictionary["KEY"].ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void CustomComparerUsedForIndexerSet()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object oldValue = new();
        object newValue = new();
        dictionary.TryAdd("key", oldValue);

        // Setting via a differently-cased key should overwrite the existing entry rather than insert a new one.
        dictionary["KEY"] = newValue;

        dictionary.UnsafeCount.ShouldBe(1);
        dictionary["key"].ShouldBeSameAs(newValue);

        GC.KeepAlive(oldValue);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void CustomComparerUsedForAdd()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object value1 = new();
        object value2 = new();
        dictionary.Add("key", value1);

        Should.Throw<ArgumentException>(() => dictionary.Add("KEY", value2));

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void CustomComparerUsedForRemoveOutValue()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object value = new();
        dictionary.TryAdd("key", value);

        dictionary.Remove("KEY", out object? removed).ShouldBeTrue();
        removed.ShouldBeSameAs(value);
        dictionary.ContainsKey("key").ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void CustomComparerUsedForRemoveKeyValue()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object value = new();
        dictionary.TryAdd("key", value);

        dictionary.Remove("KEY", value).ShouldBeTrue();
        dictionary.ContainsKey("key").ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void CustomComparerUsedForContains()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object value = new();
        dictionary.TryAdd("key", value);

        dictionary.Contains("KEY", value).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void CustomComparerUsedForContainsKeyValuePair()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        object value = new();
        dictionary.TryAdd("key", value);

        dictionary.Contains(new KeyValuePair<string, object>("KEY", value)).ShouldBeTrue();

        GC.KeepAlive(value);
    }
}
