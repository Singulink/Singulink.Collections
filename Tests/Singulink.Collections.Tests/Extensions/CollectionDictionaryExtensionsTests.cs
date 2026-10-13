namespace Singulink.Collections.Tests.Extensions;

[PrefixTestClass]
public class CollectionDictionaryExtensionsTests
{
    private static ListDictionary<string, int> CreateListDictionary()
    {
        var dictionary = new ListDictionary<string, int>();
        dictionary["a"].AddRange([1, 2, 3]);
        dictionary["b"].Add(10);
        return dictionary;
    }

    private static HashSetDictionary<string, int> CreateSetDictionary()
    {
        var dictionary = new HashSetDictionary<string, int>();
        dictionary["a"].AddRange([1, 2, 3]);
        dictionary["b"].Add(10);
        return dictionary;
    }

    [TestMethod]
    public void AsCollectionDictionaryFromListDictionary()
    {
        var source = CreateListDictionary();
        var adapter = source.AsCollectionDictionary();

        // Note: members inherited from both IReadOnlyCollectionDictionary instantiations are ambiguous through ICollectionDictionary<TKey, TValue>, so
        // they are accessed through the read-only interface here.
        IReadOnlyCollectionDictionary<string, int> readOnly = adapter;

        readOnly.Count.ShouldBe(2);
        readOnly.ValueCount.ShouldBe(4);
        adapter["a"].ShouldBeSameAs(source["a"]);
        readOnly.Keys.ShouldBe(["a", "b"], ignoreOrder: true);
        readOnly.Values.ShouldBe([1, 2, 3, 10], ignoreOrder: true);
        adapter.ValueCollections.Count.ShouldBe(2);
        adapter.ValueCollections.Select(c => c.Key).ShouldBe(["a", "b"], ignoreOrder: true);
        readOnly.Contains("a", 2).ShouldBeTrue();
        readOnly.Contains("a", 9).ShouldBeFalse();
        readOnly.ContainsKey("b").ShouldBeTrue();
        readOnly.ContainsValue(10).ShouldBeTrue();
        readOnly.GetValueCount("a").ShouldBe(3);
        readOnly.GetValueCount("c").ShouldBe(0);

        adapter.TryGetValues("a", out var collection).ShouldBeTrue();
        collection.ShouldBeSameAs(source["a"]);
        adapter.TryGetValues("c", out collection).ShouldBeFalse();
        collection.ShouldBeNull();

        readOnly["b"].ShouldBe([10]);
        readOnly.ValueCollections.Count.ShouldBe(2);
        readOnly.TryGetValues("b", out var readOnlyCollection).ShouldBeTrue();
        readOnlyCollection.ShouldBeSameAs(source["b"]);

        // Mutations flow through to the source.

        adapter["c"].Add(20);
        source["c"].ShouldBe([20]);

        adapter.Clear("a").ShouldBeTrue();
        adapter.Clear("a").ShouldBeFalse();
        source.ContainsKey("a").ShouldBeFalse();

        adapter.Clear();
        source.Count.ShouldBe(0);
        readOnly.Count.ShouldBe(0);
        adapter.ValueCollections.ShouldBeEmpty();
    }

    [TestMethod]
    public void AsCollectionDictionaryFromSetDictionary()
    {
        var source = CreateSetDictionary();
        var adapter = source.AsCollectionDictionary();

        ((IReadOnlyCollectionDictionary<string, int>)adapter).Count.ShouldBe(2);
        adapter["a"].ShouldBeSameAs(source["a"]);
        adapter.ValueCollections.Select(c => c.Key).ShouldBe(["a", "b"], ignoreOrder: true);

        adapter["c"].Add(20);
        source["c"].ShouldBe([20]);

        adapter.Clear("b").ShouldBeTrue();
        source.ContainsKey("b").ShouldBeFalse();
    }

