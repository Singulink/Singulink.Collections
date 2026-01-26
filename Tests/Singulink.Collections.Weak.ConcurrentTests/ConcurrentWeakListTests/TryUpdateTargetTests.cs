#if NET9_0_OR_GREATER
namespace Singulink.Collections.Weak.ConcurrentTests.ConcurrentWeakListTests;

[PrefixTestClass]
public class TryUpdateTargetTests
{
    [TestMethod]
    public void TryUpdateTargetSucceeds()
    {
        var list = new ConcurrentWeakList<object>();
        object originalValue = new();
        object newValue = new();
        var node = list.AddLast(originalValue);

        bool result = node.TryUpdateTarget(newValue);

        result.ShouldBeTrue();
        node.Value.ShouldBeSameAs(newValue);
        list.ToList().ShouldBe([newValue]);

        GC.KeepAlive(originalValue);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryUpdateTargetMultipleTimes()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        var node = list.AddLast(value1);

        node.TryUpdateTarget(value2).ShouldBeTrue();
        node.Value.ShouldBeSameAs(value2);

        node.TryUpdateTarget(value3).ShouldBeTrue();
        node.Value.ShouldBeSameAs(value3);

        list.ToList().ShouldBe([value3]);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
    }

    [TestMethod]
    public void TryUpdateTargetOnRemovedNodeReturnsFalse()
    {
        var list = new ConcurrentWeakList<object>();
        object originalValue = new();
        object newValue = new();
        var node = list.AddLast(originalValue);

        list.Remove(node);

        bool result = node.TryUpdateTarget(newValue);

        result.ShouldBeFalse();

        GC.KeepAlive(originalValue);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryUpdateTargetOnDisposedNodeReturnsFalse()
    {
        var list = new ConcurrentWeakList<object>();
        object originalValue = new();
        object newValue = new();
        var node = list.AddLast(originalValue);

        node.Dispose();

        bool result = node.TryUpdateTarget(newValue);

        result.ShouldBeFalse();

        GC.KeepAlive(originalValue);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryUpdateTargetAfterListDisposedReturnsFalse()
    {
        var list = new ConcurrentWeakList<object>();
        object originalValue = new();
        object newValue = new();
        var node = list.AddLast(originalValue);

        list.Dispose();

        bool result = node.TryUpdateTarget(newValue);

        result.ShouldBeFalse();

        GC.KeepAlive(originalValue);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryUpdateTargetAfterListClearReturnsFalse()
    {
        var list = new ConcurrentWeakList<object>();
        object originalValue = new();
        object newValue = new();
        var node = list.AddLast(originalValue);

        list.Clear();

        bool result = node.TryUpdateTarget(newValue);

        result.ShouldBeFalse();

        GC.KeepAlive(originalValue);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryUpdateTargetWithNullThrows()
    {
        var list = new ConcurrentWeakList<object>();
        object originalValue = new();
        var node = list.AddLast(originalValue);

        Should.Throw<ArgumentNullException>(() => node.TryUpdateTarget(null!));

        GC.KeepAlive(originalValue);
    }

    [TestMethod]
    public void TryUpdateTargetPreservesNodePosition()
    {
        var list = new ConcurrentWeakList<object>();
        object value1 = new();
        object value2 = new();
        object value3 = new();
        object newValue2 = new();
        list.AddLast(value1);
        var node2 = list.AddLast(value2);
        list.AddLast(value3);

        node2.TryUpdateTarget(newValue2).ShouldBeTrue();

        list.ToList().ShouldBe([value1, newValue2, value3]);
        list.UnsafeGetIndexOfNode(node2).ShouldBe((nint)1);

        GC.KeepAlive(value1);
        GC.KeepAlive(value2);
        GC.KeepAlive(value3);
        GC.KeepAlive(newValue2);
    }

    [TestMethod]
    public void TryUpdateTargetDoesNotChangeListVersion()
    {
        var list = new ConcurrentWeakList<object>();
        object originalValue = new();
        object newValue = new();
        var node = list.AddLast(originalValue);
        var versionBefore = list.Version;

        node.TryUpdateTarget(newValue);

        var versionAfter = list.Version;
        (versionBefore == versionAfter).ShouldBeTrue();

        GC.KeepAlive(originalValue);
        GC.KeepAlive(newValue);
    }

    [TestMethod]
    public void TryUpdateTargetDoesNotChangeNodeVersion()
    {
        var list = new ConcurrentWeakList<object>();
        object originalValue = new();
        object newValue = new();
        var node = list.AddLast(originalValue);
        var versionBefore = node.Version;

        node.TryUpdateTarget(newValue);

        var versionAfter = node.Version;
        (versionBefore == versionAfter).ShouldBeTrue();

        GC.KeepAlive(originalValue);
        GC.KeepAlive(newValue);
    }
}
#endif
