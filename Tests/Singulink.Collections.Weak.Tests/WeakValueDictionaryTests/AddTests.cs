namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class AddTests
{
    [TestMethod]
    public void TryAddToEmptyDictionary()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();

        dictionary.TryAdd(1, value).ShouldBeTrue();

        dictionary.UnsafeCount.ShouldBe(1);
        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryAddMultipleKeys()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();

        dictionary.TryAdd(1, value1).ShouldBeTrue();
        dictionary.TryAdd(2, value2).ShouldBeTrue();
        dictionary.TryAdd(3, value3).ShouldBeTrue();

        dictionary.UnsafeCount.ShouldBe(3);
        dictionary.ToList().ShouldBe(
            [new(1, value1), new(2, value2), new(3, value3)],
            ignoreOrder: true);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void TryAddDuplicateKeyWithLiveValueReturnsFalse()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value1 = new();
        object value2 = new();
        dictionary.TryAdd(1, value1);

        dictionary.TryAdd(1, value2).ShouldBeFalse();

        // The original value should still be associated with the key.
        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void TryAddSucceedsForKeyWhoseValueWasCollected()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object newValue = new();

        var deadValueRef = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object deadValue = new();
            dictionary.TryAdd(1, deadValue);
            return new WeakReference<object>(deadValue);
        });

        Helpers.ForceGC();

        // Precondition: the previous value must actually have been collected for this test to be meaningful.
        deadValueRef.TryGetTarget(out _).ShouldBeFalse();

        // Once the previous value has been collected, the key is free again and can be re-added.
        dictionary.TryAdd(1, newValue).ShouldBeTrue();
        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(newValue);

        GC.KeepAlive(newValue);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void AddToEmptyDictionary()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value = new();

        dictionary.Add(1, value);

        dictionary.UnsafeCount.ShouldBe(1);
        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddMultipleKeys()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value1 = new();
        object value2 = new();

        dictionary.Add(1, value1);
        dictionary.Add(2, value2);

        dictionary.UnsafeCount.ShouldBe(2);
        dictionary.ToList().ShouldBe([new(1, value1), new(2, value2)], ignoreOrder: true);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddDuplicateKeyWithLiveValueThrows()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object value1 = new();
        object value2 = new();
        dictionary.Add(1, value1);

        Should.Throw<ArgumentException>(() => dictionary.Add(1, value2));

        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddSucceedsForKeyWhoseValueWasCollected()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object newValue = new();

        var deadValueRef = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object deadValue = new();
            dictionary.Add(1, deadValue);
            return new WeakReference<object>(deadValue);
        });

        Helpers.ForceGC();

        // Precondition: the previous value must actually have been collected for this test to be meaningful.
        deadValueRef.TryGetTarget(out _).ShouldBeFalse();

        Should.NotThrow(() => dictionary.Add(1, newValue));
        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(newValue);

        GC.KeepAlive(newValue);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void TryAddNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();

        Should.Throw<ArgumentNullException>(() => dictionary.TryAdd(null!, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryAddNullValueThrows()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        Should.Throw<ArgumentNullException>(() => dictionary.TryAdd(1, null!));
    }

    [TestMethod]
    public void AddNullKeyThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();

        Should.Throw<ArgumentNullException>(() => dictionary.Add(null!, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddNullValueThrows()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        Should.Throw<ArgumentNullException>(() => dictionary.Add(1, null!));
    }
}
