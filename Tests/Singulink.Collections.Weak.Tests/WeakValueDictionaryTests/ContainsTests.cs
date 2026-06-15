namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class ContainsTests
{
    [TestMethod]
    public void ContainsKeyReturnsTrueWhenPresent()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.ContainsKey(1).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKeyReturnsFalseWhenMissing()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        dictionary.ContainsKey(1).ShouldBeFalse();
    }

    [TestMethod]
    public void ContainsValueReturnsTrueWhenPresent()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.ContainsValue(value).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsValueReturnsFalseWhenMissing()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        object other = new();
        dictionary.TryAdd(1, value);

        dictionary.ContainsValue(other).ShouldBeFalse();

        GC.KeepAlive(value);
        GC.KeepAlive(other);
    }

    [TestMethod]
    public void ContainsValueWithComparer()
    {
        var dictionary = new WeakValueDictionary<int, string>();
        string value = "hello";
        dictionary.TryAdd(1, value);

        dictionary.ContainsValue("HELLO", StringComparer.OrdinalIgnoreCase).ShouldBeTrue();
        dictionary.ContainsValue("HELLO", StringComparer.Ordinal).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKeyValueDefaultComparerMatch()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.Contains(1, value).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKeyValueDefaultComparerNonMatchReturnsFalse()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        object other = new();
        dictionary.TryAdd(1, value);

        dictionary.Contains(1, other).ShouldBeFalse();

        GC.KeepAlive(value);
        GC.KeepAlive(other);
    }

    [TestMethod]
    public void ContainsKeyValueWithComparer()
    {
        var dictionary = new WeakValueDictionary<int, string>();
        string value = "hello";
        dictionary.TryAdd(1, value);

        dictionary.Contains(1, "HELLO", StringComparer.OrdinalIgnoreCase).ShouldBeTrue();
        dictionary.Contains(1, "HELLO", StringComparer.Ordinal).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKeyValuePairWrapper()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        object other = new();
        dictionary.TryAdd(1, value);

        dictionary.Contains(new KeyValuePair<int, object>(1, value)).ShouldBeTrue();
        dictionary.Contains(new KeyValuePair<int, object>(1, other)).ShouldBeFalse();
        dictionary.Contains(new KeyValuePair<int, object>(2, value)).ShouldBeFalse();

        GC.KeepAlive(value);
        GC.KeepAlive(other);
    }

    [TestMethod]
    public void ContainsKeyValuePairWithComparer()
    {
        var dictionary = new WeakValueDictionary<int, string>();
        string value = "hello";
        dictionary.TryAdd(1, value);

        dictionary.Contains(new KeyValuePair<int, string>(1, "HELLO"), StringComparer.OrdinalIgnoreCase).ShouldBeTrue();
        dictionary.Contains(new KeyValuePair<int, string>(1, "HELLO"), StringComparer.Ordinal).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKeyNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        Should.Throw<ArgumentNullException>(() => dictionary.ContainsKey(null!));
    }

    [TestMethod]
    public void ContainsKeyValueNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();

        Should.Throw<ArgumentNullException>(() => dictionary.Contains(null!, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKeyValueNullValueThrows()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        Should.Throw<ArgumentNullException>(() => dictionary.Contains(1, null!));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKvpNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();

        Should.Throw<ArgumentNullException>(() => dictionary.Contains(new KeyValuePair<string, object>(null!, value)));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKvpNullValueThrows()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        Should.Throw<ArgumentNullException>(() => dictionary.Contains(new KeyValuePair<int, object>(1, null!)));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsValueNullThrows()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        Should.Throw<ArgumentNullException>(() => dictionary.ContainsValue(null!));
    }
}
