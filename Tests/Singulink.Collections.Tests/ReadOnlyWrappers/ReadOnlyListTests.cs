namespace Singulink.Collections.Tests.ReadOnlyWrappers;

[PrefixTestClass]
public class ReadOnlyListTests
{
    [TestMethod]
    public void EmptyIsSharedAndEmpty()
    {
        ReadOnlyList<int>.Empty.ShouldBeSameAs(ReadOnlyList<int>.Empty);
        ReadOnlyList<int>.Empty.Count.ShouldBe(0);
        ReadOnlyList<int>.Empty.ShouldBeEmpty();
    }

    [TestMethod]
    public void WrapsLiveList()
    {
        var list = new List<int> { 1, 2, 3 };
        var readOnly = new ReadOnlyList<int>(list);

        readOnly.Count.ShouldBe(3);
        readOnly[1].ShouldBe(2);
        readOnly.ShouldBe([1, 2, 3]);

        list.Add(4);

        readOnly.Count.ShouldBe(4);
        readOnly[3].ShouldBe(4);
        Should.Throw<ArgumentOutOfRangeException>(() => readOnly[4]);
    }

    [TestMethod]
    public void DelegatedMembers()
    {
        var readOnly = new ReadOnlyList<int>([1, 2, 3, 2]);

        readOnly.BinarySearch(2).ShouldBeInRange(1, 1);
        readOnly.BinarySearch(2, Comparer<int>.Default).ShouldBe(1);
        readOnly.BinarySearch(0, 3, 3, null).ShouldBe(2);
        readOnly.Contains(3).ShouldBeTrue();
        readOnly.Contains(9).ShouldBeFalse();
        readOnly.ConvertAll(v => v.ToString()).ShouldBe(["1", "2", "3", "2"]);
        readOnly.Exists(v => v == 3).ShouldBeTrue();
        readOnly.Find(v => v > 1).ShouldBe(2);
        readOnly.FindAll(v => v == 2).ShouldBe([2, 2]);
        readOnly.FindIndex(v => v == 2).ShouldBe(1);
        readOnly.FindIndex(2, v => v == 2).ShouldBe(3);
        readOnly.FindIndex(0, 1, v => v == 2).ShouldBe(-1);
        readOnly.FindLast(v => v < 3).ShouldBe(2);
        readOnly.FindLastIndex(v => v == 2).ShouldBe(3);
        readOnly.FindLastIndex(2, v => v == 2).ShouldBe(1);
        readOnly.FindLastIndex(3, 1, v => v == 2).ShouldBe(3);
        readOnly.GetRange(1, 2).ShouldBe([2, 3]);
        readOnly.IndexOf(2).ShouldBe(1);
        readOnly.IndexOf(2, 2).ShouldBe(3);
        readOnly.IndexOf(2, 2, 1).ShouldBe(-1);
        readOnly.LastIndexOf(2).ShouldBe(3);
        readOnly.LastIndexOf(2, 2).ShouldBe(1);
        readOnly.LastIndexOf(2, 3, 1).ShouldBe(3);
        readOnly.ToArray().ShouldBe([1, 2, 3, 2]);
        readOnly.TrueForAll(v => v > 0).ShouldBeTrue();

        int sum = 0;
        readOnly.ForEach(v => sum += v);
        sum.ShouldBe(8);

        int[] array = new int[6];
        readOnly.CopyTo(array);
        array.ShouldBe([1, 2, 3, 2, 0, 0]);

        array = new int[6];
        readOnly.CopyTo(array, 2);
        array.ShouldBe([0, 0, 1, 2, 3, 2]);

        array = new int[6];
        readOnly.CopyTo(1, array, 4, 2);
        array.ShouldBe([0, 0, 0, 0, 2, 3]);
    }

    [TestMethod]
    public void MutationsThroughInterfacesThrow()
    {
        var list = new List<int> { 1, 2, 3 };
        IList<int> readOnly = new ReadOnlyList<int>(list);

        readOnly.IsReadOnly.ShouldBeTrue();
        readOnly[0].ShouldBe(1);
        readOnly.IndexOf(3).ShouldBe(2);

        Should.Throw<NotSupportedException>(() => readOnly[0] = 9);
        Should.Throw<NotSupportedException>(() => readOnly.Add(9));
        Should.Throw<NotSupportedException>(() => readOnly.Insert(0, 9));
        Should.Throw<NotSupportedException>(() => readOnly.Remove(1));
        Should.Throw<NotSupportedException>(() => readOnly.RemoveAt(0));
        Should.Throw<NotSupportedException>(() => readOnly.Clear());

        list.ShouldBe([1, 2, 3]);
    }

    [TestMethod]
    public void EnumeratesThroughInterfaces()
    {
        var readOnly = new ReadOnlyList<int>([1, 2]);

        ((IEnumerable<int>)readOnly).ShouldBe([1, 2]);

        var items = new List<int>();

        foreach (var item in (System.Collections.IEnumerable)readOnly)
            items.Add((int)item);

        items.ShouldBe([1, 2]);
        ((IReadOnlyList<int>)readOnly)[1].ShouldBe(2);
    }
}
