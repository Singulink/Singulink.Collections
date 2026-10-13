namespace Singulink.Collections.Tests.HashSetDictionary;

[PrefixTestClass]
public class CollectionViewTests
{
    private static HashSetDictionary<string, int> Create()
    {
        var dictionary = new HashSetDictionary<string, int>();
        dictionary["a"].AddRange([1, 2, 3]);
        dictionary["b"].Add(10);
        return dictionary;
    }

    [TestMethod]
    public void ConstructorNegativeCapacityThrows()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new HashSetDictionary<string, int>(-1));
        Should.Throw<ArgumentOutOfRangeException>(() => new HashSetDictionary<string, int>(-1, null, null));
    }

    [TestMethod]
    public void KeyComparer()
    {
        new HashSetDictionary<string, int>().KeyComparer.ShouldBe(EqualityComparer<string>.Default);
        new HashSetDictionary<string, int>(StringComparer.OrdinalIgnoreCase).KeyComparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);

        var dictionary = new HashSetDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        dictionary["a"].Add(1);
        dictionary["A"].Add(2);

        dictionary.Count.ShouldBe(1);
        dictionary["A"].ShouldBe([1, 2], ignoreOrder: true);
        dictionary.ContainsKey("A").ShouldBeTrue();
    }

    [TestMethod]
    public void KeysContainsAndCopyTo()
    {
        var dictionary = Create();

        dictionary.Keys.Count.ShouldBe(2);
        dictionary.Keys.Contains("a").ShouldBeTrue();
        dictionary.Keys.Contains("c").ShouldBeFalse();
        dictionary.Keys.ShouldBe(["a", "b"], ignoreOrder: true);

        string[] array = new string[3];
        dictionary.Keys.CopyTo(array, 1);
        array[0].ShouldBeNull();
        array.Skip(1).ShouldBe(["a", "b"], ignoreOrder: true);
    }

    [TestMethod]
    public void KeysAreReadOnlyThroughInterface()
    {
        ICollection<string> keys = Create().Keys;

        keys.IsReadOnly.ShouldBeTrue();
        Should.Throw<NotSupportedException>(() => keys.Add("z"));
        Should.Throw<NotSupportedException>(() => keys.Remove("a"));
        Should.Throw<NotSupportedException>(() => keys.Clear());
    }

    [TestMethod]
    public void KeysEnumeratorInvalidatedByChange()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Keys.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary["a"].Add(4);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void ValuesEnumeratesAllSets()
    {
        var dictionary = Create();
        dictionary["c"].AddRange([20, 21]);

        dictionary.Values.Count.ShouldBe(6);
        dictionary.Values.ShouldBe([1, 2, 3, 10, 20, 21], ignoreOrder: true);
        dictionary.Values.Contains(21).ShouldBeTrue();
        dictionary.Values.Contains(99).ShouldBeFalse();
    }

    [TestMethod]
    public void ValuesEmpty()
    {
        var dictionary = new HashSetDictionary<string, int>();
        dictionary.Values.ShouldBeEmpty();
        dictionary.Values.Count.ShouldBe(0);
        dictionary.Keys.ShouldBeEmpty();
        dictionary.ValueSets.ShouldBeEmpty();
    }

    [TestMethod]
    public void ValuesCopyTo()
    {
        var dictionary = Create();

        int[] array = new int[6];
        dictionary.Values.CopyTo(array, 2);

        array.Take(2).ShouldBe([0, 0]);
        array.Skip(2).ShouldBe([1, 2, 3, 10], ignoreOrder: true);
    }

    [TestMethod]
    public void ValuesCopyToInvalidArgumentsThrow()
    {
        var dictionary = Create();

        Should.Throw<ArgumentException>(() => dictionary.Values.CopyTo(new int[3], 0));
        Should.Throw<ArgumentException>(() => dictionary.Values.CopyTo(new int[4], 1));
        Should.Throw<ArgumentOutOfRangeException>(() => dictionary.Values.CopyTo(new int[4], 5));
        Should.Throw<ArgumentOutOfRangeException>(() => dictionary.Values.CopyTo(new int[4], -1));
    }

    [TestMethod]
    public void ValuesAreReadOnlyThroughInterface()
    {
        ICollection<int> values = Create().Values;

        values.IsReadOnly.ShouldBeTrue();
        Should.Throw<NotSupportedException>(() => values.Add(1));
        Should.Throw<NotSupportedException>(() => values.Remove(1));
        Should.Throw<NotSupportedException>(() => values.Clear());
    }

    [TestMethod]
    public void ValuesEnumeratorInvalidatedByRemove()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary["b"].Remove(10);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void ValuesEnumeratorInvalidatedByClear()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary.Clear();

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void ValuesEnumeratorNotInvalidatedByNoOpChanges()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Values.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary["a"].Add(1).ShouldBeFalse();
        dictionary["a"].Remove(99).ShouldBeFalse();
        dictionary["c"].AddRange([]).ShouldBe(0);
        dictionary.Clear("c").ShouldBeFalse();

        enumerator.MoveNext().ShouldBeTrue();
    }

    [TestMethod]
    public void ValueSetsMembers()
    {
        var dictionary = Create();
        var other = Create();
        var valueSets = dictionary.ValueSets;

        valueSets.Dictionary.ShouldBeSameAs(dictionary);
        valueSets.Count.ShouldBe(2);
        valueSets.Select(s => s.Key).ShouldBe(["a", "b"], ignoreOrder: true);

        valueSets.Contains(dictionary["a"]).ShouldBeTrue();
        valueSets.Contains(dictionary["c"]).ShouldBeFalse();
        valueSets.Contains(other["a"]).ShouldBeFalse();

        var array = new HashSetDictionary<string, int>.ValueSet[3];
        valueSets.CopyTo(array, 1);
        array[0].ShouldBeNull();
        array.Skip(1).Select(s => s.Key).ShouldBe(["a", "b"], ignoreOrder: true);
    }

    [TestMethod]
    public void ValueSetsAreReadOnlyThroughInterface()
    {
        var dictionary = Create();
        ICollection<HashSetDictionary<string, int>.ValueSet> valueSets = dictionary.ValueSets;

        valueSets.IsReadOnly.ShouldBeTrue();
        Should.Throw<NotSupportedException>(() => valueSets.Add(dictionary["z"]));
        Should.Throw<NotSupportedException>(() => valueSets.Remove(dictionary["a"]));
        Should.Throw<NotSupportedException>(() => valueSets.Clear());
    }

    [TestMethod]
    public void ValueSetsEnumeratorInvalidatedByChange()
    {
        var dictionary = Create();
        using var enumerator = dictionary.ValueSets.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary["a"].Add(4);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void DictionaryEnumeratesValueSets()
    {
        var dictionary = Create();
        var keys = new List<string>();

        foreach (var set in dictionary)
            keys.Add(set.Key);

        keys.ShouldBe(["a", "b"], ignoreOrder: true);
    }

    [TestMethod]
    public void ContainsAndCounts()
    {
        var dictionary = Create();

        dictionary.Contains("a", 2).ShouldBeTrue();
        dictionary.Contains("a", 10).ShouldBeFalse();
        dictionary.Contains("c", 1).ShouldBeFalse();
        dictionary.ContainsValue(10).ShouldBeTrue();
        dictionary.ContainsValue(11).ShouldBeFalse();
        dictionary.GetValueCount("a").ShouldBe(3);
        dictionary.GetValueCount("c").ShouldBe(0);
        dictionary.TryGetValues("c", out var missing).ShouldBeFalse();
        missing.ShouldBeNull();
    }

    [TestMethod]
    public void ClearKey()
    {
        var dictionary = Create();

        dictionary.Clear("c").ShouldBeFalse();
        dictionary.Clear("a").ShouldBeTrue();
        dictionary.Clear("a").ShouldBeFalse();

        dictionary.Count.ShouldBe(1);
        dictionary.ValueCount.ShouldBe(1);
    }

    [TestMethod]
    public void ClearEmptyDictionary()
    {
        var dictionary = new HashSetDictionary<string, int>();
        dictionary.Clear();
        dictionary.Count.ShouldBe(0);
        dictionary.ValueCount.ShouldBe(0);
    }

    [TestMethod]
    public void ExplicitInterfaceViews()
    {
        var dictionary = Create();

        ISetDictionary<string, int> setDictionary = dictionary;
        setDictionary["a"].ShouldBeSameAs(dictionary["a"]);
        setDictionary.ValueCollections.Count.ShouldBe(2);
        setDictionary.TryGetValues("a", out var keyedSet).ShouldBeTrue();
        keyedSet.ShouldBeSameAs(dictionary["a"]);
        setDictionary.TryGetValues("c", out keyedSet).ShouldBeFalse();
        keyedSet.ShouldBeNull();

        IReadOnlySetDictionary<string, int> readOnly = dictionary;
        readOnly.Keys.Count.ShouldBe(2);
        readOnly.Values.Count.ShouldBe(4);
        readOnly.ValueCollections.Count.ShouldBe(2);
        readOnly["b"].ShouldBe([10]);
        readOnly.TryGetValues("b", out var readOnlySet).ShouldBeTrue();
        readOnlySet.ShouldBeSameAs(dictionary["b"]);

        ICollectionDictionary<string, int, IKeyedSet<string, int>> collectionDictionary = dictionary;
        collectionDictionary["a"].Count.ShouldBe(3);
        collectionDictionary.Count.ShouldBe(2);
        collectionDictionary.ValueCollections.Count.ShouldBe(2);
        collectionDictionary.TryGetValues("a", out var keyedCollection).ShouldBeTrue();
        keyedCollection.ShouldBeSameAs(dictionary["a"]);
    }

#if !NETFRAMEWORK

    [TestMethod]
    public void EnsureCapacityAndTrimExcessPreserveContents()
    {
        var dictionary = Create();

        dictionary.EnsureCapacity(100).ShouldBeGreaterThanOrEqualTo(100);
        dictionary["a"].ShouldBe([1, 2, 3], ignoreOrder: true);

        dictionary.TrimExcess();
        dictionary.TrimExcess(trimValueSets: false);
        dictionary.TrimExcess(10, trimValueSets: true);

        dictionary["a"].ShouldBe([1, 2, 3], ignoreOrder: true);
        dictionary.Count.ShouldBe(2);
        dictionary.ValueCount.ShouldBe(4);
    }

    [TestMethod]
    public void EnsureCapacityInvalidatesEnumerators()
    {
        var dictionary = Create();
        using var enumerator = dictionary.Keys.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        dictionary.EnsureCapacity(100);

        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

#endif
}
