namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class FuzzTests
{
    public static IEnumerable<object[]> BasicFuzzTestData => Enumerable.Range(0, 20).Select((x) => new object[] { x });

    [DynamicData(nameof(BasicFuzzTestData))]
    [TestMethod]
    public void BasicFuzzTest(int seed)
    {
        // In this, we just make an actual list, perform random operations on both it and a ConcurrentWeakList,
        // and ensure they stay the same (we keep all references alive).
        Random r = new(seed);
        ConcurrentWeakList<object> weakList = new();
        List<object> actualList = [];
        List<ConcurrentWeakList<object>.Node> nodes = [];
        const int Operations = 10000;
        const int MaxValues = 50;

        // Do random operations out of AddFirst, AddLast, AddBefore, AddAfter, Remove, InsertAt - we weight the add & remove groups equally, but if there's
        // only 1 valid option, we always do that.
        for (int i = 0; i < Operations; i++)
        {
            // Determine whether to add or remove:
            bool doAdd;
            if (actualList.Count == MaxValues) doAdd = false;
            else if (actualList.Count == 0) doAdd = true;
            else doAdd = r.Next(2) == 0;

            // Perform the operation:
            if (doAdd)
            {
                object newValue = new();
                int idx;
                ConcurrentWeakList<object>.Node newNode;
                switch (actualList.Count == 0 ? r.Next(3) : r.Next(5))
                {
                    case 0:
                        newNode = weakList.AddFirst(newValue);
                        actualList.Insert(0, newValue);
                        nodes.Insert(0, newNode);
                        break;
                    case 1:
                        newNode = weakList.AddLast(newValue);
                        actualList.Add(newValue);
                        nodes.Add(newNode);
                        break;
                    case 2:
                        idx = r.Next(actualList.Count + 1);
                        newNode = weakList.UnsafeInsertAt(newValue, idx);
                        actualList.Insert(idx, newValue);
                        nodes.Insert(idx, newNode);
                        break;
                    case 3:
                        idx = r.Next(actualList.Count);
                        var refNode = nodes[idx];
                        newNode = weakList.AddBefore(refNode, newValue);
                        actualList.Insert(idx, newValue);
                        nodes.Insert(idx, newNode);
                        break;
                    case 4:
                        idx = r.Next(actualList.Count);
                        refNode = nodes[idx];
                        newNode = weakList.AddAfter(refNode, newValue);
                        actualList.Insert(idx + 1, newValue);
                        nodes.Insert(idx + 1, newNode);
                        break;
                }
            }
            else
            {
                int idx = r.Next(actualList.Count);
                object oldValue = actualList[idx];
                actualList.RemoveAt(idx);
                var oldNode = nodes[idx];
                nodes.RemoveAt(idx);
                weakList.Remove(oldNode);
                GC.KeepAlive(oldValue);
            }

            // Check they're the same:
            weakList.Count.ShouldBe(actualList.Count);
            weakList.ToList().ShouldBe(actualList);
            weakList.GetNodeEnumerator().AsEnumerable().ShouldBe(nodes);

            // Check all the indices match up:
            for (int j = 0; j < nodes.Count; j++)
            {
                var node = nodes[j];
                weakList.UnsafeGetIndexOfNode(node).ShouldBe(j); // This also checks GetNodeAtImpl in debug mode, since GetIndexOfNode checks it.
            }
        }

        // Keep values that are still in the list alive:
        GC.KeepAlive(actualList);
    }

    // It's not worth making a new file just for these 2 - here is good enough:

    [TestMethod]
    public void UnsafePerformLockedOperationWithNullOperationThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.UnsafePerformLockedOperation(0, null!));
    }

    [TestMethod]
    public void UnsafeTryPerformLockedOperationWithNullOperationThrows()
    {
        var list = new ConcurrentWeakList<object>();

        Should.Throw<ArgumentNullException>(() => list.UnsafeTryPerformLockedOperation(0, null!));
    }
}
