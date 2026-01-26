using System.Runtime.CompilerServices;

namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class EnumerationTests
{
    [TestMethod]
    public void EnumerateEmpty()
    {
        var list = new ConcurrentWeakList<object>();

        var enumerator = list.GetEnumerator();
        enumerator.MoveNext().ShouldBeFalse();
    }

    [TestMethod]
    public void EnumerateNodesEmpty()
    {
        var list = new ConcurrentWeakList<object>();

        var enumerator = list.GetNodeEnumerator();
        enumerator.MoveNext().ShouldBeFalse();
    }

    [TestMethod]
    public void EnumerateOneValue()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        var enumerator = list.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(value);
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void EnumerateNodesOneValue()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        var enumerator = list.GetNodeEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node);
        enumerator.Current.Value.ShouldBeSameAs(value);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void EnumerateTwoDistinctValues()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        list.AddLast(value1);
        list.AddLast(value2);

        var enumerator = list.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(value1);
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(value2);
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void EnumerateNodesTwoDistinctValues()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);

        var enumerator = list.GetNodeEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.Current.Value.ShouldBeSameAs(value1);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.Current.Value.ShouldBeSameAs(value2);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void EnumerateTwoSameValues()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);
        list.AddLast(value);

        var enumerator = list.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(value);
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(value);
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void EnumerateNodesTwoSameValues()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node1 = list.AddLast(value);
        var node2 = list.AddLast(value);

        var enumerator = list.GetNodeEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.Current.Value.ShouldBeSameAs(value);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.Current.Value.ShouldBeSameAs(value);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveCurrentNodeWhileEnumerating()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        var enumerator = list.GetNodeEnumerator();

        // Move to first node and remove it
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        list.Remove(node1);
        node1.IsRemoved.ShouldBeTrue();

        // Should still be able to continue to the next nodes
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.Current.Value.ShouldBeSameAs(value2);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);
        enumerator.Current.Value.ShouldBeSameAs(value3);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemoveNextNodeWhileEnumerating()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        var enumerator = list.GetNodeEnumerator();

        // Move to first node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();

        // Remove the next node (node2)
        list.Remove(node2);
        node2.IsRemoved.ShouldBeTrue();

        // Should skip the removed node and continue to node3
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);
        enumerator.Current.Value.ShouldBeSameAs(value3);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemovePreviousNodeWhileEnumerating()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        var enumerator = list.GetNodeEnumerator();

        // Move to second node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();

        // Remove the previous node (node1)
        list.Remove(node1);
        node1.IsRemoved.ShouldBeTrue();

        // Should continue to node3
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);
        enumerator.Current.Value.ShouldBeSameAs(value3);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AddNodeAtEndWhileEnumerating()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        var node1 = list.AddLast(value1);

        var enumerator = list.GetNodeEnumerator();

        // Move to first node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();

        // Add a new node at the end
        var node2 = list.AddLast(value2);

        // Should see the new node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.Current.Value.ShouldBeSameAs(value2);
        enumerator.WasAddedDuringEnumeration.ShouldBeTrue();
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddNodeAtStartWhileEnumerating()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        var node1 = list.AddLast(value1);

        var enumerator = list.GetNodeEnumerator();

        // Move to first node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();

        // Add a new node at the start
        list.AddFirst(value2);

        // Should not see the new node via MoveNext (it was added before our current position)
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddNodeAfterCurrentWhileEnumerating()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node3 = list.AddLast(value3);

        var enumerator = list.GetNodeEnumerator();

        // Move to first node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);

        // Add a new node after the current node
        var node2 = list.AddAfter(node1, value2);

        // Should see the new node next
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.Current.Value.ShouldBeSameAs(value2);
        enumerator.WasAddedDuringEnumeration.ShouldBeTrue();

        // Then the original second node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);
        enumerator.Current.Value.ShouldBeSameAs(value3);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AddNodeBeforeCurrentWhileEnumerating()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node3 = list.AddLast(value3);

        var enumerator = list.GetNodeEnumerator();

        // Move to second node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);

        // Add a new node before the current node (between node1 and node3)
        list.AddBefore(node3, value2);

        // Should not see the new node via MoveNext (it was added before our current position)
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void MovePreviousEmpty()
    {
        var list = new ConcurrentWeakList<object>();

        var enumerator = list.GetNodeEnumerator();
        enumerator.MovePrevious().ShouldBeFalse();
    }

    [TestMethod]
    public void MovePreviousOneValue()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        var enumerator = list.GetNodeEnumerator();
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node);
        enumerator.Current.Value.ShouldBeSameAs(value);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MovePrevious().ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void MovePreviousTwoDistinctValues()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);

        var enumerator = list.GetNodeEnumerator();
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.Current.Value.ShouldBeSameAs(value2);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.Current.Value.ShouldBeSameAs(value1);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MovePrevious().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void MovePreviousTwoSameValues()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node1 = list.AddLast(value);
        var node2 = list.AddLast(value);

        var enumerator = list.GetNodeEnumerator();
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.Current.Value.ShouldBeSameAs(value);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.Current.Value.ShouldBeSameAs(value);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MovePrevious().ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddNodeAtEndWhileEnumeratingBackwards()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        var node1 = list.AddLast(value1);

        var enumerator = list.GetNodeEnumerator();

        // Move to last node (which is node1)
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();

        // Add a new node at the end
        list.AddLast(value2);

        // Should not see the new node via MovePrevious (it was added after our current position)
        enumerator.MovePrevious().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddNodeAtStartWhileEnumeratingBackwards()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        var node1 = list.AddLast(value1);

        var enumerator = list.GetNodeEnumerator();

        // Move to last node (which is node1)
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();

        // Add a new node at the start
        var node2 = list.AddFirst(value2);

        // Should see the new node via MovePrevious (it was added before our current position)
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.Current.Value.ShouldBeSameAs(value2);
        enumerator.WasAddedDuringEnumeration.ShouldBeTrue();
        enumerator.MovePrevious().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddNodeBeforeCurrentWhileEnumeratingBackwards()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node3 = list.AddLast(value3);

        var enumerator = list.GetNodeEnumerator();

        // Move backwards to node3
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);

        // Add a new node before node3 (between node1 and node3)
        var node2 = list.AddBefore(node3, value2);

        // Should see the new node via MovePrevious (it was added before our current position)
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.WasAddedDuringEnumeration.ShouldBeTrue();

        // Then node1
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.WasAddedDuringEnumeration.ShouldBeFalse();
        enumerator.MovePrevious().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AddNodeAfterCurrentWhileEnumeratingBackwards()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        list.AddLast(value3);

        var enumerator = list.GetNodeEnumerator();

        // Move backwards to node3, then to node1
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);

        // Add a new node after node1 (between node1 and node3)
        list.AddAfter(node1, value2);

        // Should not see the new node via MovePrevious (it was added after our current position)
        enumerator.MovePrevious().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void NavigateBackAndForth()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        var enumerator = list.GetNodeEnumerator();

        // Forward through all
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);

        // At end - MoveNext should fail
        enumerator.MoveNext().ShouldBeFalse();

        // Back through all
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);

        // At start - MovePrevious should fail
        enumerator.MovePrevious().ShouldBeFalse();

        // Forward again from before-start position
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);

        // Zigzag in the middle
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);

        // Start from end using MovePrevious from initial position
        var enumerator2 = list.GetNodeEnumerator();
        enumerator2.MovePrevious().ShouldBeTrue();
        enumerator2.Current.ShouldBeSameAs(node3);

        // At end - MoveNext should fail
        enumerator2.MoveNext().ShouldBeFalse();

        // Back from after-end position
        enumerator2.MovePrevious().ShouldBeTrue();
        enumerator2.Current.ShouldBeSameAs(node3);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AsEnumerableBasic()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        // Forward
        var nodeResult = list.GetNodeEnumerator().AsEnumerable().ToList();
        nodeResult.Count.ShouldBe(3);
        nodeResult[0].ShouldBeSameAs(node1);
        nodeResult[1].ShouldBeSameAs(node2);
        nodeResult[2].ShouldBeSameAs(node3);

        // Reversed
        nodeResult = list.GetNodeEnumerator().AsEnumerable(reversed: true).ToList();
        nodeResult.Count.ShouldBe(3);
        nodeResult[0].ShouldBeSameAs(node3);
        nodeResult[1].ShouldBeSameAs(node2);
        nodeResult[2].ShouldBeSameAs(node1);

        // Value enumerator forward
        var valueResult = list.GetEnumerator().AsEnumerable().ToList();
        valueResult.Count.ShouldBe(3);
        valueResult[0].ShouldBeSameAs(value1);
        valueResult[1].ShouldBeSameAs(value2);
        valueResult[2].ShouldBeSameAs(value3);

        // Value enumerator reversed
        valueResult = list.GetEnumerator().AsEnumerable(reversed: true).ToList();
        valueResult.Count.ShouldBe(3);
        valueResult[0].ShouldBeSameAs(value3);
        valueResult[1].ShouldBeSameAs(value2);
        valueResult[2].ShouldBeSameAs(value1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AsEnumerableFromOffsetPosition()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        // Starting from first node, forward - should get remaining nodes
        var enumerator = list.GetNodeEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        var result = enumerator.AsEnumerable().ToList();
        result.Count.ShouldBe(2);
        result[0].ShouldBeSameAs(node2);
        result[1].ShouldBeSameAs(node3);

        // Starting from second node, reversed - should get previous nodes
        enumerator = list.GetNodeEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.MoveNext().ShouldBeTrue();
        result = enumerator.AsEnumerable(reversed: true).ToList();
        result.Count.ShouldBe(1);
        result[0].ShouldBeSameAs(node1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void GetEnumeratorFromStartNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        // Start from node1 - should enumerate from node2 onwards (startNode is excluded)
        var enumerator = list.GetNodeEnumerator(node1);
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node2);
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);
        enumerator.MoveNext().ShouldBeFalse();

        // Start from node2 - forward should get node3
        enumerator = list.GetNodeEnumerator(node2);
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);
        enumerator.MoveNext().ShouldBeFalse();

        // Start from node2 - backward should get node1
        enumerator = list.GetNodeEnumerator(node2);
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);
        enumerator.MovePrevious().ShouldBeFalse();

        // Value enumerator with start node
        var valueEnumerator = list.GetEnumerator(node1);
        valueEnumerator.MoveNext().ShouldBeTrue();
        valueEnumerator.Current.ShouldBeSameAs(value2);
        valueEnumerator.MoveNext().ShouldBeTrue();
        valueEnumerator.Current.ShouldBeSameAs(value3);
        valueEnumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AsEnumerableSkipNewNodes()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);

        // Get enumerable with skipNewNodes - then add a new node during iteration
        var enumerator = list.GetNodeEnumerator();
        var enumerable = enumerator.AsEnumerable(skipNewNodes: true);

        object value3 = new();
        ConcurrentWeakList<object>.Node? node3 = null;
        var result = new List<ConcurrentWeakList<object>.Node>();

        foreach (var node in enumerable)
        {
            result.Add(node);
            if (node == node1)
            {
                // Add a new node while iterating
                node3 = list.AddLast(value3);
            }
        }

        // Should only have node1 and node2 - node3 was added during enumeration and should be skipped
        result.Count.ShouldBe(2);
        result[0].ShouldBeSameAs(node1);
        result[1].ShouldBeSameAs(node2);

        // Verify node3 exists but was skipped
        node3.ShouldNotBeNull();
        node3!.Value.ShouldBeSameAs(value3);

        // Without skipNewNodes - should include newly added nodes
        enumerator = list.GetNodeEnumerator();
        enumerable = enumerator.AsEnumerable(skipNewNodes: false);

        object value4 = new();
        result.Clear();

        foreach (var node in enumerable)
        {
            result.Add(node);
            if (node == node1)
            {
                list.AddLast(value4);
            }
        }

        // Should have all 4 nodes
        result.Count.ShouldBe(4);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
        GC.KeepAlive(value4);
    }

    [TestMethod]
    public void LinqOnListDirectly()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        list.AddLast(value2);
        list.AddLast(value3);

        // Using LINQ directly on list (IEnumerable<T>)
        var result = list.ToList();
        result.Count.ShouldBe(3);
        result[0].ShouldBeSameAs(value1);
        result[1].ShouldBeSameAs(value2);
        result[2].ShouldBeSameAs(value3);

        // Using LINQ with Where
        var filtered = list.Where((x) => x == value2).ToList();
        filtered.Count.ShouldBe(1);
        filtered[0].ShouldBeSameAs(value2);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AutoRemovedNodeSkippedDuringEnumeration()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node3 = list.AddLast(value3);

        // Add a value that will be collected - must be done in a non-inlined method & w/o putting it on the stack.
        var box = new StrongBox<object?>(null);
        var node2 = Helpers.NotInlined((list, node1, box), static (state) =>
        {
            state.box.Value = new object();
            return state.list.AddAfter(state.node1, state.box.Value);
        });

        // Start enumerating
        var enumerator = list.GetNodeEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node1);

        // Force GC to collect value2 after we null it out to remove all references.
        GC.KeepAlive(box);
        Helpers.NotInlined(box, static (box) => box.Value = null);
        Helpers.ForceGC();

        // node2 should be automatically removed
        node2.IsRemoved.ShouldBeTrue();

        // Continue enumeration - should skip the removed node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(node3);
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void NodeRemovedWhileLockedStaysInListDuringEnumeration()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        // This test emulates the race condition where a value dies while we're enumerating.
        // By holding the lock, another thread trying to remove a node should be blocked
        // until we release the lock.

        bool removeStarted = false;
        bool removeCompleted = false;
        Thread? removeThread = null;

        list.UnsafePerformLockedOperation((list, node1, node2, node3, value1, value2, value3), (list, state) =>
        {
            var (_, node1, node2, node3, value1, value2, value3) = state;

            var enumerator = list.GetNodeEnumerator();

            // Move to first node
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBeSameAs(node1);

            // Start a thread that tries to remove node2
            removeThread = new Thread(() =>
            {
                Volatile.Write(ref removeStarted, true);
                list.Remove(node2); // This should block until lock is released
                Volatile.Write(ref removeCompleted, true);
            });
            removeThread.Start();

            // Wait for thread to start attempting the remove
            while (!Volatile.Read(ref removeStarted))
                Thread.Sleep(1);
            Thread.Sleep(50); // Give it time to block on the lock

            // Remove should not have completed yet (blocked on lock)
            Volatile.Read(ref removeCompleted).ShouldBeFalse();
            node2.IsRemoved.ShouldBeFalse();

            // We should still be able to enumerate through node2
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBeSameAs(node2);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBeSameAs(node3);
            enumerator.MoveNext().ShouldBeFalse();

            // MovePrevious should also work through node2
            enumerator.MovePrevious().ShouldBeTrue();
            enumerator.Current.ShouldBeSameAs(node3);
            enumerator.MovePrevious().ShouldBeTrue();
            enumerator.Current.ShouldBeSameAs(node2);
            enumerator.MovePrevious().ShouldBeTrue();
            enumerator.Current.ShouldBeSameAs(node1);
            enumerator.MovePrevious().ShouldBeFalse();

            GC.KeepAlive(value1);
            GC.KeepAlive(value2);
            GC.KeepAlive(value3);
        });

        // After lock is released, the remove thread should complete
        removeThread!.Join();
        Volatile.Read(ref removeCompleted).ShouldBeTrue();
        node2.IsRemoved.ShouldBeTrue();
    }

    [TestMethod]
    public void EnumerateManyValues()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 100).Select((_) => new object()).ToList();

        foreach (object value in values)
            list.AddLast(value);

        // Forward enumeration
        var result = list.ToList();
        result.ShouldBe(values);

        // Reverse enumeration
        var reversed = list.GetEnumerator().AsEnumerable(reversed: true).ToList();
        values.Reverse();
        reversed.ShouldBe(values);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void RemoveMultipleConsecutiveNodesWhileEnumeratingForward()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 5).Select((_) => new object()).ToList();
        var nodes = values.Select(list.AddLast).ToList();

        var enumerator = list.GetNodeEnumerator();

        // Move to second node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(nodes[0]);
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(nodes[1]);

        // Remove middle three nodes (indices 1, 2, 3)
        list.Remove(nodes[1]);
        list.Remove(nodes[2]);
        list.Remove(nodes[3]);

        // Should skip to the last node
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(nodes[4]);
        enumerator.MoveNext().ShouldBeFalse();

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void RemoveMultipleConsecutiveNodesWhileEnumeratingBackward()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 5).Select((_) => new object()).ToList();
        var nodes = values.Select(list.AddLast).ToList();

        var enumerator = list.GetNodeEnumerator();

        // Move backwards to second-to-last node
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(nodes[4]);
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(nodes[3]);

        // Remove middle three nodes (indices 1, 2, 3)
        list.Remove(nodes[1]);
        list.Remove(nodes[2]);
        list.Remove(nodes[3]);

        // Should skip to the first node
        enumerator.MovePrevious().ShouldBeTrue();
        enumerator.Current.ShouldBeSameAs(nodes[0]);
        enumerator.MovePrevious().ShouldBeFalse();

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void GetEnumeratorWithNullStartNodeThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.GetEnumerator(null!));
    }

    [TestMethod]
    public void GetNodeEnumeratorWithNullStartNodeThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.GetNodeEnumerator(null!));
    }
}
