namespace Singulink.Collections.Tests.ListDictionary;

[TestClass]
public class AsReadOnlyTests
{
    [TestMethod]
    public void AsReadOnlyListDictionary()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(new[] { "one", "uno", "1" });
        d[2].AddRange(new[] { "two", "dos", "2" });

        var rod = d.AsReadOnlyListDictionary();

        rod.Values.ShouldBe(new[] { "one", "uno", "1", "two", "dos", "2" }, ignoreOrder: true);
        rod[1].ShouldBe(new[] { "one", "uno", "1" });
        rod.ValueCollections.ShouldBe(new[] { d[1], d[2] }, ignoreOrder: true);
        rod.Values.Contains("uno").ShouldBeTrue();
        rod.ContainsValue("dos").ShouldBeTrue();
    }

    [TestMethod]
    public void AsReadOnlyCollectionDictionary()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(new[] { "one", "uno", "1" });
        d[2].AddRange(new[] { "two", "dos", "2" });

        var rod = d.AsReadOnlyCollectionDictionary();

        rod.Values.ShouldBe(new[] { "one", "uno", "1", "two", "dos", "2" }, ignoreOrder: true);
        rod[1].ShouldBe(new[] { "one", "uno", "1" });
        rod.ValueCollections.ShouldBe(new[] { d[1], d[2] }, ignoreOrder: true);
        rod.Values.Contains("uno").ShouldBeTrue();
        rod.ContainsValue("dos").ShouldBeTrue();
    }

    [TestMethod]
    public void AsReadOnlyListDictionaryToReadOnlyCollectionDictionary()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(new[] { "one", "uno", "1" });
        d[2].AddRange(new[] { "two", "dos", "2" });

        var rod = d.AsReadOnlyListDictionary().AsReadOnlyCollectionDictionary();

        rod.Values.ShouldBe(new[] { "one", "uno", "1", "two", "dos", "2" }, ignoreOrder: true);
        rod[1].ShouldBe(new[] { "one", "uno", "1" });
        rod.ValueCollections.ShouldBe(new[] { d[1], d[2] }, ignoreOrder: true);
        rod.Values.Contains("uno").ShouldBeTrue();
        rod.ContainsValue("dos").ShouldBeTrue();
    }

    [TestMethod]
    public void AsReadOnlyDictionaryOfList()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(new[] { "one", "uno", "1" });
        d[2].AddRange(new[] { "two", "dos", "2" });

        var rod = d.AsReadOnlyDictionaryOfList();

        rod[1].ShouldBe(new[] { "one", "uno", "1" });
        rod[1][2].ShouldBe("1");
        rod.Values.ShouldBe(new[] { d[1], d[2] }, ignoreOrder: true);
        rod.Count.ShouldBe(2);
        rod[1].Contains("uno").ShouldBe(true);
        rod[2].Contains("uno").ShouldBe(false);
        rod.Contains(new(2, d[2])).ShouldBe(true);
        rod.Contains(new(2, d[1])).ShouldBe(false);
    }

    [TestMethod]
    public void AsReadOnlyDictionaryOfCollection()
    {
        var d = new ListDictionary<int, string>();
        d[1].AddRange(new[] { "one", "uno", "1" });
        d[2].AddRange(new[] { "two", "dos", "2" });

        var rod = d.AsReadOnlyDictionaryOfCollection();

        rod[1].ShouldBe(new[] { "one", "uno", "1" });
        rod.Values.ShouldBe(new[] { d[1], d[2] }, ignoreOrder: true);
        rod.Count.ShouldBe(2);
        rod[1].Contains("uno").ShouldBe(true);
        rod[2].Contains("uno").ShouldBe(false);
        rod.Contains(new(2, d[2])).ShouldBe(true);
        rod.Contains(new(2, d[1])).ShouldBe(false);
    }
}