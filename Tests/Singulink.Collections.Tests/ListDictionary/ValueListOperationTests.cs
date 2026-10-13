namespace Singulink.Collections.Tests.ListDictionary;

[PrefixTestClass]
public class ValueListOperationTests
{
    private static ListDictionary<string, int> Create()
    {
        var dictionary = new ListDictionary<string, int>();
        dictionary["a"].AddRange([1, 2, 3]);
        dictionary["b"].Add(10);
        return dictionary;
    }

    private static void ShouldBeConsistent(ListDictionary<string, int> dictionary)
    {
        dictionary.ValueCount.ShouldBe(dictionary.ValueLists.Sum(l => l.Count));
        dictionary.Count.ShouldBe(dictionary.ValueLists.Count());
        dictionary.ValueLists.ShouldAllBe(l => l.Count > 0);
        dictionary.Keys.Count.ShouldBe(dictionary.Count);
        dictionary.Values.Count.ShouldBe(dictionary.ValueCount);
    }

    [TestMethod]
    public void Insert()
    {
        var dictionary = Create();
        dictionary["a"].Insert(1, 99);

        dictionary["a"].ShouldBe([1, 99, 2, 3]);
        dictionary.ValueCount.ShouldBe(5);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void InsertAttachesNewKey()
    {
        var dictionary = Create();
        dictionary["c"].Insert(0, 7);

        dictionary.ContainsKey("c").ShouldBeTrue();
        dictionary["c"].ShouldBe([7]);
        dictionary.Count.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void InsertOutOfRangeDoesNotAttach()
    {
        var dictionary = Create();
        Should.Throw<ArgumentOutOfRangeException>(() => dictionary["c"].Insert(1, 7));

        dictionary.ContainsKey("c").ShouldBeFalse();
        dictionary.Count.ShouldBe(2);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void InsertRange()
    {
        var dictionary = Create();
        dictionary["a"].InsertRange(1, [7, 8]).ShouldBe(2);

        dictionary["a"].ShouldBe([1, 7, 8, 2, 3]);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void InsertRangeEmptyOnDetachedListDoesNotAttach()
    {
        var dictionary = Create();
        dictionary["c"].InsertRange(0, []).ShouldBe(0);

        dictionary.ContainsKey("c").ShouldBeFalse();
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void InsertRangePartialFailureKeepsInsertedItems()
    {
        var dictionary = Create();
        var list = dictionary["c"];

        Should.Throw<InvalidOperationException>(() => list.InsertRange(0, new[] { 5, 6 }.ThenThrow()));

        dictionary.ContainsKey("c").ShouldBeTrue();
        dictionary["c"].ShouldBe([5, 6]);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void AddRangePartialFailureKeepsAddedItems()
    {
        var dictionary = Create();

        Should.Throw<InvalidOperationException>(() => dictionary["a"].AddRange(new[] { 5, 6 }.ThenThrow()));

        dictionary["a"].ShouldBe([1, 2, 3, 5, 6]);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void IndexerSet()
    {
        var dictionary = Create();
        dictionary["a"][1] = 20;

        dictionary["a"].ShouldBe([1, 20, 3]);
        dictionary.ValueCount.ShouldBe(4);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void IndexerSetInvalidatesValueEnumeration()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary["a"][0] = 100;

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void IndexerGetSetOnDetachedListThrows()
    {
        var dictionary = Create();

        Should.Throw<ArgumentOutOfRangeException>(() => dictionary["c"][0]);
        Should.Throw<ArgumentOutOfRangeException>(() => dictionary["c"][0] = 1);
        dictionary.ContainsKey("c").ShouldBeFalse();
    }

    [TestMethod]
    public void RemoveAt()
    {
        var dictionary = Create();
        dictionary["a"].RemoveAt(0);

        dictionary["a"].ShouldBe([2, 3]);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveAtDetachesEmptiedKey()
    {
        var dictionary = Create();
        dictionary["b"].RemoveAt(0);

        dictionary.ContainsKey("b").ShouldBeFalse();
        dictionary.Count.ShouldBe(1);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveRange()
    {
        var dictionary = Create();
        dictionary["a"].RemoveRange(0, 2);

        dictionary["a"].ShouldBe([3]);
        dictionary.ValueCount.ShouldBe(2);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveRangeAllDetachesKey()
    {
        var dictionary = Create();
        dictionary["a"].RemoveRange(0, 3);

        dictionary.ContainsKey("a").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(1);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveRangeZeroCountOnDetachedListIsNoOp()
    {
        var dictionary = Create();
        dictionary["c"].RemoveRange(0, 0);

        dictionary.ContainsKey("c").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(4);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveAll()
    {
        var dictionary = Create();
        dictionary["a"].RemoveAll(v => v % 2 == 1).ShouldBe(2);

        dictionary["a"].ShouldBe([2]);
        dictionary.ValueCount.ShouldBe(2);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveAllNoMatch()
    {
        var dictionary = Create();
        dictionary["a"].RemoveAll(v => v > 100).ShouldBe(0);

        dictionary["a"].ShouldBe([1, 2, 3]);
        dictionary.ValueCount.ShouldBe(4);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveAllEverythingDetachesKey()
    {
        var dictionary = Create();
        dictionary["a"].RemoveAll(v => true).ShouldBe(3);

        dictionary.ContainsKey("a").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(1);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveAllOnDetachedListIsNoOp()
    {
        var dictionary = Create();
        dictionary["c"].RemoveAll(v => true).ShouldBe(0);

        dictionary.ContainsKey("c").ShouldBeFalse();
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveMissingValueReturnsFalse()
    {
        var dictionary = Create();
        dictionary["a"].Remove(99).ShouldBeFalse();
        dictionary["c"].Remove(1).ShouldBeFalse();

        dictionary.ValueCount.ShouldBe(4);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void RemoveFirstOccurrenceOnly()
    {
        var dictionary = new ListDictionary<string, int>();
        dictionary["a"].AddRange([1, 2, 1]);

        dictionary["a"].Remove(1).ShouldBeTrue();

        dictionary["a"].ShouldBe([2, 1]);
        dictionary.ValueCount.ShouldBe(2);
    }

    [TestMethod]
    public void ClearValueListDetachesKey()
    {
        var dictionary = Create();
        var list = dictionary["a"];
        list.Clear();

        list.Count.ShouldBe(0);
        dictionary.ContainsKey("a").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(1);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void ClearOnDetachedListIsNoOp()
    {
        var dictionary = Create();
        dictionary["c"].Clear();

        dictionary.ContainsKey("c").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(4);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void Reverse()
    {
        var dictionary = Create();
        dictionary["a"].Reverse();
        dictionary["a"].ShouldBe([3, 2, 1]);

        dictionary["a"].Reverse(1, 2);
        dictionary["a"].ShouldBe([3, 1, 2]);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void ReverseInvalidatesValueEnumeration()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary["a"].Reverse();

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void Sort()
    {
        var dictionary = new ListDictionary<string, int>();
        dictionary["a"].AddRange([3, 1, 2]);

        dictionary["a"].Sort();
        dictionary["a"].ShouldBe([1, 2, 3]);

        dictionary["a"].Sort((x, y) => y.CompareTo(x));
        dictionary["a"].ShouldBe([3, 2, 1]);

        dictionary["a"].Sort(Comparer<int>.Default);
        dictionary["a"].ShouldBe([1, 2, 3]);

        dictionary["a"].Sort(1, 2, Comparer<int>.Create((x, y) => y.CompareTo(x)));
        dictionary["a"].ShouldBe([1, 3, 2]);
    }

    [TestMethod]
    public void SortInvalidatesValueEnumeration()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary["a"].Sort();

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void SetRangeReplacesValues()
    {
        var dictionary = Create();
        dictionary["a"].SetRange([7, 8]);

        dictionary["a"].ShouldBe([7, 8]);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetRangeAttachesNewKey()
    {
        var dictionary = Create();
        dictionary["c"].SetRange([7, 8]);

        dictionary.ContainsKey("c").ShouldBeTrue();
        dictionary["c"].ShouldBe([7, 8]);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetRangeEmptyOnDetachedListIsNoOp()
    {
        var dictionary = Create();
        dictionary["c"].SetRange([]);

        dictionary.ContainsKey("c").ShouldBeFalse();
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetRangeFailureLeavesListUntouched()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() => dictionary["a"].SetRange(new[] { 5 }.ThenThrow()));
        Should.Throw<InvalidOperationException>(() => dictionary["c"].SetRange(new[] { 5 }.ThenThrow()));

        dictionary["a"].ShouldBe([1, 2, 3]);
        dictionary.ContainsKey("c").ShouldBeFalse();
        dictionary.ValueCount.ShouldBe(4);
        enumerator.MoveNext().ShouldBeTrue();
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SetRangeFromProjectionOfSelf()
    {
        var dictionary = Create();
        var list = dictionary["a"];

        list.SetRange(list.Where(v => v > 1));

        dictionary["a"].ShouldBe([2, 3]);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void DetachedInstanceSyncsToNewlyAttachedList()
    {
        var dictionary = new ListDictionary<string, int>();
        var stale = dictionary["a"];

        dictionary["a"].Add(1);

        stale.Count.ShouldBe(1);
        stale.ShouldBe([1]);
        stale.Add(2);

        dictionary["a"].ShouldBe([1, 2]);
        dictionary.ValueCount.ShouldBe(2);
        dictionary.ValueLists.Contains(stale).ShouldBeTrue();
    }

    [TestMethod]
    public void DetachedThenReattachedThroughOtherInstance()
    {
        var dictionary = Create();
        var original = dictionary["a"];

        dictionary.Clear("a");
        original.Count.ShouldBe(0);

        dictionary["a"].AddRange([5, 6]);

        original.ShouldBe([5, 6]);
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
        original.Add(42);

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

        // Read-only wrappers are a different type and are never equal to the mutable list.
        a1.AsReadOnly().Equals(a1).ShouldBeFalse();
        a1.AsReadOnly().Equals(a2.AsReadOnly()).ShouldBeTrue();
    }

    [TestMethod]
    public void EqualityUsesKeyComparer()
    {
        var dictionary = new ListDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        dictionary["a"].Add(1);

        (dictionary["A"] == dictionary["a"]).ShouldBeTrue();
        dictionary["A"].GetHashCode().ShouldBe(dictionary["a"].GetHashCode());
    }

    [TestMethod]
    public void AsReadOnlyIsLiveViewAndCached()
    {
        var dictionary = Create();
        var list = dictionary["a"];
        var readOnly = list.AsReadOnly();

        readOnly.ShouldBeSameAs(list.AsReadOnly());
        readOnly.Key.ShouldBe("a");
        readOnly.Dictionary.ShouldBeSameAs(dictionary);
        readOnly.ShouldBe([1, 2, 3]);

        list.Add(4);
        readOnly.ShouldBe([1, 2, 3, 4]);
        readOnly[3].ShouldBe(4);

        list.Clear();
        readOnly.Count.ShouldBe(0);

        dictionary["a"].Add(9);
        readOnly.ShouldBe([9]);
    }

    [TestMethod]
    public void ReadOnlyInterfaceMutationsThrow()
    {
        var dictionary = Create();
        IList<int> readOnly = dictionary["a"].AsReadOnly();

        readOnly.IsReadOnly.ShouldBeTrue();
        Should.Throw<NotSupportedException>(() => readOnly.Add(1));
        Should.Throw<NotSupportedException>(() => readOnly.Insert(0, 1));
        Should.Throw<NotSupportedException>(() => readOnly.Remove(1));
        Should.Throw<NotSupportedException>(() => readOnly.RemoveAt(0));
        Should.Throw<NotSupportedException>(() => readOnly.Clear());
        Should.Throw<NotSupportedException>(() => readOnly[0] = 1);

        readOnly[0].ShouldBe(1);
        readOnly.IndexOf(2).ShouldBe(1);
        readOnly.Contains(3).ShouldBeTrue();
    }

    [TestMethod]
    public void MutableInterfaceOperations()
    {
        var dictionary = Create();
        IList<int> list = dictionary["a"];

        list.IsReadOnly.ShouldBeFalse();
        list.Add(4);
        list.Insert(0, 0);
        list[1] = 11;
        list.Remove(2).ShouldBeTrue();
        list.RemoveAt(0);

        dictionary["a"].ShouldBe([11, 3, 4]);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void AsTransientReadOnlyTracksCurrentList()
    {
        var dictionary = Create();
        var list = dictionary["a"];
        var transient = list.AsTransientReadOnly();

        transient.ShouldBe([1, 2, 3]);
        transient.ShouldBeSameAs(list.AsTransientReadOnly());

        dictionary.Clear("a");
        dictionary["a"].AddRange([7]);

        list.AsTransientReadOnly().ShouldBe([7]);
    }

    [TestMethod]
    public void SearchAndCopyMembers()
    {
        var dictionary = Create();
        var list = dictionary["a"];

        list.IndexOf(2).ShouldBe(1);
        list.IndexOf(2, 2).ShouldBe(-1);
        list.IndexOf(3, 0, 3).ShouldBe(2);
        list.LastIndexOf(1).ShouldBe(0);
        list.LastIndexOf(3, 2).ShouldBe(2);
        list.LastIndexOf(1, 2, 3).ShouldBe(0);
        list.Contains(3).ShouldBeTrue();
        list.Exists(v => v == 2).ShouldBeTrue();
        list.Find(v => v > 1).ShouldBe(2);
        list.FindLast(v => v < 3).ShouldBe(2);
        list.FindAll(v => v != 2).ShouldBe([1, 3]);
        list.FindIndex(v => v == 3).ShouldBe(2);
        list.FindIndex(1, v => v == 1).ShouldBe(-1);
        list.FindIndex(0, 2, v => v == 3).ShouldBe(-1);
        list.FindLastIndex(v => v == 1).ShouldBe(0);
        list.FindLastIndex(1, v => v == 3).ShouldBe(-1);
        list.FindLastIndex(2, 3, v => v == 2).ShouldBe(1);
        list.TrueForAll(v => v > 0).ShouldBeTrue();
        list.GetRange(1, 2).ShouldBe([2, 3]);
        list.ToArray().ShouldBe([1, 2, 3]);

        int sum = 0;
        list.ForEach(v => sum += v);
        sum.ShouldBe(6);

        int[] array = new int[5];
        list.CopyTo(array);
        array.ShouldBe([1, 2, 3, 0, 0]);

        array = new int[5];
        list.CopyTo(array, 2);
        array.ShouldBe([0, 0, 1, 2, 3]);

        array = new int[5];
        list.CopyTo(1, array, 3, 2);
        array.ShouldBe([0, 0, 0, 2, 3]);
    }

    [TestMethod]
    public void EnumeratorThrowsWhenListModified()
    {
        var dictionary = Create();
        var list = dictionary["a"];

        using var enumerator = list.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        list.Add(4);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void EnumeratorThrowsWhenListDetachedAndReattachedElsewhere()
    {
        var dictionary = Create();
        var list = dictionary["a"];

        using var enumerator = list.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary.Clear("a");
        dictionary["a"].Add(5);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void EnumeratorOnDetachedListThrowsAfterAttachElsewhere()
    {
        var dictionary = Create();
        var list = dictionary["c"];

        using var enumerator = list.GetEnumerator();
        dictionary["c"].Add(5);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void EnumeratorOnDetachedListIsEmpty()
    {
        var dictionary = Create();
        dictionary["c"].ShouldBeEmpty();
        dictionary["c"].AsReadOnly().ShouldBeEmpty();
        dictionary["c"].AsTransientReadOnly().ShouldBeEmpty();
    }

    [TestMethod]
    public void CapacityAndTrimExcess()
    {
        var dictionary = Create();
        var list = dictionary["a"];

        list.Capacity = 10;
        list.Capacity.ShouldBe(10);

        list.TrimExcess();
        list.Capacity.ShouldBe(3);
        dictionary["a"].ShouldBe([1, 2, 3]);
    }

#if NET8_0_OR_GREATER

    [TestMethod]
    public void EnsureCapacity()
    {
        var dictionary = Create();
        dictionary["a"].EnsureCapacity(20).ShouldBeGreaterThanOrEqualTo(20);
        dictionary["a"].Capacity.ShouldBeGreaterThanOrEqualTo(20);
        dictionary["a"].ShouldBe([1, 2, 3]);
    }

#endif
}
