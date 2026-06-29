namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class RemoveTests
{
    [TestMethod]
    public void RemoveKeyPresentReturnsTrue()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.Remove(1).ShouldBeTrue();

        dictionary.UnsafeCount.ShouldBe(0);
        dictionary.ContainsKey(1).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveKeyMissingReturnsFalse()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        dictionary.Remove(1).ShouldBeFalse();
    }

    [TestMethod]
    public void RemoveKeyTwiceReturnsFalseSecondTime()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.Remove(1).ShouldBeTrue();
        dictionary.Remove(1).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveKeyOutValueReturnsValue()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        dictionary.Remove(1, out object? removed).ShouldBeTrue();
        removed.ShouldBeSameAs(value);
        dictionary.ContainsKey(1).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveKeyOutValueMissingReturnsFalseAndNull()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        dictionary.Remove(1, out object? removed).ShouldBeFalse();
        removed.ShouldBeNull();
    }

    [TestMethod]
    public void RemoveKeyValueWithComparerMatchRemoves()
    {
        var dictionary = new WeakValueDictionary<int, string>();
        string value = "hello";
        dictionary.TryAdd(1, value);

        dictionary.Remove(1, "HELLO", StringComparer.OrdinalIgnoreCase).ShouldBeTrue();
        dictionary.ContainsKey(1).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveKeyValueWithComparerNonMatchKeepsEntry()
    {
        var dictionary = new WeakValueDictionary<int, string>();
        string value = "hello";
        dictionary.TryAdd(1, value);

        dictionary.Remove(1, "world", StringComparer.OrdinalIgnoreCase).ShouldBeFalse();
        dictionary.ContainsKey(1).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveKeyValueMissingKeyReturnsFalse()
    {
        var dictionary = new WeakValueDictionary<int, string>();
        string value = "hello";
        dictionary.TryAdd(1, value);

        dictionary.Remove(99, "hello", StringComparer.Ordinal).ShouldBeFalse();
        dictionary.ContainsKey(1).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveKeyValueWithNullComparerUsesDefaultComparer()
    {
        // With no comparer specified, Remove(key, value) compares the supplied value against the stored value using EqualityComparer<TValue>.Default.
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        object unrelated = new();
        dictionary.TryAdd(1, value);

        // A non-matching value should not remove the entry.
        dictionary.Remove(1, unrelated).ShouldBeFalse();
        dictionary.ContainsKey(1).ShouldBeTrue();

        // The matching value should remove it.
        dictionary.Remove(1, value).ShouldBeTrue();
        dictionary.ContainsKey(1).ShouldBeFalse();

        GC.KeepAlive(value);
        GC.KeepAlive(unrelated);
    }

    [TestMethod]
    public void RemoveNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        Should.Throw<ArgumentNullException>(() => dictionary.Remove(null!));
    }

    [TestMethod]
    public void RemoveOutValueNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        Should.Throw<ArgumentNullException>(() => dictionary.Remove(null!, out _));
    }

    [TestMethod]
    public void RemoveKeyValueNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();

        Should.Throw<ArgumentNullException>(() => dictionary.Remove(null!, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveKeyValueNullValueThrows()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();
        dictionary.TryAdd(1, value);

        Should.Throw<ArgumentNullException>(() => dictionary.Remove(1, null!));

        GC.KeepAlive(value);
    }
}
