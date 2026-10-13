#if NET9_0_OR_GREATER

namespace Singulink.Collections.Tests.Map;

[PrefixTestClass]
public class AlternateLookupTests
{
    private static Map<string, string> Create() => new(StringComparer.Ordinal, StringComparer.Ordinal) {
        { "one", "ONE" },
        { "two", "TWO" },
    };

    private static void ShouldBeConsistent(Map<string, string> map)
    {
        map.Count.ShouldBe(map.Reverse.Count);

        foreach (var (left, right) in map)
            map.Reverse[right].ShouldBe(left);
    }

    [TestMethod]
    public void RemoveRightByAlternateKeyRemovesBothSides()
    {
        var map = Create();
        var lookup = map.GetAlternateLookup<ReadOnlySpan<char>, ReadOnlySpan<char>>();

        lookup.RemoveRight("ONE".AsSpan()).ShouldBeTrue();

        map.ContainsLeft("one").ShouldBeFalse();
        map.ContainsRight("ONE").ShouldBeFalse();
        map.Count.ShouldBe(1);
        ShouldBeConsistent(map);

        lookup.RemoveRight("ONE".AsSpan()).ShouldBeFalse();
        lookup.RemoveRight("missing".AsSpan()).ShouldBeFalse();
    }

    [TestMethod]
    public void RemoveLeftByAlternateKeyRemovesBothSides()
    {
        var map = Create();
        var lookup = map.GetAlternateLookup<ReadOnlySpan<char>, ReadOnlySpan<char>>();

        lookup.RemoveLeft("one".AsSpan()).ShouldBeTrue();

        map.ContainsLeft("one").ShouldBeFalse();
        map.ContainsRight("ONE").ShouldBeFalse();
        map.Count.ShouldBe(1);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void TryAddWithAlternateLeftAndRealRight()
    {
        var map = Create();
        var lookup = map.GetAlternateLookup<ReadOnlySpan<char>, ReadOnlySpan<char>>();

        lookup.TryAdd("three".AsSpan(), "THREE").ShouldBeTrue();
        map["three"].ShouldBe("THREE");
        map.Reverse["THREE"].ShouldBe("three");
        ShouldBeConsistent(map);

        // Existing left value
        lookup.TryAdd("three".AsSpan(), "OTHER").ShouldBeFalse();

        // Existing right value
        lookup.TryAdd("other".AsSpan(), "THREE").ShouldBeFalse();

        map.Count.ShouldBe(3);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void TryAddWithRealLeftAndAlternateRight()
    {
        var map = Create();
        var lookup = map.GetAlternateLookup<ReadOnlySpan<char>, ReadOnlySpan<char>>();

        lookup.TryAdd("three", "THREE".AsSpan()).ShouldBeTrue();
        map["three"].ShouldBe("THREE");

        lookup.TryAdd("three", "OTHER".AsSpan()).ShouldBeFalse();
        lookup.TryAdd("other", "THREE".AsSpan()).ShouldBeFalse();

        map.Count.ShouldBe(3);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void TryAddWithBothAlternate()
    {
        var map = Create();
        var lookup = map.GetAlternateLookup<ReadOnlySpan<char>, ReadOnlySpan<char>>();

        lookup.TryAdd("three".AsSpan(), "THREE".AsSpan(), out string? left, out string? right).ShouldBeTrue();
        left.ShouldBe("three");
        right.ShouldBe("THREE");

        lookup.TryAdd("three".AsSpan(), "OTHER".AsSpan()).ShouldBeFalse();
        lookup.TryAdd("other".AsSpan(), "THREE".AsSpan()).ShouldBeFalse();

        map.Count.ShouldBe(3);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void LeftLookupTryAdd()
    {
        var map = Create();
        var lookup = map.GetAlternateLeftLookup<ReadOnlySpan<char>>();

        lookup.TryAdd("three".AsSpan(), "THREE", out string? left).ShouldBeTrue();
        left.ShouldBe("three");
        map["three"].ShouldBe("THREE");

        lookup.TryAdd("three".AsSpan(), "OTHER").ShouldBeFalse();
        lookup.TryAdd("other".AsSpan(), "THREE").ShouldBeFalse();

        map.Count.ShouldBe(3);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void LeftLookupRemoveLeft()
    {
        var map = Create();
        var lookup = map.GetAlternateLeftLookup<ReadOnlySpan<char>>();

        lookup.RemoveLeft("two".AsSpan()).ShouldBeTrue();
        map.ContainsRight("TWO").ShouldBeFalse();
        map.Count.ShouldBe(1);
        ShouldBeConsistent(map);
    }
}

#endif
