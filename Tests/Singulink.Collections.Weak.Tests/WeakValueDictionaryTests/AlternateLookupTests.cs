#if NET9_0_OR_GREATER
namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class AlternateLookupTests
{
    [TestMethod]
    public void GetAlternateLookupReturnsUsableLookup()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();
        dictionary.TryAdd("key", value);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.TryGetValue("key".AsSpan(), out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryGetAlternateLookupSucceedsWithDefaultComparer()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        dictionary.TryGetAlternateLookup<ReadOnlySpan<char>>(out _).ShouldBeTrue();
    }

    [TestMethod]
    public void TryGetAlternateLookupSucceedsWithSupportingComparer()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);

        dictionary.TryGetAlternateLookup<ReadOnlySpan<char>>(out _).ShouldBeTrue();
    }

    [TestMethod]
    public void TryGetAlternateLookupFailsWithUnsupportingComparer()
    {
        var dictionary = new WeakValueDictionary<string, object>(EqualityComparer<string>.Create(StringComparer.Ordinal.Equals, StringComparer.Ordinal.GetHashCode));

        dictionary.TryGetAlternateLookup<ReadOnlySpan<char>>(out _).ShouldBeFalse();
    }

    [TestMethod]
    public void GetAlternateLookupSucceedsWithDefaultComparer()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        Should.NotThrow(() => dictionary.GetAlternateLookup<ReadOnlySpan<char>>());
    }

    [TestMethod]
    public void GetAlternateLookupSucceedsWithSupportingComparer()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);

        Should.NotThrow(() => dictionary.GetAlternateLookup<ReadOnlySpan<char>>());
    }

    [TestMethod]
    public void GetAlternateLookupThrowsWithUnsupportingComparer()
    {
        var dictionary = new WeakValueDictionary<string, object>(EqualityComparer<string>.Create(StringComparer.Ordinal.Equals, StringComparer.Ordinal.GetHashCode));

        Should.Throw<InvalidOperationException>(() => dictionary.GetAlternateLookup<ReadOnlySpan<char>>());
    }

    [TestMethod]
    public void IndexerGetReturnsValue()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();
        dictionary.TryAdd("key", value);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup["key".AsSpan()].ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IndexerGetMissingKeyThrowsKeyNotFound()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        Should.Throw<KeyNotFoundException>(() =>
        {
            var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
            _ = lookup["missing".AsSpan()];
        });
    }

    [TestMethod]
    public void ComparerPropertyReturnsAlternateComparer()
    {
        var dictionary = new WeakValueDictionary<string, object>(StringComparer.Ordinal);
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.Comparer.ShouldBeSameAs(StringComparer.Ordinal);
    }

    [TestMethod]
    public void DictionaryPropertyReturnsUnderlyingDictionary()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.Dictionary.ShouldBeSameAs(dictionary);
    }

    [TestMethod]
    public void ContainsKeyReturnsTrueWhenPresent()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();
        dictionary.TryAdd("key", value);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.ContainsKey("key".AsSpan()).ShouldBeTrue();
        lookup.ContainsKey("missing".AsSpan()).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ContainsKeyOutActualKeyReturnsActualKey()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();
        dictionary.TryAdd("key", value);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.ContainsKey("key".AsSpan(), out string? actualKey).ShouldBeTrue();
        actualKey.ShouldBe("key");

        lookup.ContainsKey("missing".AsSpan(), out string? missingActualKey).ShouldBeFalse();
        missingActualKey.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryGetValueReturnsValue()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();
        dictionary.TryAdd("key", value);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.TryGetValue("key".AsSpan(), out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        lookup.TryGetValue("missing".AsSpan(), out object? missing).ShouldBeFalse();
        missing.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryGetValueOutActualKeyReturnsActualKeyAndValue()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();
        dictionary.TryAdd("key", value);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.TryGetValue("key".AsSpan(), out string? actualKey, out object? current).ShouldBeTrue();
        actualKey.ShouldBe("key");
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveRemovesEntry()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();
        dictionary.TryAdd("key", value);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.Remove("key".AsSpan()).ShouldBeTrue();
        dictionary.ContainsKey("key").ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveMissingKeyReturnsFalse()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.Remove("missing".AsSpan()).ShouldBeFalse();
    }

    [TestMethod]
    public void RemoveOutActualKeyAndValueReturnsThem()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();
        dictionary.TryAdd("key", value);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.Remove("key".AsSpan(), out string? actualKey, out object? removed).ShouldBeTrue();
        actualKey.ShouldBe("key");
        removed.ShouldBeSameAs(value);
        dictionary.ContainsKey("key").ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IndexerSetAddsNewEntry()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        lookup["key".AsSpan()] = value;

        dictionary.UnsafeCount.ShouldBe(1);
        dictionary.TryGetValue("key", out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IndexerSetOverwritesExistingValue()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value1 = new();
        object value2 = new();
        dictionary.TryAdd("key", value1);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        lookup["key".AsSpan()] = value2;

        dictionary.UnsafeCount.ShouldBe(1);
        dictionary.TryGetValue("key", out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value2);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void IndexerSetReplacesEntryWhoseValueWasCollected()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object newValue = new();

        var deadValueRef = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object deadValue = new();
            var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
            lookup["key".AsSpan()] = deadValue;
            return new WeakReference<object>(deadValue);
        });

        Helpers.ForceGC();

        // Precondition: the previous value must actually have been collected for this test to be meaningful.
        deadValueRef.TryGetTarget(out _).ShouldBeFalse();

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        lookup["key".AsSpan()] = newValue;

        lookup.TryGetValue("key".AsSpan(), out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(newValue);

        GC.KeepAlive(newValue);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void IndexerSetNullValueThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        Should.Throw<ArgumentNullException>(() =>
        {
            var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
            lookup["key".AsSpan()] = null!;
        });
    }

    [TestMethod]
    public void TryAddAddsNewEntry()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value = new();

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        lookup.TryAdd("key".AsSpan(), value).ShouldBeTrue();

        dictionary.UnsafeCount.ShouldBe(1);
        dictionary.TryGetValue("key", out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryAddDuplicateKeyWithLiveValueReturnsFalse()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object value1 = new();
        object value2 = new();
        dictionary.TryAdd("key", value1);

        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        lookup.TryAdd("key".AsSpan(), value2).ShouldBeFalse();

        // The original value should still be associated with the key.
        dictionary.TryGetValue("key", out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void TryAddSucceedsForKeyWhoseValueWasCollected()
    {
        var dictionary = new WeakValueDictionary<string, object>();
        object newValue = new();

        var deadValueRef = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object deadValue = new();
            var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
            lookup.TryAdd("key".AsSpan(), deadValue);
            return new WeakReference<object>(deadValue);
        });

        Helpers.ForceGC();

        // Precondition: the previous value must actually have been collected for this test to be meaningful.
        deadValueRef.TryGetTarget(out _).ShouldBeFalse();

        // Once the previous value has been collected, the key is free again and can be re-added through the alternate lookup.
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        lookup.TryAdd("key".AsSpan(), newValue).ShouldBeTrue();
        lookup.TryGetValue("key".AsSpan(), out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(newValue);

        GC.KeepAlive(newValue);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void TryAddNullValueThrows()
    {
        var dictionary = new WeakValueDictionary<string, object>();

        Should.Throw<ArgumentNullException>(() =>
        {
            var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
            lookup.TryAdd("key".AsSpan(), null!);
        });
    }
}
#endif
