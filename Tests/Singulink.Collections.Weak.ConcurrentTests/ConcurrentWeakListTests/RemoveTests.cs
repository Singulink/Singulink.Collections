namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class RemoveTests
{
    [TestMethod]
    public void RemoveSingleNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        // Verify .List and .Value before removal
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeSameAs(value);

        list.Remove(node);

        list.Count.ShouldBe(0);
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list); // List reference is retained
        node.Value.ShouldBeNull(); // Value is cleared on removal
        list.ToList().ShouldBeEmpty();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveFirstNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        list.AddLast(value2);
        list.AddLast(value3);

        list.Remove(node1);

        list.Count.ShouldBe(2);
        node1.IsRemoved.ShouldBeTrue();
        node1.List.ShouldBeSameAs(list);
        node1.Value.ShouldBeNull();
        list.ToList().ShouldBe([value2, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemoveMiddleNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        list.Remove(node2);

        list.Count.ShouldBe(2);
        node2.IsRemoved.ShouldBeTrue();
        node2.List.ShouldBeSameAs(list);
        node2.Value.ShouldBeNull();
        list.ToList().ShouldBe([value1, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemoveLastNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        list.AddLast(value2);
        var node3 = list.AddLast(value3);

        list.Remove(node3);

        list.Count.ShouldBe(2);
        node3.IsRemoved.ShouldBeTrue();
        node3.List.ShouldBeSameAs(list);
        node3.Value.ShouldBeNull();
        list.ToList().ShouldBe([value1, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemoveViaListMultipleTimes()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        list.Remove(node);
        list.Remove(node); // Should not throw
        list.Remove(node); // Should not throw

        list.Count.ShouldBe(0);
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void DisposeNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        // Verify .List and .Value before disposal
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeSameAs(value);

        node.Dispose();

        list.Count.ShouldBe(0);
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list); // List reference is retained
        node.Value.ShouldBeNull(); // Value is cleared on disposal
        list.ToList().ShouldBeEmpty();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void DisposeFirstNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        list.AddLast(value2);
        list.AddLast(value3);

        node1.Dispose();

        list.Count.ShouldBe(2);
        node1.IsRemoved.ShouldBeTrue();
        node1.List.ShouldBeSameAs(list);
        node1.Value.ShouldBeNull();
        list.ToList().ShouldBe([value2, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void DisposeMiddleNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        node2.Dispose();

        list.Count.ShouldBe(2);
        node2.IsRemoved.ShouldBeTrue();
        node2.List.ShouldBeSameAs(list);
        node2.Value.ShouldBeNull();
        list.ToList().ShouldBe([value1, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void DisposeNodeMultipleTimes()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        node.Dispose();
        node.Dispose(); // Should not throw
        node.Dispose(); // Should not throw

        list.Count.ShouldBe(0);
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveThenDispose()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        list.Remove(node);
        node.Dispose(); // Should not throw

        list.Count.ShouldBe(0);
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void DisposeThenRemove()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        node.Dispose();
        list.Remove(node); // Should not throw

        list.Count.ShouldBe(0);
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveAllNodesOneByOne()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 10).Select((_) => new object()).ToList();
        var nodes = values.Select(list.AddLast).ToList();

        for (int i = 0; i < nodes.Count; i++)
        {
            list.Remove(nodes[i]);
            list.Count.ShouldBe(nodes.Count - i - 1);
            nodes[i].IsRemoved.ShouldBeTrue();
            nodes[i].List.ShouldBeSameAs(list);
            nodes[i].Value.ShouldBeNull();
        }

        list.Count.ShouldBe(0);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void DisposeAllNodesOneByOne()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 10).Select((_) => new object()).ToList();
        var nodes = values.Select(list.AddLast).ToList();

        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Dispose();
            list.Count.ShouldBe(nodes.Count - i - 1);
            nodes[i].IsRemoved.ShouldBeTrue();
            nodes[i].List.ShouldBeSameAs(list);
            nodes[i].Value.ShouldBeNull();
        }

        list.Count.ShouldBe(0);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void IsRemovedIsFalseForActiveNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        node.IsRemoved.ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IsRemovedIsTrueAfterRemove()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        node.IsRemoved.ShouldBeFalse();
        list.Remove(node);
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IsRemovedIsTrueAfterDispose()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        node.IsRemoved.ShouldBeFalse();
        node.Dispose();
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IsRemovedIsTrueAfterListDispose()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        node.IsRemoved.ShouldBeFalse();
        list.Dispose();
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void IsRemovedIsTrueAfterClear()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        node.IsRemoved.ShouldBeFalse();
        list.Clear();
        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list);
        node.Value.ShouldBeNull();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveFromLargeList()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 100).Select((_) => new object()).ToList();
        var nodes = values.Select(list.AddLast).ToList();

        // Remove every other node
        for (int i = 0; i < nodes.Count; i += 2)
        {
            list.Remove(nodes[i]);
            nodes[i].IsRemoved.ShouldBeTrue();
            nodes[i].List.ShouldBeSameAs(list);
            nodes[i].Value.ShouldBeNull();
        }

        list.Count.ShouldBe(50);

        // Verify remaining values
        var expectedValues = new List<object>();
        for (int i = 1; i < values.Count; i += 2)
            expectedValues.Add(values[i]);

        list.ToList().ShouldBe(expectedValues);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void RemoveNullNodeThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.Remove((ConcurrentWeakList<object>.Node)null!));
    }
}
