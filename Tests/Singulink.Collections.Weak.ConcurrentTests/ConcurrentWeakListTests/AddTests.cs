namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class AddTests
{
    [TestMethod]
    public void AddFirstToEmptyList()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();

        var node = list.AddFirst(value);

        list.Count.ShouldBe(1);
        node.Value.ShouldBeSameAs(value);
        list.ToList().ShouldBe([value]);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddFirstToSingleItemList()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        list.AddLast(value1);

        var node = list.AddFirst(value2);

        list.Count.ShouldBe(2);
        node.Value.ShouldBeSameAs(value2);
        list.ToList().ShouldBe([value2, value1]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddFirstMultipleTimes()
    {
        var list = new ConcurrentWeakList<object>();
        object[] values = [new object(), new object(), new object()];

        foreach (object v in values)
            list.AddFirst(v);

        list.Count.ShouldBe(3);

        // AddFirst adds to the front, so order is reversed
        list.ToList().ShouldBe([values[2], values[1], values[0]]);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void AddLastToEmptyList()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();

        var node = list.AddLast(value);

        list.Count.ShouldBe(1);
        node.Value.ShouldBeSameAs(value);
        list.ToList().ShouldBe([value]);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddLastToSingleItemList()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        list.AddLast(value1);

        var node = list.AddLast(value2);

        list.Count.ShouldBe(2);
        node.Value.ShouldBeSameAs(value2);
        list.ToList().ShouldBe([value1, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddLastMultipleTimes()
    {
        var list = new ConcurrentWeakList<object>();
        object[] values = [new object(), new object(), new object()];

        foreach (object v in values)
            list.AddLast(v);

        list.Count.ShouldBe(3);
        list.ToList().ShouldBe(values);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void InsertAtZeroInEmptyList()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();

        var node = list.UnsafeInsertAt(value, 0);

        list.Count.ShouldBe(1);
        node.Value.ShouldBeSameAs(value);
        list.ToList().ShouldBe([value]);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void InsertAtZeroWithExistingItems()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        list.AddLast(value1);

        var node = list.UnsafeInsertAt(value2, 0);

        list.Count.ShouldBe(2);
        node.Value.ShouldBeSameAs(value2);
        list.ToList().ShouldBe([value2, value1]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void InsertAtEnd()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        list.AddLast(value1);

        var node = list.UnsafeInsertAt(value2, 1);

        list.Count.ShouldBe(2);
        node.Value.ShouldBeSameAs(value2);
        list.ToList().ShouldBe([value1, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void InsertAtMiddle()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        list.AddLast(value3);

        var node = list.UnsafeInsertAt(value2, 1);

        list.Count.ShouldBe(3);
        node.Value.ShouldBeSameAs(value2);
        list.ToList().ShouldBe([value1, value2, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void InsertAtVariousIndices()
    {
        var list = new ConcurrentWeakList<object>();
        object[] values = [new object(), new object(), new object(), new object(), new object()];

        // Build list: [0], [1, 0], [1, 2, 0], [1, 2, 3, 0], [1, 4, 2, 3, 0]
        list.AddLast(values[0]);
        list.UnsafeInsertAt(values[1], 0);
        list.UnsafeInsertAt(values[2], 1);
        list.UnsafeInsertAt(values[3], 2);
        list.UnsafeInsertAt(values[4], 1);

        list.Count.ShouldBe(5);
        list.ToList().ShouldBe([values[1], values[4], values[2], values[3], values[0]]);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void InsertAtNegativeIndexThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();

        Should.Throw<ArgumentOutOfRangeException>(() => list.UnsafeInsertAt(value, -1));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void InsertAtOutOfBoundsIndexThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(new object());

        Should.Throw<ArgumentOutOfRangeException>(() => list.UnsafeInsertAt(value, 2));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddFirstNullThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.AddFirst(null!));
    }

    [TestMethod]
    public void AddLastNullThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.AddLast(null!));
    }

    [TestMethod]
    public void InsertAtNullThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.UnsafeInsertAt(null!, 0));
    }

    [TestMethod]
    public void AddBeforeNullValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.AddBefore(node, null!));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddAfterNullValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        var node = list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.AddAfter(node, null!));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddBeforeNullNodeThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.AddBefore(null!, new object()));
    }

    [TestMethod]
    public void AddAfterNullNodeThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.AddAfter(null!, new object()));
    }

    [TestMethod]
    public void AddFirstOnDisposedListThrows()
    {
        var list = new ConcurrentWeakList<object>();
        list.Dispose();

        Should.Throw<ObjectDisposedException>(() => list.AddFirst(new object()));
    }

    [TestMethod]
    public void AddLastOnDisposedListThrows()
    {
        var list = new ConcurrentWeakList<object>();
        list.Dispose();

        Should.Throw<ObjectDisposedException>(() => list.AddLast(new object()));
    }

    [TestMethod]
    public void InsertAtOnDisposedListThrows()
    {
        var list = new ConcurrentWeakList<object>();
        list.Dispose();

        Should.Throw<ObjectDisposedException>(() => list.UnsafeInsertAt(new object(), 0));
    }

    [TestMethod]
    public void AddBeforeNodeFromDifferentListThrows()
    {
        var list1 = new ConcurrentWeakList<object>();
        var list2 = new ConcurrentWeakList<object>();
        object value = new();
        var node = list1.AddLast(value);

        Should.Throw<InvalidOperationException>(() => list2.AddBefore(node, new object()));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddAfterNodeFromDifferentListThrows()
    {
        var list1 = new ConcurrentWeakList<object>();
        var list2 = new ConcurrentWeakList<object>();
        object value = new();
        var node = list1.AddLast(value);

        Should.Throw<InvalidOperationException>(() => list2.AddAfter(node, new object()));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddBeforeFirstNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        var node1 = list.AddLast(value1);

        var node2 = list.AddBefore(node1, value2);

        list.Count.ShouldBe(2);
        node2.Value.ShouldBeSameAs(value2);
        list.ToList().ShouldBe([value2, value1]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddBeforeMiddleNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);

        var node3 = list.AddBefore(node2, value3);

        list.Count.ShouldBe(3);
        node3.Value.ShouldBeSameAs(value3);
        list.ToList().ShouldBe([value1, value3, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AddBeforeLastNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);

        list.AddBefore(node2, value3);

        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, value3, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AddBeforeRemovedNodeAllowed()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        list.Remove(node2);

        // First AddBefore on removed node should go into the gap (position 1)
        object newValue1 = new();
        var newNode1 = list.AddBefore(node2, newValue1);

        newNode1.ShouldNotBeNull();
        list.Count.ShouldBe(3);
        list.UnsafeGetIndexOfNode(newNode1).ShouldBe((nint)1);
        list.ToList().ShouldBe([value1, newValue1, value3]);

        // Additional calls should still succeed (position not guaranteed after first, since base node is removed)
        object newValue2 = new();
        var newNode2 = list.AddBefore(node2, newValue2);
        newNode2.ShouldNotBeNull();
        list.Count.ShouldBe(4);

        object newValue3 = new();
        var newNode3 = list.AddBefore(node2, newValue3);
        newNode3.ShouldNotBeNull();
        list.Count.ShouldBe(5);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
        GC.KeepAlive(newValue1);
        GC.KeepAlive(newValue2);
        GC.KeepAlive(newValue3);
    }

    [TestMethod]
    public void AddBeforeRemovedNodeDisallowed()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);

        list.Remove(node2);

        // AddBefore on removed node with allowBeforeRemovedNode=false should return null
        object newValue = new();
        var newNode = list.AddBefore(node2, newValue, allowBeforeRemovedNode: false);

        newNode.ShouldBeNull();
        list.Count.ShouldBe(1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void AddAfterFirstNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        var node1 = list.AddLast(value1);

        var node2 = list.AddAfter(node1, value2);

        list.Count.ShouldBe(2);
        node2.Value.ShouldBeSameAs(value2);
        list.ToList().ShouldBe([value1, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void AddAfterMiddleNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        list.AddLast(value3);

        var node2 = list.AddAfter(node1, value2);

        list.Count.ShouldBe(3);
        node2.Value.ShouldBeSameAs(value2);
        list.ToList().ShouldBe([value1, value2, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AddAfterLastNode()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);

        list.AddAfter(node2, value3);

        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, value2, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void AddAfterRemovedNodeAllowed()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        list.Remove(node2);

        // First AddAfter on removed node should go into the gap (position 1)
        object newValue1 = new();
        var newNode1 = list.AddAfter(node2, newValue1);

        newNode1.ShouldNotBeNull();
        list.Count.ShouldBe(3);
        list.UnsafeGetIndexOfNode(newNode1).ShouldBe((nint)1);
        list.ToList().ShouldBe([value1, newValue1, value3]);

        // Additional calls should still succeed (position not guaranteed after first, since base node is removed)
        object newValue2 = new();
        var newNode2 = list.AddAfter(node2, newValue2);
        newNode2.ShouldNotBeNull();
        list.Count.ShouldBe(4);

        object newValue3 = new();
        var newNode3 = list.AddAfter(node2, newValue3);
        newNode3.ShouldNotBeNull();
        list.Count.ShouldBe(5);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
        GC.KeepAlive(newValue1);
        GC.KeepAlive(newValue2);
        GC.KeepAlive(newValue3);
    }

    [TestMethod]
    public void AddAfterRemovedNodeDisallowed()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);

        list.Remove(node2);

        // AddAfter on removed node with allowAfterRemovedNode=false should return null
        object newValue = new();
        var newNode = list.AddAfter(node2, newValue, allowAfterRemovedNode: false);

        newNode.ShouldBeNull();
        list.Count.ShouldBe(1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void CountUpdatesCorrectlyWithMixedOperations()
    {
        var list = new ConcurrentWeakList<object>();
        list.Count.ShouldBe(0);

        object value1 = new();
        object value2 = new();
        object value3 = new();
        object value4 = new();
        object value5 = new();

        list.AddFirst(value1);
        list.Count.ShouldBe(1);

        list.AddLast(value2);
        list.Count.ShouldBe(2);

        var node = list.UnsafeInsertAt(value3, 1);
        list.Count.ShouldBe(3);

        list.AddBefore(node, value4);
        list.Count.ShouldBe(4);

        list.AddAfter(node, value5);
        list.Count.ShouldBe(5);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
        GC.KeepAlive(value4);
        GC.KeepAlive(value5);
    }

    [TestMethod]
    public void AddManyItemsInOrder()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 100).Select((_) => new object()).ToList();

        foreach (object v in values)
            list.AddLast(v);

        list.Count.ShouldBe(100);
        list.ToList().ShouldBe(values);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void AddManyItemsInReverseOrder()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 100).Select((_) => new object()).ToList();

        foreach (object v in values)
            list.AddFirst(v);

        list.Count.ShouldBe(100);
        var expected = values.ToList();
        expected.Reverse();
        list.ToList().ShouldBe(expected);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void ComplexInsertionPattern_AlternatingEnds()
    {
        // This pattern forces multiple rotations as we alternate adding to front and back
        var list = new ConcurrentWeakList<object>();
        var values = new List<object>();
        var expectedOrder = new List<object>();

        for (int i = 0; i < 50; i++)
        {
            object frontValue = new();
            object backValue = new();
            values.Add(frontValue);
            values.Add(backValue);

            list.AddFirst(frontValue);
            list.AddLast(backValue);

            expectedOrder.Insert(0, frontValue);
            expectedOrder.Add(backValue);
        }

        list.Count.ShouldBe(100);
        list.ToList().ShouldBe(expectedOrder);

        // Verify nodes return correct values at each position
        for (int i = 0; i < list.Count; i++)
        {
            var node = list.UnsafeGetNodeAt(i);
            node.Value.ShouldBeSameAs(expectedOrder[i]);
        }

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void ComplexInsertionPattern_MiddleInsertions()
    {
        // Build a list then insert many items in the middle to trigger rebalancing
        var list = new ConcurrentWeakList<object>();
        var expectedOrder = new List<object>();

        // First, add 20 items
        for (int i = 0; i < 20; i++)
        {
            object value = new();
            expectedOrder.Add(value);
            list.AddLast(value);
        }

        // Now insert 30 items at various middle positions
        for (int i = 0; i < 30; i++)
        {
            object value = new();
            int insertPos = (i % (expectedOrder.Count - 1)) + 1; // Always insert somewhere in the middle
            expectedOrder.Insert(insertPos, value);
            list.UnsafeInsertAt(value, insertPos);
        }

        list.Count.ShouldBe(50);
        list.ToList().ShouldBe(expectedOrder);

        // Verify nodes return correct values at each position
        for (int i = 0; i < list.Count; i++)
        {
            var node = list.UnsafeGetNodeAt(i);
            node.Value.ShouldBeSameAs(expectedOrder[i]);
            node.IsRemoved.ShouldBeFalse();
        }

        GC.KeepAlive(expectedOrder);
    }

    [TestMethod]
    public void ComplexInsertionPattern_AddBeforeAndAfterChain()
    {
        // Create a chain using AddBefore and AddAfter repeatedly
        var list = new ConcurrentWeakList<object>();
        object anchor = new();
        var anchorNode = list.AddLast(anchor);
        var expectedOrder = new List<object> { anchor };
        int anchorIndex = 0;

        // Alternate adding before and after the anchor
        for (int i = 0; i < 40; i++)
        {
            object value = new();

            if (i % 2 == 0)
            {
                list.AddBefore(anchorNode, value);
                expectedOrder.Insert(anchorIndex, value);
                anchorIndex++;
            }
            else
            {
                list.AddAfter(anchorNode, value);
                expectedOrder.Insert(anchorIndex + 1, value);
            }
        }

        list.Count.ShouldBe(41);
        list.ToList().ShouldBe(expectedOrder);

        // Verify nodes return correct values at each position
        for (int i = 0; i < list.Count; i++)
        {
            var node = list.UnsafeGetNodeAt(i);
            node.Value.ShouldBeSameAs(expectedOrder[i]);
            node.IsRemoved.ShouldBeFalse();
        }

        // Verify anchor is still at the expected position
        list.UnsafeGetIndexOfNode(anchorNode).ShouldBe((nint)anchorIndex);

        GC.KeepAlive(expectedOrder);
    }

    [TestMethod]
    public void ComplexInsertionAndRemovalPattern()
    {
        // Insert and remove in a pattern that exercises tree rebalancing
        var list = new ConcurrentWeakList<object>();
        var values = new List<object>();
        var nodes = new List<ConcurrentWeakList<object>.Node>();

        // Add 30 items
        for (int i = 0; i < 30; i++)
        {
            object value = new();
            values.Add(value);
            nodes.Add(list.AddLast(value));
        }

        // Remove every 3rd node
        for (int i = 0; i < nodes.Count; i += 3)
        {
            list.Remove(nodes[i]);
        }

        // Insert new items at various positions
        for (int i = 0; i < 10; i++)
        {
            object value = new();
            values.Add(value);
            list.UnsafeInsertAt(value, (i * 2) % list.Count);
        }

        // Verify remaining structure - nodes match ToList output
        var listContents = list.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            var node = list.UnsafeGetNodeAt(i);
            node.Value.ShouldBeSameAs(listContents[i]);
            node.IsRemoved.ShouldBeFalse();
        }

        GC.KeepAlive(values);
    }
}
