namespace Singulink.Collections.Tests.ListDictionary;

[PrefixTestClass]
public class AsReadOnlyTests
{
    [TestMethod]
    public void AsReadOnlyListDictionary()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(["one", "uno", "1"]);
        d[2].AddRange(["two", "dos", "2"]);

        var rod = d.AsReadOnly();

        rod.Values.ShouldBe(["one", "uno", "1", "two", "dos", "2"], ignoreOrder: true);
        rod[1].ShouldBe(["one", "uno", "1"]);
        rod.ValueCollections.ShouldBe([d[1], d[2]], ignoreOrder: true);
        rod.Values.Contains("uno").ShouldBeTrue();
        rod.ContainsValue("dos").ShouldBeTrue();
    }

    [TestMethod]
    public void AsReadOnlyCollectionDictionary()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(["one", "uno", "1"]);
        d[2].AddRange(["two", "dos", "2"]);

        var rod = d.AsReadOnlyCollectionDictionary();

        rod.Values.ShouldBe(["one", "uno", "1", "two", "dos", "2"], ignoreOrder: true);
        rod[1].ShouldBe(["one", "uno", "1"]);
        rod.ValueCollections.ShouldBe([d[1], d[2]], ignoreOrder: true);
        rod.Values.Contains("uno").ShouldBeTrue();
        rod.ContainsValue("dos").ShouldBeTrue();
    }

    [TestMethod]
    public void AsReadOnlyListDictionaryToReadOnlyCollectionDictionary()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(["one", "uno", "1"]);
        d[2].AddRange(["two", "dos", "2"]);

        var rod = d.AsReadOnly().AsReadOnlyCollectionDictionary();

        rod.Values.ShouldBe(["one", "uno", "1", "two", "dos", "2"], ignoreOrder: true);
        rod[1].ShouldBe(["one", "uno", "1"]);
        rod.ValueCollections.ShouldBe([d[1], d[2]], ignoreOrder: true);
        rod.Values.Contains("uno").ShouldBeTrue();
        rod.ContainsValue("dos").ShouldBeTrue();
    }

    [TestMethod]
    public void AsLookup_LiveView()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(["one", "uno", "1"]);
        d[2].AddRange(["two", "dos", "2"]);

        var lookup = d.AsLookup();

        lookup.Count.ShouldBe(2);
        lookup.Contains(1).ShouldBeTrue();
        lookup.Contains(3).ShouldBeFalse();
        lookup[1].ShouldBe(["one", "uno", "1"]);
        lookup[3].ShouldBeEmpty();

        d[3].Add("three");
        lookup.Count.ShouldBe(3);
        lookup[3].ShouldBe(["three"]);
    }

    [TestMethod]
    public void ToLookup_Snapshot()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(["one", "uno", "1"]);
        d[2].AddRange(["two", "dos", "2"]);

        var lookup = d.ToLookup();

        lookup.Count.ShouldBe(2);
        lookup[1].ShouldBe(["one", "uno", "1"]);

        d[3].Add("three");
        lookup.Count.ShouldBe(2);
        lookup.Contains(3).ShouldBeFalse();
    }
}