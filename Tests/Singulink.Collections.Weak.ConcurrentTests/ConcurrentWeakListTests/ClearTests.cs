namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class ClearTests
{
    [TestMethod]
    public void ClearEmptyList()
    {
        var list = new ConcurrentWeakList<object>();
        list.Clear();
        list.Count.ShouldBe(0);
    }

    [TestMethod]
    public void ClearOneValue()
    {
        var list = new ConcurrentWeakList<object>();
        object value = new();
        list.AddLast(value);

        list.Clear();
        list.Count.ShouldBe(0);

        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ClearTwoValues()
    {
        var list = new ConcurrentWeakList<object>();
        object[] values = [new object(), new object()];
        foreach (object v in values)
            list.AddLast(v);

        list.Clear();
        list.Count.ShouldBe(0);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void ClearThreeValues()
    {
        var list = new ConcurrentWeakList<object>();
        object[] values = [new object(), new object(), new object()];
        foreach (object v in values)
            list.AddLast(v);

        list.Clear();
        list.Count.ShouldBe(0);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void ClearManyValues()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 100).Select((_) => new object()).ToList();
        foreach (object v in values)
            list.AddLast(v);

        list.Clear();
        list.Count.ShouldBe(0);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void ClearAndDisposeSimultaneously()
    {
        // Run multiple times as timing is non-deterministic
        for (int i = 0; i < 100; i++)
        {
            var list = new ConcurrentWeakList<object>();
            var values = Enumerable.Range(0, 50).Select((_) => new object()).ToList();
            var nodes = values.Select(list.AddLast).ToList();

            // Start Dispose in a separate thread, then immediately call Clear
            // This biases toward Clear starting first (or very close to Dispose)
            var disposeThread = new Thread(list.Dispose);
            disposeThread.Start();
            list.Clear();

            disposeThread.Join();

            // After both complete, all nodes should be removed
            foreach (var node in nodes)
                node.IsRemoved.ShouldBeTrue();

            GC.KeepAlive(values);
        }
    }

    [TestMethod]
    public void ClearWhileAddingValues()
    {
        // Run multiple times as timing is non-deterministic (it fails on 1 run about 1/3 of the time on a high core computer, so run 1000 times to be certain)
        for (int i = 0; i < 1000; i++)
        {
            var list = new ConcurrentWeakList<object>();
            var initialValues = Enumerable.Range(0, 2000).Select((_) => new object()).ToList();
            foreach (object v in initialValues)
                list.AddLast(v);

            var newValues = Enumerable.Range(0, 2000).Select((_) => new object()).ToList();
            var addedNodes = new List<ConcurrentWeakList<object>.Node>();

            // Start adding values in a separate thread
            var addThread = new Thread(() =>
            {
                foreach (object v in newValues)
                    addedNodes.Add(list.AddLast(v));
            });
            addThread.Start();

            // Clear immediately after starting the add thread
            list.Clear();

            addThread.Join();

            // We should have ended up with some of the newly added nodes in the list:
            bool success = false;
            foreach (var node in addedNodes)
            {
                if (!node.IsRemoved)
                {
                    success = true;
                }
            }

            GC.KeepAlive(initialValues);
            GC.KeepAlive(newValues);
            if (success) return;
        }

#pragma warning disable RS0030 // Do not use banned APIs
        // If we have less than 4 cores, return an inconclusive result instead:
        if (Environment.ProcessorCount < 4) Assert.Inconclusive("Not enough cores for ClearWhileAddingValues to be reliable.");

        // If we get here, no added nodes survived the Clear in any of the attempts
        Assert.Fail("No added nodes survived the Clear in any of the attempts.");
#pragma warning restore RS0030 // Do not use banned APIs
    }

    [TestMethod]
    public void ClearMarksNodesAsRemoved()
    {
        var list = new ConcurrentWeakList<object>();
        object[] values = [new object(), new object(), new object()];
        var nodes = values.Select(list.AddLast).ToArray();

        list.Clear();

        foreach (var node in nodes)
            node.IsRemoved.ShouldBeTrue();

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void ClearTwice()
    {
        var list = new ConcurrentWeakList<object>();
        var values = Enumerable.Range(0, 10).Select((_) => new object()).ToList();
        foreach (object v in values)
            list.AddLast(v);

        list.Clear();
        list.Count.ShouldBe(0);

        list.Clear(); // Should be safe to call again
        list.Count.ShouldBe(0);

        GC.KeepAlive(values);
    }

    [TestMethod]
    public void ClearThenAddNewValues()
    {
        var list = new ConcurrentWeakList<object>();
        var oldValues = Enumerable.Range(0, 10).Select((_) => new object()).ToList();
        foreach (object v in oldValues)
            list.AddLast(v);

        list.Clear();
        list.Count.ShouldBe(0);

        var newValues = Enumerable.Range(0, 5).Select((_) => new object()).ToList();
        foreach (object v in newValues)
            list.AddLast(v);

        list.Count.ShouldBe(5);
        list.ToList().ShouldBe(newValues);

        GC.KeepAlive(oldValues);
        GC.KeepAlive(newValues);
    }
}
