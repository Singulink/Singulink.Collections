namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class VersionTests
{
    [TestMethod]
    public void VersionIncreasesAfterAdd()
    {
        var list = new ConcurrentWeakList<object>();
        var versionBefore = list.Version;

        object value = new();
        list.AddLast(value);

        var versionAfter = list.Version;
        (versionAfter > versionBefore).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void VersionDoesNotChangeOnRemoveOrClear()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);
        var versionAfterAdd = list.Version;

        list.Remove(node);
        (list.Version == versionAfterAdd).ShouldBeTrue();

        // Re-add and clear
        list.AddLast(value);
        var versionBeforeClear = list.Version;
        list.Clear();
        (list.Version == versionBeforeClear).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void NodeVersionMatchesListVersionAtTimeOfAdd()
    {
        var list = new ConcurrentWeakList<object>();
        var versionBefore = list.Version;

        object value = new();
        var node = list.AddLast(value);

        // Node version should be greater than version before add
        (node.Version > versionBefore).ShouldBeTrue();

        // Node version should equal list version after add (since no more adds happened)
        (node.Version == list.Version).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void LaterNodesHaveHigherVersions()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();

        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        (node1.Version < node2.Version).ShouldBeTrue();
        (node2.Version < node3.Version).ShouldBeTrue();
        (node1.Version < node3.Version).ShouldBeTrue();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void ComparisonOperators()
    {
        var list = new ConcurrentWeakList<object>();
        var version1 = list.Version;
        var version1Copy = list.Version;

        object value = new();
        list.AddLast(value);

        var version2 = list.Version;

        // Equal case (version1 == version1Copy)
        (version1 == version1Copy).ShouldBeTrue();
        (version1 != version1Copy).ShouldBeFalse();
        (version1 < version1Copy).ShouldBeFalse();
        (version1 > version1Copy).ShouldBeFalse();
        (version1 <= version1Copy).ShouldBeTrue();
        (version1 >= version1Copy).ShouldBeTrue();

        // Less than case (version1 < version2)
        (version1 == version2).ShouldBeFalse();
        (version1 != version2).ShouldBeTrue();
        (version1 < version2).ShouldBeTrue();
        (version1 > version2).ShouldBeFalse();
        (version1 <= version2).ShouldBeTrue();
        (version1 >= version2).ShouldBeFalse();

        // Greater than case (version2 > version1)
        (version2 == version1).ShouldBeFalse();
        (version2 != version1).ShouldBeTrue();
        (version2 < version1).ShouldBeFalse();
        (version2 > version1).ShouldBeTrue();
        (version2 <= version1).ShouldBeFalse();
        (version2 >= version1).ShouldBeTrue();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void EqualsAndCompareTo()
    {
        var list = new ConcurrentWeakList<object>();
        var version1 = list.Version;
        var version1Copy = list.Version;

        object value = new();
        list.AddLast(value);

        var version2 = list.Version;

        // Equals - equal case
        version1.Equals(version1Copy).ShouldBeTrue();
        version1.Equals((object)version1Copy).ShouldBeTrue();

        // Equals - not equal case
        version1.Equals(version2).ShouldBeFalse();
        version1.Equals((object)version2).ShouldBeFalse();

        // Equals with invalid types
        version1.Equals("not a version").ShouldBeFalse();
        version1.Equals(null).ShouldBeFalse();

        // CompareTo - equal case
        version1.CompareTo(version1Copy).ShouldBe(0);

        // CompareTo - less than case
        version1.CompareTo(version2).ShouldBeLessThan(0);

        // CompareTo - greater than case
        version2.CompareTo(version1).ShouldBeGreaterThan(0);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetHashCodeConsistency()
    {
        var list = new ConcurrentWeakList<object>();
        var version1 = list.Version;
        var version1Copy = list.Version;

        // Same versions should have same hash code
        version1.GetHashCode().ShouldBe(version1Copy.GetHashCode());

        // Different versions should have different hash codes (not guaranteed, but very likely)
        object value = new();
        list.AddLast(value);
        var version2 = list.Version;
        version2.GetHashCode().ShouldNotBe(version1.GetHashCode());

        GC.KeepAlive(value);
    }
}
