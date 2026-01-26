namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class GetNodeAtTests
{
    [TestMethod]
    public void GetNodeAtZeroInSingleItemList()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var expectedNode = list.AddLast(value);

        var node = list.UnsafeGetNodeAt(0);

        node.ShouldBeSameAs(expectedNode);
        node.Value.ShouldBeSameAs(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetNodeAtFirstMiddleAndLast()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        list.UnsafeGetNodeAt(0).ShouldBeSameAs(node1);
        list.UnsafeGetNodeAt(1).ShouldBeSameAs(node2);
        list.UnsafeGetNodeAt(2).ShouldBeSameAs(node3);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void GetNodeAtAllIndicesInLargeList()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 100).Select((_) => new object()).ToList();
        var nodes = values.Select(list.AddLast).ToList();

        for (int i = 0; i < nodes.Count; i++)
        {
            list.UnsafeGetNodeAt(i).ShouldBeSameAs(nodes[i]);
        }

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void GetNodeAtAfterRemovingEarlierNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        list.Remove(node1);

        // After removing node1, node2 should be at index 0 and node3 at index 1
        list.UnsafeGetNodeAt(0).ShouldBeSameAs(node2);
        list.UnsafeGetNodeAt(1).ShouldBeSameAs(node3);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void GetNodeAtAfterRemovingMiddleNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        list.Remove(node2);

        list.UnsafeGetNodeAt(0).ShouldBeSameAs(node1);
        list.UnsafeGetNodeAt(1).ShouldBeSameAs(node3);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void GetNodeAtAfterInsertions()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        object value4 = new();

        var node1 = list.AddLast(value1);
        var node3 = list.AddLast(value3);
        var node2 = list.UnsafeInsertAt(value2, 1);
        var node4 = list.AddFirst(value4);

        // Expected order: value4, value1, value2, value3
        list.UnsafeGetNodeAt(0).ShouldBeSameAs(node4);
        list.UnsafeGetNodeAt(1).ShouldBeSameAs(node1);
        list.UnsafeGetNodeAt(2).ShouldBeSameAs(node2);
        list.UnsafeGetNodeAt(3).ShouldBeSameAs(node3);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
        GC.KeepAlive(value4);
    }

    [TestMethod]
    public void GetNodeAtNegativeIndexThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentOutOfRangeException>(() => list.UnsafeGetNodeAt(-1));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetNodeAtIndexEqualToCountThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentOutOfRangeException>(() => list.UnsafeGetNodeAt(1));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetNodeAtIndexGreaterThanCountThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentOutOfRangeException>(() => list.UnsafeGetNodeAt(5));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetNodeAtOnEmptyListThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentOutOfRangeException>(() => list.UnsafeGetNodeAt(0));
    }

    [TestMethod]
    public void GetNodeAtOnDisposedListThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);
        list.Dispose();

        Should.Throw<ObjectDisposedException>(() => list.UnsafeGetNodeAt(0));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetNodeAtWithMixedOperations()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        object value4 = new();
        object value5 = new();

        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);
        list.Remove(node2);
        var node4 = list.AddAfter(node1, value4);
        var node5 = list.AddBefore(node3, value5);

        // Expected order: value1, value4, value5, value3
        list.UnsafeGetNodeAt(0).ShouldBeSameAs(node1);
        list.UnsafeGetNodeAt(1).ShouldBeSameAs(node4);
        list.UnsafeGetNodeAt(2).ShouldBeSameAs(node5);
        list.UnsafeGetNodeAt(3).ShouldBeSameAs(node3);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
        GC.KeepAlive(value4);
        GC.KeepAlive(value5);
    }
}
