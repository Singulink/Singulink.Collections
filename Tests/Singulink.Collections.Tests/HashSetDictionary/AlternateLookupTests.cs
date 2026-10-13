#if NET9_0_OR_GREATER

namespace Singulink.Collections.Tests.HashSetDictionary;

[PrefixTestClass]
public class AlternateLookupTests
{
    private static HashSetDictionary<string, int> Create()
    {
        var dictionary = new HashSetDictionary<string, int>(StringComparer.Ordinal);
        dictionary["a"].AddRange([1, 2, 3]);
        dictionary["b"].Add(10);
        return dictionary;
    }

    [TestMethod]
    public void IndexerReturnsAttachedSet()
    {
        var dictionary = Create();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup["a".AsSpan()].ShouldBeSameAs(dictionary["a"]);
        lookup["a".AsSpan()].ShouldBe([1, 2, 3], ignoreOrder: true);
    }

    [TestMethod]
    public void IndexerCreatesDetachedSetWithCreatedKey()
    {
        var dictionary = Create();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        var set = lookup["c".AsSpan()];
        set.Key.ShouldBe("c");
        set.Dictionary.ShouldBeSameAs(dictionary);
        dictionary.ContainsKey("c").ShouldBeFalse();

        set.Add(7).ShouldBeTrue();

        dictionary.ContainsKey("c").ShouldBeTrue();
        dictionary["c"].ShouldBe([7]);
        dictionary.ValueCount.ShouldBe(5);
    }

    [TestMethod]
    public void Clear()
    {
        var dictionary = Create();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        var set = dictionary["a"];

        lookup.Clear("a".AsSpan()).ShouldBeTrue();

        set.Count.ShouldBe(0);
        dictionary.ContainsKey("a").ShouldBeFalse();
        dictionary.Count.ShouldBe(1);
        dictionary.ValueCount.ShouldBe(1);

        lookup.Clear("a".AsSpan()).ShouldBeFalse();
        lookup.Clear("missing".AsSpan(), out string? actualKey).ShouldBeFalse();
        actualKey.ShouldBeNull();

        lookup.Clear("b".AsSpan(), out actualKey).ShouldBeTrue();
        actualKey.ShouldBe("b");
        dictionary.Count.ShouldBe(0);
        dictionary.ValueCount.ShouldBe(0);
    }

    [TestMethod]
    public void ClearInvalidatesEnumerators()
    {
        var dictionary = Create();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        lookup.Clear("b".AsSpan());

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void Contains()
    {
        var dictionary = Create();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.Contains("a".AsSpan(), 2).ShouldBeTrue();
        lookup.Contains("a".AsSpan(), 10).ShouldBeFalse();
        lookup.Contains("c".AsSpan(), 1).ShouldBeFalse();

        lookup.Contains("a".AsSpan(), 2, out string? actualKey).ShouldBeTrue();
        actualKey.ShouldBe("a");

        lookup.Contains("c".AsSpan(), 1, out actualKey).ShouldBeFalse();
        actualKey.ShouldBeNull();
    }

    [TestMethod]
    public void ContainsKey()
    {
        var dictionary = Create();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.ContainsKey("a".AsSpan()).ShouldBeTrue();
        lookup.ContainsKey("c".AsSpan()).ShouldBeFalse();

        lookup.ContainsKey("a".AsSpan(), out string? actualKey).ShouldBeTrue();
        actualKey.ShouldBe("a");

        lookup.ContainsKey("c".AsSpan(), out actualKey).ShouldBeFalse();
        actualKey.ShouldBeNull();
    }

    [TestMethod]
    public void GetValueCountAndTryGetValues()
    {
        var dictionary = Create();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.GetValueCount("a".AsSpan()).ShouldBe(3);
        lookup.GetValueCount("c".AsSpan()).ShouldBe(0);

        lookup.TryGetValues("a".AsSpan(), out var set).ShouldBeTrue();
        set.ShouldBeSameAs(dictionary["a"]);

        lookup.TryGetValues("c".AsSpan(), out set).ShouldBeFalse();
        set.ShouldBeNull();

        lookup.TryGetValues("b".AsSpan(), out string? actualKey, out set).ShouldBeTrue();
        actualKey.ShouldBe("b");
        set.ShouldBe([10]);

        lookup.TryGetValues("c".AsSpan(), out actualKey, out set).ShouldBeFalse();
        actualKey.ShouldBeNull();
        set.ShouldBeNull();
    }

    [TestMethod]
    public void DictionaryAndComparerProperties()
    {
        var dictionary = Create();
        var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        lookup.Dictionary.ShouldBeSameAs(dictionary);
        lookup.Comparer.ShouldNotBeNull();
        lookup.Comparer.Equals("a".AsSpan(), "a").ShouldBeTrue();
        lookup.Comparer.Create("x".AsSpan()).ShouldBe("x");
    }

    [TestMethod]
    public void TryGetAlternateLookup()
    {
        Create().TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup).ShouldBeTrue();
        lookup.ContainsKey("a".AsSpan()).ShouldBeTrue();

        var unsupported = new HashSetDictionary<string, int>(new PlainComparer());
        unsupported.TryGetAlternateLookup<ReadOnlySpan<char>>(out _).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => unsupported.GetAlternateLookup<ReadOnlySpan<char>>());
    }

    private sealed class PlainComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);

        public int GetHashCode(string obj) => obj.GetHashCode();
    }
}

#endif
