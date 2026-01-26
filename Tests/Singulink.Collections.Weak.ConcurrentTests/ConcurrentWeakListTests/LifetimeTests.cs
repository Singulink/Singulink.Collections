namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class LifetimeTests
{
    [TestMethod]
    public void ValueKeepsNodeInList()
    {
        ConcurrentWeakList<object> list = new();

        var (node, value) = Helpers.NotInlined(list, (list) =>
        {
            object value = new();
            var node = list.AddLast(value);
            return (node, value);
        });

        Helpers.ForceGC();

        node.IsRemoved.ShouldBeFalse();
        node.Value.ShouldBeSameAs(value);

        GC.KeepAlive(value);
        GC.KeepAlive(list);
    }

    [TestMethod]
    public void ValueDiesWhenOnlyNodeInList()
    {
        ConcurrentWeakList<object> list = new();

        var (valueRef, node, internalNodeHelperWeakRef) = Helpers.NotInlined(list, (list) =>
        {
            object value = new();
            var node = list.AddLast(value);
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(node));
            GC.KeepAlive(value);
            return (new WeakReference<object>(value), node, internalNodeHelperWeakRef);
        });

        Helpers.ForceGC();

        valueRef.TryGetTarget(out _).ShouldBeFalse();
        node.IsRemoved.ShouldBeTrue();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(list);
    }

    [TestMethod]
    public void NodeDoesNotKeepValueAlive()
    {
        ConcurrentWeakList<object> list = new();

        var (node, valueRef, internalNodeHelperWeakRef) = Helpers.NotInlined(list, (list) =>
        {
            object value = new();
            var node = list.AddLast(value);
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(node));
            GC.KeepAlive(value);
            return (node, new WeakReference<object>(value), internalNodeHelperWeakRef);
        });

        Helpers.ForceGC();

        node.IsRemoved.ShouldBeTrue();
        valueRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(node);
        GC.KeepAlive(list);
    }

    [TestMethod]
    public void ValueReferencingListDoesNotLeak()
    {
        var (listWeakRef, nodeWeakRef, internalNodeWeakRef, internalNodeHelperWeakRef) = Helpers.NotInlined(() =>
        {
            var list = new ConcurrentWeakList<object>();
            List<object> value = [list]; // Add as many things as possible to try to force a leak if there is one:
            var node = list.AddLast(value);
            value.Add(node);
            value.Add(value);
            var internalNodeWeakRef = new WeakReference<object?>(Helpers.GetInternalNode(node));
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(node));
            GC.KeepAlive(value);
            return (new WeakReference<object>(list), new WeakReference<object?>(node), internalNodeWeakRef, internalNodeHelperWeakRef);
        });

        Helpers.ForceGC();

        listWeakRef.TryGetTarget(out _).ShouldBeFalse();
        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();
    }

    [TestMethod]
    public void AliveValueInListDoesNotLeakUnreferencedList()
    {
        var (listWeakRef, nodeWeakRef, internalNodeWeakRef, internalNodeHelperWeakRef, o) = Helpers.NotInlined(() =>
        {
            var list = new ConcurrentWeakList<object>();
            object value = new();
            var node = list.AddLast(value);
            var internalNodeWeakRef = new WeakReference<object?>(Helpers.GetInternalNode(node));
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(node));
            list.Clear();
            GC.KeepAlive(value);
            return (new WeakReference<object>(list), new WeakReference<object?>(node), internalNodeWeakRef, internalNodeHelperWeakRef, value);
        });

        Helpers.ForceGC();

        listWeakRef.TryGetTarget(out _).ShouldBeFalse();
        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(o);
    }

    [TestMethod]
    public void AliveValueInClearedListDoesNotLeakUnreferencedList()
    {
        var (listWeakRef, nodeWeakRef, internalNodeWeakRef, internalNodeHelperWeakRef, o) = Helpers.NotInlined(() =>
        {
            var list = new ConcurrentWeakList<object>();
            object value = new();
            var node = list.AddLast(value);
            var internalNodeWeakRef = new WeakReference<object?>(Helpers.GetInternalNode(node));
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(node));
            list.Clear();
            GC.KeepAlive(value);
            return (new WeakReference<object>(list), new WeakReference<object?>(node), internalNodeWeakRef, internalNodeHelperWeakRef, value);
        });

        Helpers.ForceGC();

        listWeakRef.TryGetTarget(out _).ShouldBeFalse();
        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(o);
    }

    [TestMethod]
    public void AliveValueInDisposedListDoesNotLeakUnreferencedList()
    {
        var (listWeakRef, nodeWeakRef, internalNodeWeakRef, internalNodeHelperWeakRef, o) = Helpers.NotInlined(() =>
        {
            var list = new ConcurrentWeakList<object>();
            object value = new();
            var node = list.AddLast(value);
            var internalNodeWeakRef = new WeakReference<object?>(Helpers.GetInternalNode(node));
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(node));
            list.Dispose();
            GC.KeepAlive(value);
            return (new WeakReference<object>(list), new WeakReference<object?>(node), internalNodeWeakRef, internalNodeHelperWeakRef, value);
        });

        Helpers.ForceGC();

        listWeakRef.TryGetTarget(out _).ShouldBeFalse();
        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(o);
    }

    [TestMethod]
    public void ValueKeepsNodeAlive()
    {
        ConcurrentWeakList<object> list = new();

        var (node, value) = Helpers.NotInlined(list, (list) =>
        {
            object value = new();
            var node = list.AddLast(value);
            return (new WeakReference<ConcurrentWeakList<object>.Node>(node), value);
        });

        Helpers.ForceGC();

        node.TryGetTarget(out var actualNode).ShouldBeTrue();
        actualNode.IsRemoved.ShouldBeFalse();

        GC.KeepAlive(value);
        GC.KeepAlive(list);
    }

    [TestMethod]
    public void ClearAllowsNodeToDie()
    {
        ConcurrentWeakList<object> list = new();

        var nodeWeakRef = Helpers.NotInlined(list, (list) =>
        {
            object value = new();
            var node = list.AddLast(value);
            list.Clear();
            return new WeakReference<ConcurrentWeakList<object>.Node>(node);
        });

        Helpers.ForceGC();

        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(list);
    }

    [TestMethod]
    public void DisposeAllowsNodeToDie()
    {
        ConcurrentWeakList<object> list = new();

        var nodeWeakRef = Helpers.NotInlined(list, (list) =>
        {
            object value = new();
            var node = list.AddLast(value);
            list.Dispose();
            return new WeakReference<ConcurrentWeakList<object>.Node>(node);
        });

        Helpers.ForceGC();

        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(list);
    }

    [TestMethod]
    public void NodeKeepsListAlive()
    {
        var (listWeak, node) = Helpers.NotInlined(() =>
        {
            ConcurrentWeakList<object> list = new();
            object value = new();
            var node = list.AddLast(value);
            return (new WeakReference<ConcurrentWeakList<object>>(list), node);
        });

        Helpers.ForceGC();

        node.IsRemoved.ShouldBeTrue();
        listWeak.TryGetTarget(out var list).ShouldBeTrue();
        list.AddLast(new object()); // Verify list is still usable

        GC.KeepAlive(node);
    }

    [TestMethod]
    public void ValueKeepsKeptNodeAliveOnly()
    {
        ConcurrentWeakList<object> list = new();

        var (node1, node1InternalNode, node1InternalNodeHelper, node2, value) = Helpers.NotInlined(list, (list) =>
        {
            object value = new();
            var node1 = list.AddLast(value);
            var node2 = list.AddLast(value);
            var node1InternalNode = new WeakReference<object?>(Helpers.GetInternalNode(node1));
            var node1InternalNodeHelper = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(node1));
            node1.Dispose();
            return (
                new WeakReference<ConcurrentWeakList<object>.Node>(node1),
                node1InternalNode,
                node1InternalNodeHelper,
                new WeakReference<ConcurrentWeakList<object>.Node>(node2),
                value);
        });

        Helpers.ForceGC();

        node1.TryGetTarget(out _).ShouldBeFalse();
        node1InternalNode.TryGetTarget(out _).ShouldBeFalse();
        node1InternalNodeHelper.TryGetTarget(out _).ShouldBeFalse();
        node2.TryGetTarget(out var actualNode).ShouldBeTrue();
        actualNode.IsRemoved.ShouldBeFalse();

        GC.KeepAlive(value);
        GC.KeepAlive(list);
    }
}
