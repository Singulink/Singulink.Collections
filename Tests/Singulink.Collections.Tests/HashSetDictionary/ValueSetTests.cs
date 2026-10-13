namespace Singulink.Collections.Tests.HashSetDictionary;

[PrefixTestClass]
public class ValueSetTests
{
    private static HashSetDictionary<string, int> Create()
    {
        var dictionary = new HashSetDictionary<string, int>();
        dictionary["a"].AddRange([1, 2, 3]);
        dictionary["b"].Add(10);
        return dictionary;
    }

    private static void ShouldBeConsistent(HashSetDictionary<string, int> dictionary)
    {
        dictionary.ValueCount.ShouldBe(dictionary.ValueSets.Sum(s => s.Count));
        dictionary.Count.ShouldBe(dictionary.ValueSets.Count());
        dictionary.ValueSets.ShouldAllBe(s => s.Count > 0);
        dictionary.Keys.Count.ShouldBe(dictionary.Count);
        dictionary.Values.Count.ShouldBe(dictionary.ValueCount);
    }

    [TestMethod]
    public void AddReturnsFalseForDuplicate()
    {
        var dictionary = Create();

        dictionary["a"].Add(2).ShouldBeFalse();
        dictionary["a"].Add(4).ShouldBeTrue();

        dictionary["a"].ShouldBe([1, 2, 3, 4], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(5);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void AddAttachesNewKey()
    {
        var dictionary = Create();
        var set = dictionary["c"];

        set.Add(7).ShouldBeTrue();

        dictionary.ContainsKey("c").ShouldBeTrue();
        dictionary.ValueSets.Contains(set).ShouldBeTrue();
        dictionary.Count.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void AddRangeCountsOnlyNewItems()
    {
        var dictionary = Create();

        dictionary["a"].AddRange([2, 3, 4, 4, 5]).ShouldBe(2);

        dictionary["a"].ShouldBe([1, 2, 3, 4, 5], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void AddRangePartialFailureKeepsAddedItems()
    {
        var dictionary = Create();

        Should.Throw<InvalidOperationException>(() => dictionary["c"].AddRange(new[] { 5, 6 }.ThenThrow()));

        dictionary.ContainsKey("c").ShouldBeTrue();
        dictionary["c"].ShouldBe([5, 6], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void Remove()
    {
        var dictionary = Create();

        dictionary["a"].Remove(2).ShouldBeTrue();
        dictionary["a"].Remove(2).ShouldBeFalse();
        dictionary["c"].Remove(1).ShouldBeFalse();

        dictionary["a"].ShouldBe([1, 3], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveLastValueDetachesKey()
    {
        var dictionary = Create();
        var set = dictionary["b"];

        set.Remove(10).ShouldBeTrue();

        dictionary.ContainsKey("b").ShouldBeFalse();
        dictionary.ValueSets.Contains(set).ShouldBeFalse();
        dictionary.Count.ShouldBe(1);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void ClearValueSetDetachesKey()
    {
        var dictionary = Create();
        var set = dictionary["a"];
        set.Clear();

        set.Count.ShouldBe(0);
        dictionary.ContainsKey("a").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(1);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void ClearOnDetachedSetIsNoOp()
    {
        var dictionary = Create();
        dictionary["c"].Clear();

        dictionary.ContainsKey("c").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(4);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void ExceptWithOnDetachedSetIsNoOp()
    {
        var dictionary = Create();
        dictionary["c"].ExceptWith([1, 2]);

        dictionary.ContainsKey("c").ShouldBeFalse();
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void IntersectWithOnDetachedSetIsNoOp()
    {
        var dictionary = Create();
        dictionary["c"].IntersectWith([1, 2]);

        dictionary.ContainsKey("c").ShouldBeFalse();
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void ExceptWithNoChangeKeepsEnumeratorValid()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary["a"].ExceptWith([99]);
        dictionary["a"].IntersectWith([1, 2, 3, 4]);
        dictionary["a"].UnionWith([1, 2]);

        enumerator.MoveNext().ShouldBeTrue();
    }

    [TestMethod]
    public void SetRangeReplacesValues()
    {
        var dictionary = Create();
        dictionary["a"].SetRange([7, 8, 8]);

        dictionary["a"].ShouldBe([7, 8], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetRangeAttachesNewKey()
    {
        var dictionary = Create();
        dictionary["c"].SetRange([7, 8]);

        dictionary.ContainsKey("c").ShouldBeTrue();
        dictionary["c"].ShouldBe([7, 8], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetRangeEmptyDetachesKey()
    {
        var dictionary = Create();
        dictionary["a"].SetRange([]);

        dictionary.ContainsKey("a").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(1);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetRangeEmptyOnDetachedSetIsNoOp()
    {
        var dictionary = Create();
        dictionary["c"].SetRange([]);

        dictionary.ContainsKey("c").ShouldBeFalse();
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetRangeFailureLeavesSetUntouched()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() => dictionary["a"].SetRange(new[] { 5 }.ThenThrow()));
        Should.Throw<InvalidOperationException>(() => dictionary["c"].SetRange(new[] { 5 }.ThenThrow()));

        dictionary["a"].ShouldBe([1, 2, 3], ignoreOrder: true);
        dictionary.ContainsKey("c").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(4);
        enumerator.MoveNext().ShouldBeTrue();
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetRangeFromProjectionOfSelf()
    {
        var dictionary = Create();
        var set = dictionary["a"];

        set.SetRange(set.Where(v => v > 1));

        dictionary["a"].ShouldBe([2, 3], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetComparisonMembers()
    {
        var dictionary = Create();
        var set = dictionary["a"];

        set.IsSubsetOf([1, 2, 3, 4]).ShouldBeTrue();
        set.IsSubsetOf([1, 2]).ShouldBeFalse();
        set.IsProperSubsetOf([1, 2, 3, 4]).ShouldBeTrue();
        set.IsProperSubsetOf([1, 2, 3]).ShouldBeFalse();
        set.IsSupersetOf([1, 2]).ShouldBeTrue();
        set.IsSupersetOf([1, 4]).ShouldBeFalse();
        set.IsProperSupersetOf([1, 2]).ShouldBeTrue();
        set.IsProperSupersetOf([1, 2, 3]).ShouldBeFalse();
        set.Overlaps([3, 4]).ShouldBeTrue();
        set.Overlaps([4, 5]).ShouldBeFalse();
        set.SetEquals([3, 2, 1]).ShouldBeTrue();
        set.SetEquals([1, 2]).ShouldBeFalse();
        set.Contains(1).ShouldBeTrue();
        set.Contains(4).ShouldBeFalse();

        var detached = dictionary["c"];
        detached.IsSubsetOf([1]).ShouldBeTrue();
        detached.IsSupersetOf([]).ShouldBeTrue();
        detached.SetEquals([]).ShouldBeTrue();
        detached.Overlaps([1]).ShouldBeFalse();
    }

    [TestMethod]
    public void CopyTo()
    {
        var dictionary = Create();
        var set = dictionary["a"];

        int[] array = new int[5];
        set.CopyTo(array);
        array.Take(3).ShouldBe([1, 2, 3], ignoreOrder: true);
        array.Skip(3).ShouldBe([0, 0]);

        array = new int[5];
        set.CopyTo(array, 2);
        array.Take(2).ShouldBe([0, 0]);
        array.Skip(2).ShouldBe([1, 2, 3], ignoreOrder: true);
    }

    [TestMethod]
    public void ValueComparerIsUsed()
    {
        var dictionary = new HashSetDictionary<int, string>(null, StringComparer.OrdinalIgnoreCase);

        dictionary.ValueComparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
        new HashSetDictionary<int, string>().ValueComparer.ShouldBe(EqualityComparer<string>.Default);

        dictionary[1].Add("one").ShouldBeTrue();
        dictionary[1].Add("ONE").ShouldBeFalse();
        dictionary[1].Contains("One").ShouldBeTrue();
        dictionary.Contains(1, "oNe").ShouldBeTrue();
        dictionary.ContainsValue("ONE").ShouldBeTrue();
        dictionary.Values.Contains("ONE").ShouldBeTrue();
        dictionary[1].Remove("ONE").ShouldBeTrue();
        dictionary.ContainsKey(1).ShouldBeFalse();
    }

    [TestMethod]
    public void DetachedInstanceSyncsToNewlyAttachedSet()
    {
        var dictionary = new HashSetDictionary<string, int>();
        var stale = dictionary["a"];

        dictionary["a"].Add(1);

        stale.Count.ShouldBe(1);
        stale.ShouldBe([1]);
        stale.Add(2).ShouldBeTrue();
        stale.Add(1).ShouldBeFalse();

        dictionary["a"].ShouldBe([1, 2], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(2);
        dictionary.ValueSets.Contains(stale).ShouldBeTrue();
    }

    [TestMethod]
    public void DetachedThenReattachedThroughOtherInstance()
    {
        var dictionary = Create();
        var original = dictionary["a"];

        dictionary.Clear("a");
        original.Count.ShouldBe(0);

        dictionary["a"].AddRange([5, 6]);

        original.ShouldBe([5, 6], ignoreOrder: true);
        original.Remove(5).ShouldBeTrue();
        dictionary["a"].ShouldBe([6]);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void ReattachingThroughStaleInstanceAfterDictionaryClear()
    {
        var dictionary = Create();
        var original = dictionary["a"];

        dictionary.Clear();
        original.Add(42).ShouldBeTrue();

        dictionary.ContainsKey("a").ShouldBeTrue();
        dictionary["a"].ShouldBe([42]);
        dictionary.Count.ShouldBe(1);
        dictionary.ValueCount.ShouldBe(1);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void EqualityIsByDictionaryAndKey()
    {
        var dictionary = Create();
        var other = Create();

        var a1 = dictionary["a"];
        var a2 = dictionary["a"];
        var c1 = dictionary["c"];
        var c2 = dictionary["c"];

        a1.Equals(a2).ShouldBeTrue();
        (a1 == a2).ShouldBeTrue();
        a1.GetHashCode().ShouldBe(a2.GetHashCode());

        c1.ShouldNotBeSameAs(c2);
        (c1 == c2).ShouldBeTrue();

        (a1 != dictionary["b"]).ShouldBeTrue();
        (a1 == other["a"]).ShouldBeFalse();
        a1.Equals(null).ShouldBeFalse();
        a1.Equals((object)a2).ShouldBeTrue();

        a1.AsReadOnly().Equals(a1).ShouldBeFalse();
        a1.AsReadOnly().Equals(a2.AsReadOnly()).ShouldBeTrue();
    }

    [TestMethod]
    public void EqualityUsesKeyComparer()
    {
        var dictionary = new HashSetDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        dictionary["a"].Add(1);

        (dictionary["A"] == dictionary["a"]).ShouldBeTrue();
        dictionary["A"].GetHashCode().ShouldBe(dictionary["a"].GetHashCode());
    }

    [TestMethod]
    public void AsReadOnlyIsLiveViewAndCached()
    {
        var dictionary = Create();
        var set = dictionary["a"];
        var readOnly = set.AsReadOnly();

        readOnly.ShouldBeSameAs(set.AsReadOnly());
        readOnly.Key.ShouldBe("a");
        readOnly.Dictionary.ShouldBeSameAs(dictionary);
        readOnly.ShouldBe([1, 2, 3], ignoreOrder: true);

        set.Add(4);
        readOnly.ShouldBe([1, 2, 3, 4], ignoreOrder: true);
        readOnly.Contains(4).ShouldBeTrue();

        set.Clear();
        readOnly.Count.ShouldBe(0);

        dictionary["a"].Add(9);
        readOnly.ShouldBe([9]);
    }

    [TestMethod]
    public void ReadOnlyInterfaceMutationsThrow()
    {
        var dictionary = Create();
        ISet<int> readOnly = dictionary["a"].AsReadOnly();

        readOnly.IsReadOnly.ShouldBeTrue();
        Should.Throw<NotSupportedException>(() => readOnly.Add(1));
        Should.Throw<NotSupportedException>(() => ((ICollection<int>)readOnly).Add(1));
        Should.Throw<NotSupportedException>(() => readOnly.Remove(1));
        Should.Throw<NotSupportedException>(() => readOnly.Clear());
        Should.Throw<NotSupportedException>(() => readOnly.UnionWith([1]));
        Should.Throw<NotSupportedException>(() => readOnly.IntersectWith([1]));
        Should.Throw<NotSupportedException>(() => readOnly.ExceptWith([1]));
        Should.Throw<NotSupportedException>(() => readOnly.SymmetricExceptWith([1]));

        readOnly.Contains(1).ShouldBeTrue();
        readOnly.SetEquals([1, 2, 3]).ShouldBeTrue();
    }

    [TestMethod]
    public void MutableInterfaceOperations()
    {
        var dictionary = Create();
        ISet<int> set = dictionary["a"];

        set.IsReadOnly.ShouldBeFalse();
        set.Add(4).ShouldBeTrue();
        set.Add(4).ShouldBeFalse();
        ((ICollection<int>)set).Add(5);
        set.Remove(1).ShouldBeTrue();

        dictionary["a"].ShouldBe([2, 3, 4, 5], ignoreOrder: true);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void AsTransientReadOnlyTracksCurrentSet()
    {
        var dictionary = Create();
        var set = dictionary["a"];
        var transient = set.AsTransientReadOnly();

        transient.ShouldBe([1, 2, 3], ignoreOrder: true);
        transient.ShouldBeSameAs(set.AsTransientReadOnly());

        dictionary.Clear("a");
        dictionary["a"].AddRange([7]);

        set.AsTransientReadOnly().ShouldBe([7]);
    }

    [TestMethod]
    public void EnumeratorThrowsWhenSetModified()
    {
        var dictionary = Create();
        var set = dictionary["a"];

        using var enumerator = set.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        set.Add(4);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void EnumeratorThrowsWhenSetDetachedAndReattachedElsewhere()
    {
        var dictionary = Create();
        var set = dictionary["a"];

        using var enumerator = set.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary.Clear("a");
        dictionary["a"].Add(5);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void EnumeratorOnDetachedSetThrowsAfterAttachElsewhere()
    {
        var dictionary = Create();
        var set = dictionary["c"];

        using var enumerator = set.GetEnumerator();
        dictionary["c"].Add(5);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void EnumeratorOnDetachedSetIsEmpty()
    {
        var dictionary = Create();
        dictionary["c"].ShouldBeEmpty();
        dictionary["c"].AsReadOnly().ShouldBeEmpty();
        dictionary["c"].AsTransientReadOnly().ShouldBeEmpty();
    }

    [TestMethod]
    public void TrimExcessPreservesContents()
    {
        var dictionary = Create();
        dictionary["a"].TrimExcess();
        dictionary["a"].ShouldBe([1, 2, 3], ignoreOrder: true);
        ShouldBeConsistent(dictionary);
    }

#if !NETFRAMEWORK

    [TestMethod]
    public void EnsureCapacity()
    {
        var dictionary = Create();
        dictionary["a"].EnsureCapacity(20).ShouldBeGreaterThanOrEqualTo(20);
        dictionary["a"].ShouldBe([1, 2, 3], ignoreOrder: true);
    }

    [TestMethod]
    public void EnsureCapacityInvalidatesEnumerators()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary["a"].EnsureCapacity(20);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

#endif
}