    [TestMethod]
    public void AsReadOnlyFromCollectionDictionary()
    {
        var source = CreateListDictionary();
        ICollectionDictionary<string, int> collectionDictionary = source.AsCollectionDictionary();
        var readOnly = collectionDictionary.AsReadOnly();

        readOnly.Count.ShouldBe(2);
        readOnly.ValueCount.ShouldBe(4);
        readOnly["a"].ShouldBeSameAs(source["a"]);
        readOnly.Keys.ShouldBe(["a", "b"], ignoreOrder: true);
        readOnly.Values.ShouldBe([1, 2, 3, 10], ignoreOrder: true);
        readOnly.ValueCollections.Count.ShouldBe(2);
        readOnly.ValueCollections.Select(c => c.Key).ShouldBe(["a", "b"], ignoreOrder: true);
        readOnly.Contains("b", 10).ShouldBeTrue();
        readOnly.ContainsKey("a").ShouldBeTrue();
        readOnly.ContainsValue(3).ShouldBeTrue();
        readOnly.GetValueCount("b").ShouldBe(1);
        readOnly.TryGetValues("b", out var collection).ShouldBeTrue();
        collection.ShouldBeSameAs(source["b"]);
        readOnly.TryGetValues("c", out collection).ShouldBeFalse();
        collection.ShouldBeNull();

        source["c"].Add(5);
        readOnly.Count.ShouldBe(3);
    }

    [TestMethod]
    public void ReadOnlyAdaptersReflectSourceChanges()
    {
        var source = CreateListDictionary();
        var readOnlyList = source.AsReadOnly();
        var readOnlySet = CreateSetDictionary().AsReadOnly();

        readOnlyList.Count.ShouldBe(2);
        readOnlyList.ValueCount.ShouldBe(4);
        readOnlyList.Keys.ShouldBe(["a", "b"], ignoreOrder: true);
        readOnlyList.Contains("a", 1).ShouldBeTrue();
        readOnlyList.ContainsKey("c").ShouldBeFalse();
        readOnlyList.GetValueCount("a").ShouldBe(3);
        readOnlyList.TryGetValues("c", out var list).ShouldBeFalse();
        list.ShouldBeNull();
        readOnlyList.TryGetValues("a", out list).ShouldBeTrue();
        list.ShouldBe([1, 2, 3]);

        source["c"].Add(7);
        readOnlyList.Count.ShouldBe(3);
        readOnlyList["c"].ShouldBe([7]);
        readOnlyList.ValueCollections.Count.ShouldBe(3);

        readOnlySet.Count.ShouldBe(2);
        readOnlySet.Values.ShouldBe([1, 2, 3, 10], ignoreOrder: true);
        readOnlySet.ValueCollections.Select(c => c.Key).ShouldBe(["a", "b"], ignoreOrder: true);
        readOnlySet.TryGetValues("a", out var set).ShouldBeTrue();
        set.SetEquals([1, 2, 3]).ShouldBeTrue();
    }

    [TestMethod]
    public void AsReadOnlyCollectionDictionaryFromReadOnlyAdapters()
    {
        var listSource = CreateListDictionary();
        var fromList = listSource.AsReadOnly().AsReadOnlyCollectionDictionary();
        var fromSet = CreateSetDictionary().AsReadOnly().AsReadOnlyCollectionDictionary();

        fromList.Count.ShouldBe(2);
        fromList.ValueCollections.Count.ShouldBe(2);
        fromList.TryGetValues("a", out var collection).ShouldBeTrue();
        collection.ShouldBe([1, 2, 3]);

        fromSet.Count.ShouldBe(2);
        fromSet.ValueCollections.Count.ShouldBe(2);
        fromSet["a"].ShouldBe([1, 2, 3], ignoreOrder: true);
    }

