namespace Singulink.Collections.Weak.Tests.WeakListTests;

[PrefixTestClass]
public class DisposeTests
{
    [TestMethod]
    public void DisposeEmptyList()
    {
        var list = new WeakList<object>();
        list.Dispose();

        Should.Throw<ObjectDisposedException>(() => list.Count);
        list.ShouldBeEmpty();
    }

    [TestMethod]
    public void DisposeTwice()
    {
        var list = new WeakList<object>();
        var value = new object();
        var node = list.AddLast(value);

        list.Dispose();
        list.Dispose();

        node.IsRemoved.ShouldBeTrue();
        Should.Throw<ObjectDisposedException>(() => list.Count);
        GC.KeepAlive(value);
    }

    [TestMethod]
    public void CountThrowsAfterDispose()
    {
        var list = new WeakList<object>();
        list.AddLast(new object());
        list.Dispose();

        Should.Throw<ObjectDisposedException>(() => list.Count);
#pragma warning disable CS0618 // Type or member is obsolete
        Should.Throw<ObjectDisposedException>(() => list.UnsafeCount);
#pragma warning restore CS0618
    }

    [TestMethod]
    public void ValueBasedOperationsAfterDisposeFindNothing()
    {
        var list = new WeakList<object>();
        var value = new object();
        var node = list.AddLast(value);

        list.Dispose();

        list.Contains(value).ShouldBeFalse();
        list.Find(value).ShouldBeNull();
        list.Find(v => true).ShouldBeNull();
        list.Remove(value).ShouldBeFalse();
        list.TryInsertBefore(value, new object()).ShouldBeNull();
        list.TryInsertAfter(value, new object()).ShouldBeNull();

        node.IsRemoved.ShouldBeTrue();
        node.List.ShouldBeSameAs(list);
        GC.KeepAlive(value);
    }

    [TestMethod]
    public void NodeOperationsAfterDisposeAreNoOps()
    {
        var list = new WeakList<object>();
        var value = new object();
        var node = list.AddLast(value);

        list.Dispose();

        list.Remove(node);
        node.Dispose();
        list.Clear();

        node.IsRemoved.ShouldBeTrue();
        Should.Throw<ObjectDisposedException>(() => list.Count);
        GC.KeepAlive(value);
    }

    [TestMethod]
    public void AddRelativeToNodeAfterDisposeThrows()
    {
        var list = new WeakList<object>();
        var value = new object();
        var node = list.AddLast(value);

        list.Dispose();

        Should.Throw<ObjectDisposedException>(() => list.AddBefore(node, new object()));
        Should.Throw<ObjectDisposedException>(() => list.AddAfter(node, new object()));
        Should.Throw<ObjectDisposedException>(() => list.AddBefore(node, new object(), allowBeforeRemovedNode: true));
        Should.Throw<ObjectDisposedException>(() => list.AddAfter(node, new object(), allowAfterRemovedNode: true));
        GC.KeepAlive(value);
    }

    [TestMethod]
    public void EnumeratorsCreatedBeforeDisposeEndEarly()
    {
        var list = new WeakList<object>();
        var values = new[] { new object(), new object(), new object() };

        foreach (var value in values)
            list.AddLast(value);

        var enumerator = list.GetEnumerator();
        var nodeEnumerator = list.GetNodeEnumerator();

        enumerator.MoveNext().ShouldBeTrue();
        nodeEnumerator.MoveNext().ShouldBeTrue();

        list.Dispose();

        enumerator.MoveNext().ShouldBeFalse();
        nodeEnumerator.MoveNext().ShouldBeFalse();
        enumerator.MovePrevious().ShouldBeFalse();
        nodeEnumerator.MovePrevious().ShouldBeFalse();
        GC.KeepAlive(values);
    }
}
