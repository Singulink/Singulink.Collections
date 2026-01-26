namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class GetNodeIndexTests
{
    [TestMethod]
    public void GetIndexOfNodesInSimpleList()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        // Check first, middle, and last node indices
        list.UnsafeGetIndexOfNode(node1).ShouldBe((nint)0);
        list.UnsafeGetIndexOfNode(node2).ShouldBe((nint)1);
        list.UnsafeGetIndexOfNode(node3).ShouldBe((nint)2);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void GetIndexOfSingleNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        list.UnsafeGetIndexOfNode(node).ShouldBe((nint)0);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetIndexOfRemovedNodeReturnsMinusOne()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        list.Remove(node2);

        list.UnsafeGetIndexOfNode(node2).ShouldBe((nint)(-1));

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void GetIndexOfDisposedNodeReturnsMinusOne()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        node.Dispose();

        list.UnsafeGetIndexOfNode(node).ShouldBe((nint)(-1));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetIndexAfterRemovingNodes()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        object value4 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);
        var node4 = list.AddLast(value4);

        // Remove first node - others should shift down
        list.Remove(node1);
        list.UnsafeGetIndexOfNode(node1).ShouldBe((nint)(-1));
        list.UnsafeGetIndexOfNode(node2).ShouldBe((nint)0);
        list.UnsafeGetIndexOfNode(node3).ShouldBe((nint)1);
        list.UnsafeGetIndexOfNode(node4).ShouldBe((nint)2);

        // Remove last node - others should be unchanged
        list.Remove(node4);
        list.UnsafeGetIndexOfNode(node4).ShouldBe((nint)(-1));
        list.UnsafeGetIndexOfNode(node2).ShouldBe((nint)0);
        list.UnsafeGetIndexOfNode(node3).ShouldBe((nint)1);

        // Remove middle node
        list.Remove(node3);
        list.UnsafeGetIndexOfNode(node3).ShouldBe((nint)(-1));
        list.UnsafeGetIndexOfNode(node2).ShouldBe((nint)0);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
        GC.KeepAlive(value4);
    }

    [TestMethod]
    public void GetIndexWithNodesAddedInDifferentOrder()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        object value4 = new();
        object value5 = new();

        // Add in non-sequential order
        var node3 = list.AddLast(value3);  // Position 0
        var node1 = list.AddFirst(value1); // Position 0, node3 becomes 1
        var node5 = list.AddLast(value5);  // Position 2
        var node2 = list.AddAfter(node1, value2); // Position 1
        var node4 = list.AddBefore(node5, value4); // Position 3

        list.UnsafeGetIndexOfNode(node1).ShouldBe((nint)0);
        list.UnsafeGetIndexOfNode(node2).ShouldBe((nint)1);
        list.UnsafeGetIndexOfNode(node3).ShouldBe((nint)2);
        list.UnsafeGetIndexOfNode(node4).ShouldBe((nint)3);
        list.UnsafeGetIndexOfNode(node5).ShouldBe((nint)4);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
        GC.KeepAlive(value4);
        GC.KeepAlive(value5);
    }

    [TestMethod]
    public void GetIndexOfLargeListSequentialAddition()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 1000).Select((_) => new object()).ToList();
        var nodes = values.Select(list.AddLast).ToList();

        for (int i = 0; i < nodes.Count; i++)
        {
            list.UnsafeGetIndexOfNode(nodes[i]).ShouldBe((nint)i);
        }

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void GetIndexAfterManyRemovalsInLargeList()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 100).Select((_) => new object()).ToList();
        var nodes = values.Select(list.AddLast).ToList();

        // Remove every other node starting from index 1 (odd indices)
        for (int i = 1; i < nodes.Count; i += 2)
        {
            list.Remove(nodes[i]);
        }

        // Verify removed nodes return -1
        for (int i = 1; i < nodes.Count; i += 2)
        {
            list.UnsafeGetIndexOfNode(nodes[i]).ShouldBe((nint)(-1));
        }

        // Verify remaining count
        list.Count.ShouldBe(50);

        // Verify remaining nodes have correct indices (0, 2, 4, ... become 0, 1, 2, ...)
        for (int i = 0; i < nodes.Count; i += 2)
        {
            list.UnsafeGetIndexOfNode(nodes[i]).ShouldBe((nint)(i / 2));
        }

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void GetIndexOfNodeFromDifferentListThrows()
    {
        var list1 = new ConcurrentWeakList<object>();
        var list2 = new ConcurrentWeakList<object>();
        object value = new();
        var node = list1.AddLast(value);

        Should.Throw<InvalidOperationException>(() => list2.UnsafeGetIndexOfNode(node));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetIndexOfNodeOnDisposedListThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);
        list.Dispose();

        Should.Throw<ObjectDisposedException>(() => list.UnsafeGetIndexOfNode(node));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void GetIndexOfNullNodeThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.UnsafeGetIndexOfNode(null!));
    }
}