    [TestMethod]
    public void AsLookupOnReadOnlyCollectionDictionary()
    {
        var source = CreateListDictionary();
        IReadOnlyCollectionDictionary<string, int> readOnly = source.AsReadOnly().AsReadOnlyCollectionDictionary();
        var lookup = readOnly.AsLookup();

        lookup.Count.ShouldBe(2);
        lookup.Contains("a").ShouldBeTrue();
        lookup.Contains("c").ShouldBeFalse();
        lookup["a"].ShouldBe([1, 2, 3]);
        lookup["c"].ShouldBeEmpty();

        var groupings = lookup.ToList();
        groupings.Select(g => g.Key).ShouldBe(["a", "b"], ignoreOrder: true);
        groupings.Single(g => g.Key == "a").ShouldBe([1, 2, 3]);

        source["c"].Add(5);
        lookup.Count.ShouldBe(3);
        lookup["c"].ShouldBe([5]);

        var items = new List<object>();

        foreach (var g in (System.Collections.IEnumerable)lookup)
            items.Add(g);

        items.Count.ShouldBe(3);
    }

    [TestMethod]
    public void AsLookupOnReadOnlyListAndSetDictionaries()
    {
        var listLookup = CreateListDictionary().AsReadOnly().AsLookup();
        var setLookup = CreateSetDictionary().AsReadOnly().AsLookup();

        listLookup.Count.ShouldBe(2);
        listLookup["a"].ShouldBe([1, 2, 3]);
        listLookup.Select(g => g.Key).ShouldBe(["a", "b"], ignoreOrder: true);

        setLookup.Count.ShouldBe(2);
        setLookup["a"].ShouldBe([1, 2, 3], ignoreOrder: true);
        setLookup.Select(g => g.Key).ShouldBe(["a", "b"], ignoreOrder: true);
    }

    [TestMethod]
    public void ToLookupSnapshotsGroupings()
    {
        var source = CreateListDictionary();
        var lookup = source.AsReadOnly().ToLookup();

        var groupings = lookup.ToList();
        groupings.Select(g => g.Key).ShouldBe(["a", "b"], ignoreOrder: true);
        groupings.Single(g => g.Key == "a").ShouldBe([1, 2, 3]);

        source["a"].Add(4);
        source["c"].Add(5);

        lookup["a"].ShouldBe([1, 2, 3]);
        lookup.Contains("c").ShouldBeFalse();
        lookup["c"].ShouldBeEmpty();

        var items = new List<object>();

        foreach (var g in (System.Collections.IEnumerable)lookup)
            items.Add(g);

        items.Count.ShouldBe(2);

        var values = new List<object>();

        foreach (var v in (System.Collections.IEnumerable)groupings[0])
            values.Add(v);

        values.Count.ShouldBe(groupings[0].Count());
    }

    [TestMethod]
    public void ToLookupExtensionUsesSpecifiedComparerOnly()
    {
        var source = new ListDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        source["a"].Add(1);

        // The instance method uses the dictionary comparer.
        source.ToLookup().Contains("A").ShouldBeTrue();

        // The extension defaults to the default comparer and otherwise uses the one provided.
        source.AsReadOnly().ToLookup().Contains("A").ShouldBeFalse();
        source.AsReadOnly().ToLookup().Contains("a").ShouldBeTrue();
        source.AsReadOnly().ToLookup(StringComparer.OrdinalIgnoreCase).Contains("A").ShouldBeTrue();
        source.AsReadOnly().ToLookup(StringComparer.OrdinalIgnoreCase)["A"].ShouldBe([1]);

        var setSource = new HashSetDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        setSource["a"].Add(1);
        setSource.ToLookup().Contains("A").ShouldBeTrue();
        setSource.AsReadOnly().ToLookup().Contains("A").ShouldBeFalse();
        setSource.AsReadOnly().ToLookup(StringComparer.OrdinalIgnoreCase).Contains("A").ShouldBeTrue();

        IReadOnlyCollectionDictionary<string, int> collectionDictionary = source.AsReadOnly().AsReadOnlyCollectionDictionary();
        collectionDictionary.ToLookup().Contains("A").ShouldBeFalse();
        collectionDictionary.ToLookup(StringComparer.OrdinalIgnoreCase).Contains("A").ShouldBeTrue();
    }

    [TestMethod]
    public void ToLookupOnEmptyDictionary()
    {
        var lookup = new ListDictionary<string, int>().ToLookup();

        lookup.Count.ShouldBe(0);
        lookup.ShouldBeEmpty();
        lookup["a"].ShouldBeEmpty();
    }
}
