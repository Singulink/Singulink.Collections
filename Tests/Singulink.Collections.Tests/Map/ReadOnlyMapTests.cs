namespace Singulink.Collections.Tests.Map;

[PrefixTestClass]
public class ReadOnlyMapTests
{
    private static Map<int, string> Create() => new() {
        { 1, "one" },
        { 2, "two" },
    };

    [TestMethod]
    public void WrapsLiveMap()
    {
        var map = Create();
        var readOnly = new ReadOnlyMap<int, string>(map);

        readOnly.Count.ShouldBe(2);
        readOnly[1].ShouldBe("one");
        readOnly.LeftValues.ShouldBe([1, 2], ignoreOrder: true);
        readOnly.RightValues.ShouldBe(["one", "two"], ignoreOrder: true);
        readOnly.ShouldBe([new(1, "one"), new(2, "two")], ignoreOrder: true);

        map.Add(3, "three");

        readOnly.Count.ShouldBe(3);
        readOnly[3].ShouldBe("three");
        readOnly.Contains(3, "three").ShouldBeTrue();
        readOnly.Contains(3, "tres").ShouldBeFalse();
        readOnly.ContainsLeft(3).ShouldBeTrue();
        readOnly.ContainsLeft(4).ShouldBeFalse();
        readOnly.ContainsRight("three").ShouldBeTrue();
        readOnly.ContainsRight("four").ShouldBeFalse();

        readOnly.TryGetLeftValue("two", out int left).ShouldBeTrue();
        left.ShouldBe(2);
        readOnly.TryGetLeftValue("four", out left).ShouldBeFalse();

        readOnly.TryGetRightValue(2, out string? right).ShouldBeTrue();
        right.ShouldBe("two");
        readOnly.TryGetRightValue(4, out right).ShouldBeFalse();
        right.ShouldBeNull();

        Should.Throw<KeyNotFoundException>(() => readOnly[4]);
    }

    [TestMethod]
    public void ReverseIsLiveAndCached()
    {
        var map = Create();
        var readOnly = new ReadOnlyMap<int, string>(map);
        var reverse = readOnly.Reverse;

        reverse.ShouldBeSameAs(readOnly.Reverse);
        reverse.ShouldBeOfType<ReadOnlyMap<string, int>>();
        reverse["one"].ShouldBe(1);
        reverse.Count.ShouldBe(2);

        map.Add(3, "three");
        reverse["three"].ShouldBe(3);
        reverse.Reverse[3].ShouldBe("three");

        ((IMap<int, string>)readOnly).Reverse.ShouldBeSameAs(reverse);
    }

    [TestMethod]
    public void MutationsThroughInterfacesThrow()
    {
        var map = Create();
        IMap<int, string> readOnly = new ReadOnlyMap<int, string>(map);

        Should.Throw<NotSupportedException>(() => readOnly[1] = "uno");
        Should.Throw<NotSupportedException>(() => readOnly.Add(3, "three"));
        Should.Throw<NotSupportedException>(() => readOnly.Remove(1, "one"));
        Should.Throw<NotSupportedException>(() => readOnly.RemoveLeft(1));
        Should.Throw<NotSupportedException>(() => readOnly.RemoveRight("one"));
        Should.Throw<NotSupportedException>(() => readOnly.Set(1, "uno"));
        Should.Throw<NotSupportedException>(() => readOnly.TryAdd(3, "three"));

        ICollection<KeyValuePair<int, string>> collection = readOnly;
        collection.IsReadOnly.ShouldBeTrue();
        Should.Throw<NotSupportedException>(() => collection.Add(new(3, "three")));
        Should.Throw<NotSupportedException>(() => collection.Remove(new(1, "one")));
        Should.Throw<NotSupportedException>(() => collection.Clear());

        map.Count.ShouldBe(2);
        map[1].ShouldBe("one");
    }

    [TestMethod]
    public void CollectionInterfaceReads()
    {
        var map = Create();
        ICollection<KeyValuePair<int, string>> collection = new ReadOnlyMap<int, string>(map);

        collection.Count.ShouldBe(2);
        collection.Contains(new(1, "one")).ShouldBeTrue();
        collection.Contains(new(1, "two")).ShouldBeFalse();

        var array = new KeyValuePair<int, string>[3];
        collection.CopyTo(array, 1);
        array[0].ShouldBe(default);
        array.Skip(1).ShouldBe([new(1, "one"), new(2, "two")], ignoreOrder: true);

        var pairs = new List<KeyValuePair<int, string>>();

        foreach (var pair in (System.Collections.IEnumerable)collection)
            pairs.Add((KeyValuePair<int, string>)pair);

        pairs.ShouldBe([new(1, "one"), new(2, "two")], ignoreOrder: true);
    }
}
