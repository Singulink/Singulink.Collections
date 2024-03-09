namespace Singulink.Collections.Weak.Tests;

[PrefixTestClass]
public class GCTests
{
    [TestMethod]
    public void EnterNoGCRegionAndCollect()
    {
        var weakRef = Helpers.GetWeakRef();

        using (NoGCRegion.Enter(1000)) { }

        Helpers.CollectAndWait();
        weakRef.TryGetTarget(out _).ShouldBeFalse();
    }
}