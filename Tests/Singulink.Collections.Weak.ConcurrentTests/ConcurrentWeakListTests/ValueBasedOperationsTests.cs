namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class ValueBasedOperationsTests
{
    [TestMethod]
    public void FindInEmptyList()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();

        list.Find((_) => true).ShouldBeNull();
        list.Contains(value).ShouldBeFalse();
    }

    [TestMethod]
    public void FindFirstItem()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node1 = list.AddLast(value1);
        list.AddLast(value2);
        list.AddLast(value3);

        var result = list.Find((x) => x == value1);

        result.ShouldNotBeNull();
        result.Value.Node.ShouldBeSameAs(node1);
        result.Value.Value.ShouldBeSameAs(value1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void FindMiddleItem()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        var result = list.Find((x) => x == value2);

        result.ShouldNotBeNull();
        result.Value.Node.ShouldBeSameAs(node2);
        result.Value.Value.ShouldBeSameAs(value2);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void FindLastItem()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        list.AddLast(value2);
        var node3 = list.AddLast(value3);

        var result = list.Find((x) => x == value3);

        result.ShouldNotBeNull();
        result.Value.Node.ShouldBeSameAs(node3);
        result.Value.Value.ShouldBeSameAs(value3);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void FindNonexistentItem()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object searchValue = new();
        list.AddLast(value1);
        list.AddLast(value2);

        list.Find((x) => x == searchValue).ShouldBeNull();
        list.Contains(searchValue).ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(searchValue);
    }

    [TestMethod]
    public void FindWithCustomPredicate()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "banana";
        string value3 = "cherry";
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        var result = list.Find((x) => x.StartsWith("b"));

        result.ShouldNotBeNull();
        result.Value.Node.ShouldBeSameAs(node2);
        result.Value.Value.ShouldBe("banana");

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void FindValueWithDefaultComparer()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "banana";
        string value3 = "cherry";
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        var result = list.Find("banana");

        result.ShouldNotBeNull();
        result.Value.Node.ShouldBeSameAs(node2);
        result.Value.Value.ShouldBe("banana");

        list.Contains(value1).ShouldBeTrue();
        list.Contains(value2).ShouldBeTrue();
        list.Contains(value3).ShouldBeTrue();
        list.Contains("grape").ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void FindValueWithCustomComparer()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "BANANA";
        string value3 = "cherry";
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        var result = list.Find("banana", StringComparer.OrdinalIgnoreCase);

        result.ShouldNotBeNull();
        result.Value.Node.ShouldBeSameAs(node2);
        result.Value.Value.ShouldBe("BANANA");

        list.Contains("banana", StringComparer.OrdinalIgnoreCase).ShouldBeTrue();
        list.Contains("APPLE", StringComparer.OrdinalIgnoreCase).ShouldBeTrue();
        list.Contains("grape", StringComparer.OrdinalIgnoreCase).ShouldBeFalse();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void FindDoesNotIncludeItemAddedDuringSearch()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        list.AddLast(value1);
        list.AddLast(value2);

        object addedDuringSearch = new();
        bool addedItem = false;

        // Use a predicate that adds an item mid-search
        var result = list.Find((x) =>
        {
            if (!addedItem)
            {
                list.AddLast(addedDuringSearch);
                addedItem = true;
            }

            return x == addedDuringSearch;
        });

        // Should not find the item added during search
        result.ShouldBeNull();

        // But the item should be in the list now
        list.Count.ShouldBe(3);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(addedDuringSearch);
    }

    [TestMethod]
    public void FindSkipsNodeRemovedDuringSearch()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        bool removedNode = false;

        // Remove node2 when we see value1, then look for value2
        var result = list.Find((x) =>
        {
            if (!removedNode)
            {
                list.Remove(node2);
                removedNode = true;
            }

            return x == value2;
        });

        // Should not find value2 since it was removed
        result.ShouldBeNull();

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void FindSkipsNodeRemovedAfterCurrentPosition()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        int callCount = 0;

        // When at value1, remove value2 (which is after current position)
        var result = list.Find((x) =>
        {
            callCount++;
            if (x == value1)
            {
                list.Remove(node2);
            }

            return x == value2;
        });

        // Should not find value2 since it was removed after current position
        result.ShouldBeNull();

        // We should have visited value1 and value3, but not value2
        callCount.ShouldBe(2);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void FindInLargeList()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 1000).Select((_) => new object()).ToList();
        var nodes = values.Select(list.AddLast).ToList();

        // Find item near the end
        object targetValue = values[950];
        var targetNode = nodes[950];

        var result = list.Find((x) => x == targetValue);

        result.ShouldNotBeNull();
        result.Value.Node.ShouldBeSameAs(targetNode);
        result.Value.Value.ShouldBeSameAs(targetValue);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void FindFirstMatchInListWithDuplicates()
    {
        var list = new ConcurrentWeakList<string>();
        string value = "duplicate";
        var node1 = list.AddLast(value);
        list.AddLast(value);
        list.AddLast(value);

        var result = list.Find(value);

        result.ShouldNotBeNull();
        result.Value.Node.ShouldBeSameAs(node1);
        result.Value.Value.ShouldBe(value);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void FindWithNullPredicateThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.Find((Predicate<object>)null!));
    }

    [TestMethod]
    public void RemoveValueFromEmptyList()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();

        list.Remove(value).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveExistingValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "banana";
        string value3 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);
        list.AddLast(value3);

        list.Remove(value2).ShouldBeTrue();

        list.Count.ShouldBe(2);
        list.ToList().ShouldBe([value1, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemoveFirstValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "banana";
        string value3 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);
        list.AddLast(value3);

        list.Remove(value1).ShouldBeTrue();

        list.Count.ShouldBe(2);
        list.ToList().ShouldBe([value2, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemoveLastValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "banana";
        string value3 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);
        list.AddLast(value3);

        list.Remove(value3).ShouldBeTrue();

        list.Count.ShouldBe(2);
        list.ToList().ShouldBe([value1, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemoveSingleValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value = "apple";
        list.AddLast(value);

        list.Remove(value).ShouldBeTrue();

        list.Count.ShouldBe(0);
        list.ToList().ShouldBeEmpty();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveNonexistentValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "banana";
        list.AddLast(value1);
        list.AddLast(value2);

        list.Remove("cherry").ShouldBeFalse();

        list.Count.ShouldBe(2);
        list.ToList().ShouldBe([value1, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void RemoveValueWithCustomComparer()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "BANANA";
        string value3 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);
        list.AddLast(value3);

        list.Remove("banana", StringComparer.OrdinalIgnoreCase).ShouldBeTrue();

        list.Count.ShouldBe(2);
        list.ToList().ShouldBe([value1, value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemoveFirstOccurrenceOfDuplicate()
    {
        var list = new ConcurrentWeakList<string>();
        string value = "duplicate";
        list.AddLast(value);
        var node2 = list.AddLast(value);
        list.AddLast(value);

        list.Remove(value).ShouldBeTrue();

        // Should remove first occurrence only
        list.Count.ShouldBe(2);

        // The second node should now be first
        list.UnsafeGetNodeAt(0).ShouldBeSameAs(node2);
        list.ToList().ShouldBe([value, value]);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveMultipleValuesOneByOne()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "banana";
        string value3 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);
        list.AddLast(value3);

        list.Remove(value2).ShouldBeTrue();
        list.Count.ShouldBe(2);

        list.Remove(value1).ShouldBeTrue();
        list.Count.ShouldBe(1);

        list.Remove(value3).ShouldBeTrue();
        list.Count.ShouldBe(0);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void RemoveValueTwiceReturnsFalseSecondTime()
    {
        var list = new ConcurrentWeakList<string>();
        string value = "apple";
        list.AddLast(value);

        list.Remove(value).ShouldBeTrue();
        list.Remove(value).ShouldBeFalse();

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryInsertBeforeInEmptyList()
    {
        var list = new ConcurrentWeakList<string>();

        var result = list.TryInsertBefore("existingValue", "newValue");

        result.ShouldBeNull();
        list.Count.ShouldBe(0);
    }

    [TestMethod]
    public void TryInsertBeforeExistingValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "banana";
        var result = list.TryInsertBefore(value2, newValue);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, newValue, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryInsertBeforeFirstValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "banana";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "apple";
        var result = list.TryInsertBefore(value1, newValue);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([newValue, value1, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryInsertBeforeNonexistentValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        var result = list.TryInsertBefore("banana", "newValue");

        result.ShouldBeNull();
        list.Count.ShouldBe(2);
        list.ToList().ShouldBe([value1, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void TryInsertBeforeWithCustomComparer()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "CHERRY";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "banana";
        var result = list.TryInsertBefore("cherry", newValue, StringComparer.OrdinalIgnoreCase);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, newValue, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryInsertBeforeFirstOccurrenceOfDuplicate()
    {
        var list = new ConcurrentWeakList<string>();
        string value = "duplicate";
        list.AddLast(value);
        list.AddLast(value);
        list.AddLast(value);

        string newValue = "inserted";
        var result = list.TryInsertBefore(value, newValue);

        result.ShouldNotBeNull();
        list.Count.ShouldBe(4);
        list.ToList().ShouldBe([newValue, value, value, value]);

        GC.KeepAlive(value);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void InsertBeforeExistingValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "banana";
        var result = list.InsertBefore(value2, newValue);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, newValue, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void InsertBeforeNonexistentValueThrows()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        Should.Throw<ArgumentException>(() => list.InsertBefore("banana", "newValue"));

        list.Count.ShouldBe(2);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void InsertBeforeInEmptyListThrows()
    {
        var list = new ConcurrentWeakList<string>();

        Should.Throw<ArgumentException>(() => list.InsertBefore("existingValue", "newValue"));
    }

    [TestMethod]
    public void InsertBeforeWithCustomComparer()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "CHERRY";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "banana";
        var result = list.InsertBefore("cherry", newValue, StringComparer.OrdinalIgnoreCase);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, newValue, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryInsertAfterInEmptyList()
    {
        var list = new ConcurrentWeakList<string>();

        var result = list.TryInsertAfter("existingValue", "newValue");

        result.ShouldBeNull();
        list.Count.ShouldBe(0);
    }

    [TestMethod]
    public void TryInsertAfterExistingValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "banana";
        var result = list.TryInsertAfter(value1, newValue);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, newValue, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryInsertAfterLastValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "banana";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "cherry";
        var result = list.TryInsertAfter(value2, newValue);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, value2, newValue]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryInsertAfterNonexistentValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        var result = list.TryInsertAfter("banana", "newValue");

        result.ShouldBeNull();
        list.Count.ShouldBe(2);
        list.ToList().ShouldBe([value1, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void TryInsertAfterWithCustomComparer()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "APPLE";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "banana";
        var result = list.TryInsertAfter("apple", newValue, StringComparer.OrdinalIgnoreCase);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, newValue, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryInsertAfterFirstOccurrenceOfDuplicate()
    {
        var list = new ConcurrentWeakList<string>();
        string value = "duplicate";
        list.AddLast(value);
        list.AddLast(value);
        list.AddLast(value);

        string newValue = "inserted";
        var result = list.TryInsertAfter(value, newValue);

        result.ShouldNotBeNull();
        list.Count.ShouldBe(4);
        list.ToList().ShouldBe([value, newValue, value, value]);

        GC.KeepAlive(value);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void InsertAfterExistingValue()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "banana";
        var result = list.InsertAfter(value1, newValue);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, newValue, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void InsertAfterNonexistentValueThrows()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        Should.Throw<ArgumentException>(() => list.InsertAfter("banana", "newValue"));

        list.Count.ShouldBe(2);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
    }

    [TestMethod]
    public void InsertAfterInEmptyListThrows()
    {
        var list = new ConcurrentWeakList<string>();

        Should.Throw<ArgumentException>(() => list.InsertAfter("existingValue", "newValue"));
    }

    [TestMethod]
    public void InsertAfterWithCustomComparer()
    {
        var list = new ConcurrentWeakList<string>();
        string value1 = "APPLE";
        string value2 = "cherry";
        list.AddLast(value1);
        list.AddLast(value2);

        string newValue = "banana";
        var result = list.InsertAfter("apple", newValue, StringComparer.OrdinalIgnoreCase);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(newValue);
        list.Count.ShouldBe(3);
        list.ToList().ShouldBe([value1, newValue, value2]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryInsertBeforeNullValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.TryInsertBefore(value, null!));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryInsertAfterNullValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.TryInsertAfter(value, null!));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void InsertBeforeNullValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.InsertBefore(value, null!));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void InsertAfterNullValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.InsertAfter(value, null!));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryInsertBeforeNullExistingValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.TryInsertBefore(null!, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void TryInsertAfterNullExistingValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.TryInsertAfter(null!, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void InsertBeforeNullExistingValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.InsertBefore(null!, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void InsertAfterNullExistingValueThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        Should.Throw<ArgumentNullException>(() => list.InsertAfter(null!, value));

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void RemoveNullValueThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.Remove((object)null!));
    }

    [TestMethod]
    public void FindNullValueThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.Find((object)null!));
    }

    [TestMethod]
    public void ContainsNullValueThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.Contains(null!));
    }

    [TestMethod]
    public void OperationsHandleItemRemovedDuringComparison()
    {
        // This test uses a custom comparer that removes the target node just before it would be found,
        // simulating a concurrent removal. All operations should handle this gracefully.

        var list = new ConcurrentWeakList<string>();
        string value1 = "apple";
        string value2 = "banana";
        string value3 = "cherry";
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        var node3 = list.AddLast(value3);

        IEqualityComparer<string> CreateRemovingComparer() => new DelegateEqualityComparer<string>(
            (x, y) =>
            {
                bool wouldBeEqual = x == y;
                if (wouldBeEqual && node2 is not null)
                {
                    list.Remove(node2);
                    node2 = null;
                }

                return wouldBeEqual;
            },
            EqualityComparer<string>.Default.GetHashCode);

        // Find: should not find the item because it was removed just before the comparison returned true
        list.Find("banana", CreateRemovingComparer()).ShouldBeNull();
        node2 = list.AddBefore(node3, value2);

        // Contains: should not find the item because it was removed just before the comparison returned true
        list.Contains("banana", CreateRemovingComparer()).ShouldBeFalse();
        node2 = list.AddBefore(node3, value2);

        // Remove: should return false because the node was removed before it could be removed by the operation
        list.Remove("banana", CreateRemovingComparer()).ShouldBeFalse();
        node2 = list.AddBefore(node3, value2);

        // TryInsertBefore: should fail to insert because the target node was removed during comparison
        list.TryInsertBefore("banana", "inserted", CreateRemovingComparer()).ShouldBeNull();
        node2 = list.AddBefore(node3, value2);

        // TryInsertAfter: should fail to insert because the target node was removed during comparison
        list.TryInsertAfter("banana", "inserted", CreateRemovingComparer()).ShouldBeNull();
        node2 = list.AddBefore(node3, value2);

        // InsertBefore: should throw because the target node was removed during comparison
        Should.Throw<ArgumentException>(() => list.InsertBefore("banana", "inserted", CreateRemovingComparer()));
        node2 = list.AddBefore(node3, value2);

        // InsertAfter: should throw because the target node was removed during comparison
        Should.Throw<ArgumentException>(() => list.InsertAfter("banana", "inserted", CreateRemovingComparer()));

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    private sealed class DelegateEqualityComparer<T>(Func<T?, T?, bool> equals, Func<T, int> getHashCode) : IEqualityComparer<T>
    {
        public bool Equals(T? x, T? y) => equals(x, y);
        public int GetHashCode(T obj) => getHashCode(obj);
    }
}
