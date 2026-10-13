namespace Singulink.Collections.Tests.HashSetDictionary;

[PrefixTestClass]
public class ValueSetOperationTests
{
    private static HashSetDictionary<string, int> Create()
    {
        var dictionary = new HashSetDictionary<string, int>();
        dictionary["a"].Add(1);
        dictionary["a"].Add(2);
        dictionary["a"].Add(3);
        dictionary["b"].Add(10);
        return dictionary;
    }

    private static void ShouldBeConsistent(HashSetDictionary<string, int> dictionary)
    {
        dictionary.ValueCount.ShouldBe(dictionary.ValueSets.Sum(s => s.Count));
        dictionary.Count.ShouldBe(dictionary.ValueSets.Count());
        dictionary.ValueSets.ShouldAllBe(s => s.Count > 0);
    }

    [TestMethod]
    public void UnionWith()
    {
        var dictionary = Create();
        dictionary["a"].UnionWith([3, 4, 5]);

        dictionary["a"].ShouldBe([1, 2, 3, 4, 5], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void UnionWithNoChange()
    {
        var dictionary = Create();
        dictionary["a"].UnionWith([1, 2]);

        dictionary["a"].ShouldBe([1, 2, 3], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(4);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void UnionWithAttachesNewKey()
    {
        var dictionary = Create();
        var set = dictionary["c"];

        dictionary.ContainsKey("c").ShouldBeFalse();
        set.UnionWith([7, 8]);

        dictionary.ContainsKey("c").ShouldBeTrue();
        dictionary["c"].ShouldBe([7, 8], ignoreOrder: true);
        dictionary.Count.ShouldBe(3);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void IntersectWith()
    {
        var dictionary = Create();
        dictionary["a"].IntersectWith([2, 3, 4]);

        dictionary["a"].ShouldBe([2, 3], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void IntersectWithDetachesEmptiedKey()
    {
        var dictionary = Create();
        dictionary["b"].IntersectWith([99]);

        dictionary.ContainsKey("b").ShouldBeFalse();
        dictionary.Count.ShouldBe(1);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void ExceptWith()
    {
        var dictionary = Create();
        dictionary["a"].ExceptWith([1, 99]);

        dictionary["a"].ShouldBe([2, 3], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SymmetricExceptWithRemovesOnly()
    {
        var dictionary = Create();
        dictionary["a"].SymmetricExceptWith([1, 2]);

        dictionary["a"].ShouldBe([3]);
        dictionary.ValueCount.ShouldBe(2);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SymmetricExceptWithAddsOnly()
    {
        var dictionary = Create();
        dictionary["a"].SymmetricExceptWith([4, 5]);

        dictionary["a"].ShouldBe([1, 2, 3, 4, 5], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SymmetricExceptWithAddsAndRemoves()
    {
        var dictionary = Create();
        dictionary["a"].SymmetricExceptWith([2, 3, 4, 5]);

        dictionary["a"].ShouldBe([1, 4, 5], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(4);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SymmetricExceptWithSameCountDifferentContents()
    {
        var dictionary = Create();
        var set = dictionary["a"];

        // Removes 3 and adds 4, so the count is unchanged but the contents are not.
        using var enumerator = set.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();

        set.SymmetricExceptWith([3, 4]);

        set.ShouldBe([1, 2, 4], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(4);
        ShouldBeConsistent(dictionary);
        Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void SymmetricExceptWithAttachesNewKey()
    {
        var dictionary = Create();
        var set = dictionary["c"];

        set.SymmetricExceptWith([7, 8]);

        dictionary.ContainsKey("c").ShouldBeTrue();
        dictionary["c"].ShouldBe([7, 8], ignoreOrder: true);
        dictionary.ValueCount.ShouldBe(6);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SymmetricExceptWithDetachesEmptiedKey()
    {
        var dictionary = Create();
        dictionary["b"].SymmetricExceptWith([10]);

        dictionary.ContainsKey("b").ShouldBeFalse();
        dictionary.Count.ShouldBe(1);
        dictionary.ValueCount.ShouldBe(3);
        ShouldBeConsistent(dictionary);
    }

    [TestMethod]
    public void SymmetricExceptWithEmptyOtherOnDetachedSet()
    {
        var dictionary = Create();
        var set = dictionary["c"];

        set.SymmetricExceptWith([]);

        dictionary.ContainsKey("c").ShouldBeFalse();
        dictionary.Count.ShouldBe(2);
        ShouldBeConsistent(dictionary);
    }
}
