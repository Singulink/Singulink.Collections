namespace Singulink.Collections.Tests.Map;

[PrefixTestClass]
public class OperationTests
{
    private static Map<int, string> Create() => new() {
        { 1, "one" },
        { 2, "two" },
    };

    private static void ShouldBeConsistent<TLeft, TRight>(Map<TLeft, TRight> map)
        where TLeft : notnull
        where TRight : notnull
    {
        map.Count.ShouldBe(map.Reverse.Count);
        map.LeftValues.Count.ShouldBe(map.Count);
        map.RightValues.Count.ShouldBe(map.Count);

        foreach (var pair in map)
        {
            map.Reverse[pair.Value].ShouldBe(pair.Key);
            map.Contains(pair.Key, pair.Value).ShouldBeTrue();
        }
    }

    [TestMethod]
    public void AddDuplicateLeftThrowsAndLeavesMapUnchanged()
    {
        var map = Create();

        var ex = Should.Throw<ArgumentException>(() => map.Add(1, "uno"));
        ex.ParamName.ShouldBe("leftValue");

        map.Count.ShouldBe(2);
        map[1].ShouldBe("one");
        map.ContainsRight("uno").ShouldBeFalse();
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void AddDuplicateRightThrowsAndRollsBackLeft()
    {
        var map = Create();

        var ex = Should.Throw<ArgumentException>(() => map.Add(3, "one"));
        ex.ParamName.ShouldBe("rightValue");

        map.Count.ShouldBe(2);
        map.ContainsLeft(3).ShouldBeFalse();
        map.Reverse["one"].ShouldBe(1);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void TryAddDuplicatesReturnFalseAndLeaveMapUnchanged()
    {
        var map = Create();

        map.TryAdd(1, "uno").ShouldBeFalse();
        map.TryAdd(3, "one").ShouldBeFalse();
        map.TryAdd(1, "one").ShouldBeFalse();
        map.TryAdd(3, "three").ShouldBeTrue();

        map.Count.ShouldBe(3);
        map[1].ShouldBe("one");
        map[3].ShouldBe("three");
        map.ContainsRight("uno").ShouldBeFalse();
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void IndexerSetAddsNewPair()
    {
        var map = Create();
        map[3] = "three";

        map.Count.ShouldBe(3);
        map.Reverse["three"].ShouldBe(3);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void IndexerSetReplacesRightValueOfExistingLeft()
    {
        var map = Create();
        map[1] = "uno";

        map.Count.ShouldBe(2);
        map[1].ShouldBe("uno");
        map.ContainsRight("one").ShouldBeFalse();
        map.Reverse["uno"].ShouldBe(1);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void IndexerSetSamePairIsNoOp()
    {
        var map = Create();
        map[1] = "one";

        map.Count.ShouldBe(2);
        map[1].ShouldBe("one");
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void IndexerSetRightValueMappedToOtherLeftThrows()
    {
        var map = Create();

        Should.Throw<ArgumentException>(() => map[1] = "two");
        Should.Throw<ArgumentException>(() => map[3] = "two");

        map.Count.ShouldBe(2);
        map[1].ShouldBe("one");
        map[2].ShouldBe("two");
        map.ContainsLeft(3).ShouldBeFalse();
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void IndexerGetMissingThrows()
    {
        var map = Create();
        Should.Throw<KeyNotFoundException>(() => map[3]);
        Should.Throw<KeyNotFoundException>(() => map.Reverse["three"]);
    }

    [TestMethod]
    public void SetReplacesConflictsOnBothSides()
    {
        var map = Create();
        map.Set(1, "two");

        map.Count.ShouldBe(1);
        map[1].ShouldBe("two");
        map.ContainsLeft(2).ShouldBeFalse();
        map.ContainsRight("one").ShouldBeFalse();
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void SetReplacesRightOnly()
    {
        var map = Create();
        map.Set(1, "uno");

        map.Count.ShouldBe(2);
        map[1].ShouldBe("uno");
        map.ContainsRight("one").ShouldBeFalse();
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void SetReplacesLeftOnly()
    {
        var map = Create();
        map.Set(3, "one");

        map.Count.ShouldBe(2);
        map[3].ShouldBe("one");
        map.ContainsLeft(1).ShouldBeFalse();
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void SetSamePairAndNewPair()
    {
        var map = Create();

        map.Set(1, "one");
        map.Count.ShouldBe(2);

        map.Set(3, "three");
        map.Count.ShouldBe(3);
        map[3].ShouldBe("three");
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void RemovePair()
    {
        var map = Create();

        map.Remove(1, "two").ShouldBeFalse();
        map.Remove(3, "one").ShouldBeFalse();
        map.Count.ShouldBe(2);

        map.Remove(1, "one").ShouldBeTrue();
        map.Count.ShouldBe(1);
        map.ContainsLeft(1).ShouldBeFalse();
        map.ContainsRight("one").ShouldBeFalse();
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void RemoveLeftAndRight()
    {
        var map = Create();

        map.RemoveLeft(3).ShouldBeFalse();
        map.RemoveLeft(3, out string? right).ShouldBeFalse();
        right.ShouldBeNull();

        map.RemoveLeft(1, out right).ShouldBeTrue();
        right.ShouldBe("one");
        map.ContainsRight("one").ShouldBeFalse();

        map.RemoveRight("three").ShouldBeFalse();
        map.RemoveRight("three", out int left).ShouldBeFalse();
        left.ShouldBe(0);

        map.RemoveRight("two", out left).ShouldBeTrue();
        left.ShouldBe(2);
        map.ContainsLeft(2).ShouldBeFalse();

        map.Count.ShouldBe(0);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void GetAndTryGetValues()
    {
        var map = Create();

        map.GetRightValue(1).ShouldBe("one");
        map.GetLeftValue("two").ShouldBe(2);
        Should.Throw<KeyNotFoundException>(() => map.GetRightValue(3));
        Should.Throw<KeyNotFoundException>(() => map.GetLeftValue("three"));

        map.TryGetRightValue(1, out string? right).ShouldBeTrue();
        right.ShouldBe("one");
        map.TryGetRightValue(3, out right).ShouldBeFalse();
        right.ShouldBeNull();

        map.TryGetLeftValue("two", out int left).ShouldBeTrue();
        left.ShouldBe(2);
        map.TryGetLeftValue("three", out left).ShouldBeFalse();
        left.ShouldBe(0);
    }

    [TestMethod]
    public void ContainsMembers()
    {
        var map = Create();

        map.Contains(1, "one").ShouldBeTrue();
        map.Contains(1, "two").ShouldBeFalse();
        map.Contains(3, "one").ShouldBeFalse();
        map.ContainsLeft(2).ShouldBeTrue();
        map.ContainsLeft(3).ShouldBeFalse();
        map.ContainsRight("two").ShouldBeTrue();
        map.ContainsRight("three").ShouldBeFalse();
    }

    [TestMethod]
    public void ReverseRoundTripsAndIsLive()
    {
        var map = Create();
        var reverse = map.Reverse;

        reverse.ShouldBeSameAs(map.Reverse);
        reverse.Reverse.ShouldBeSameAs(map);

        reverse.Add("three", 3);
        map[3].ShouldBe("three");

        reverse["four"] = 4;
        map[4].ShouldBe("four");

        reverse.RemoveLeft("one").ShouldBeTrue();
        map.ContainsLeft(1).ShouldBeFalse();

        reverse.LeftValues.ShouldBe(map.RightValues, ignoreOrder: true);
        reverse.RightValues.ShouldBe(map.LeftValues, ignoreOrder: true);
        reverse.LeftComparer.ShouldBeSameAs(map.RightComparer);
        reverse.RightComparer.ShouldBeSameAs(map.LeftComparer);
        ShouldBeConsistent(map);
        ShouldBeConsistent(reverse);
    }

    [TestMethod]
    public void ComparersAreUsedPerSide()
    {
        var map = new Map<string, string>(StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);

        map.LeftComparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
        map.RightComparer.ShouldBeSameAs(StringComparer.Ordinal);

        map.Add("A", "x");

        map.ContainsLeft("a").ShouldBeTrue();
        map.ContainsRight("X").ShouldBeFalse();
        map.Contains("a", "x").ShouldBeTrue();
        map.Contains("a", "X").ShouldBeFalse();
        map.TryAdd("a", "y").ShouldBeFalse();
        map.TryAdd("b", "X").ShouldBeTrue();
        map.Count.ShouldBe(2);
        ShouldBeConsistent(map);
    }

    [TestMethod]
    public void DefaultComparers()
    {
        var map = new Map<string, string>();
        map.LeftComparer.ShouldBe(EqualityComparer<string>.Default);
        map.RightComparer.ShouldBe(EqualityComparer<string>.Default);
    }

    [TestMethod]
    public void EnumerationYieldsPairs()
    {
        var map = Create();
        map.ShouldBe([new(1, "one"), new(2, "two")], ignoreOrder: true);

        var pairs = new List<KeyValuePair<int, string>>();

        foreach (var pair in map)
            pairs.Add(pair);

        pairs.ShouldBe([new(1, "one"), new(2, "two")], ignoreOrder: true);
    }

    [TestMethod]
    public void DictionaryInterface()
    {
        var map = Create();
        IDictionary<int, string> dictionary = map;

        dictionary.IsReadOnly.ShouldBeFalse();
        dictionary.Keys.ShouldBe([1, 2], ignoreOrder: true);
        dictionary.Values.ShouldBe(["one", "two"], ignoreOrder: true);
        dictionary.ContainsKey(1).ShouldBeTrue();
        dictionary.ContainsKey(3).ShouldBeFalse();
        dictionary.TryGetValue(2, out string? value).ShouldBeTrue();
        value.ShouldBe("two");
        dictionary.TryGetValue(3, out value).ShouldBeFalse();

        dictionary.Add(new KeyValuePair<int, string>(3, "three"));
        dictionary.Contains(new KeyValuePair<int, string>(3, "three")).ShouldBeTrue();
        dictionary.Contains(new KeyValuePair<int, string>(3, "tres")).ShouldBeFalse();
        dictionary.Remove(new KeyValuePair<int, string>(3, "tres")).ShouldBeFalse();
        dictionary.Remove(new KeyValuePair<int, string>(3, "three")).ShouldBeTrue();
        dictionary.Remove(1).ShouldBeTrue();
        dictionary.Remove(1).ShouldBeFalse();

        var array = new KeyValuePair<int, string>[2];
        dictionary.CopyTo(array, 1);
        array[1].ShouldBe(new KeyValuePair<int, string>(2, "two"));

        map.Count.ShouldBe(1);
        ShouldBeConsistent(map);

        IReadOnlyDictionary<int, string> readOnly = map;
        readOnly.Keys.ShouldBe([2]);
        readOnly.Values.ShouldBe(["two"]);
        readOnly.ContainsKey(2).ShouldBeTrue();
        readOnly.TryGetValue(2, out value).ShouldBeTrue();
        value.ShouldBe("two");
    }

    [TestMethod]
    public void MapInterface()
    {
        var map = Create();
        IMap<int, string> imap = map;

        imap.Count.ShouldBe(2);
        imap.Reverse.ShouldBeSameAs(map.Reverse);
        imap.LeftValues.ShouldBe([1, 2], ignoreOrder: true);
        imap.RightValues.ShouldBe(["one", "two"], ignoreOrder: true);
        imap[1] = "uno";
        map[1].ShouldBe("uno");

        IReadOnlyMap<int, string> readOnlyMap = map;
        readOnlyMap.Reverse.ShouldBeSameAs(map.Reverse);
        readOnlyMap[2].ShouldBe("two");
    }

#if !NETFRAMEWORK

    [TestMethod]
    public void CapacityMembersPreserveContents()
    {
        var map = Create();

        map.EnsureCapacity(100).ShouldBeGreaterThanOrEqualTo(100);
        map.TrimExcess();
        map.TrimExcess(10);

        map.Count.ShouldBe(2);
        map[1].ShouldBe("one");
        ShouldBeConsistent(map);
    }

#endif

#if NET9_0_OR_GREATER

    [TestMethod]
    public void Capacity()
    {
        var map = new Map<int, string>(10);
        map.Capacity.ShouldBeGreaterThanOrEqualTo(10);
    }

#endif
}
