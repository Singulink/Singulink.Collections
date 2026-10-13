namespace Singulink.Collections.Tests.ReadOnlyWrappers;

[PrefixTestClass]
public class ReadOnlyHashSetTests
{
    [TestMethod]
    public void EmptyIsSharedAndEmpty()
    {
        ReadOnlyHashSet<int>.Empty.ShouldBeSameAs(ReadOnlyHashSet<int>.Empty);
        ReadOnlyHashSet<int>.Empty.Count.ShouldBe(0);
        ReadOnlyHashSet<int>.Empty.ShouldBeEmpty();
    }

    [TestMethod]
    public void WrapsLiveSet()
    {
        var set = new HashSet<int> { 1, 2, 3 };
        var readOnly = new ReadOnlyHashSet<int>(set);

        readOnly.Count.ShouldBe(3);
        readOnly.Contains(2).ShouldBeTrue();
        readOnly.ShouldBe([1, 2, 3], ignoreOrder: true);

        set.Add(4);

        readOnly.Count.ShouldBe(4);
        readOnly.Contains(4).ShouldBeTrue();
    }

    [TestMethod]
    public void DelegatedMembers()
    {
        var readOnly = new ReadOnlyHashSet<int>([1, 2, 3]);

        readOnly.IsSubsetOf([1, 2, 3, 4]).ShouldBeTrue();
        readOnly.IsProperSubsetOf([1, 2, 3]).ShouldBeFalse();
        readOnly.IsSupersetOf([1]).ShouldBeTrue();
        readOnly.IsProperSupersetOf([1, 2, 3]).ShouldBeFalse();
        readOnly.Overlaps([3, 4]).ShouldBeTrue();
        readOnly.SetEquals([3, 2, 1]).ShouldBeTrue();

        int[] array = new int[5];
        readOnly.CopyTo(array);
        array.Take(3).ShouldBe([1, 2, 3], ignoreOrder: true);

        array = new int[5];
        readOnly.CopyTo(array, 2);
        array.Take(2).ShouldBe([0, 0]);
        array.Skip(2).ShouldBe([1, 2, 3], ignoreOrder: true);
    }

    [TestMethod]
    public void MutationsThroughInterfacesThrow()
    {
        var set = new HashSet<int> { 1, 2, 3 };
        ISet<int> readOnly = new ReadOnlyHashSet<int>(set);

        readOnly.IsReadOnly.ShouldBeTrue();
        Should.Throw<NotSupportedException>(() => readOnly.Add(9));
        Should.Throw<NotSupportedException>(() => ((ICollection<int>)readOnly).Add(9));
        Should.Throw<NotSupportedException>(() => readOnly.Remove(1));
        Should.Throw<NotSupportedException>(() => readOnly.Clear());
        Should.Throw<NotSupportedException>(() => readOnly.UnionWith([9]));
        Should.Throw<NotSupportedException>(() => readOnly.IntersectWith([1]));
        Should.Throw<NotSupportedException>(() => readOnly.ExceptWith([1]));
        Should.Throw<NotSupportedException>(() => readOnly.SymmetricExceptWith([1]));

        set.ShouldBe([1, 2, 3], ignoreOrder: true);
    }

    [TestMethod]
    public void EnumeratesThroughInterfaces()
    {
        var readOnly = new ReadOnlyHashSet<int>([1, 2]);

        ((IEnumerable<int>)readOnly).ShouldBe([1, 2], ignoreOrder: true);
        ((IReadOnlyCollection<int>)readOnly).Count.ShouldBe(2);

        var items = new List<int>();

        foreach (var item in (System.Collections.IEnumerable)readOnly)
            items.Add((int)item);

        items.ShouldBe([1, 2], ignoreOrder: true);
    }

#if !NETFRAMEWORK

    [TestMethod]
    public void TryGetValue()
    {
        var readOnly = new ReadOnlyHashSet<string>(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Value" });

        readOnly.TryGetValue("VALUE", out string? actual).ShouldBeTrue();
        actual.ShouldBe("Value");
        readOnly.TryGetValue("other", out actual).ShouldBeFalse();
        actual.ShouldBeNull();
    }

#endif
}
