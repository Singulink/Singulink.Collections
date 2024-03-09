using System.Runtime.CompilerServices;

namespace Singulink.Collections.Weak.Tests;

[PrefixTestClass]
public class WeakCollectionTests
{
    [TestMethod]
    public void Clean()
    {
        var c = new WeakCollection<object>();
        object x = new();

        int noGcAddCountSinceLastClean;
        int noGcUnsafeCount;

        using (NoGCRegion.Enter(1000))
        {
            c.Add(x);
            c.Add(x);
            c.Add(x);

            AddCollectableItems(c, 3);

            noGcAddCountSinceLastClean = c.AddCountSinceLastClean;
            noGcUnsafeCount = c.UnsafeCount;
        }

        noGcAddCountSinceLastClean.ShouldBe(6);
        noGcUnsafeCount.ShouldBe(6);

        Helpers.CollectAndWait();

        c.Clean();
        c.AddCountSinceLastClean.ShouldBe(0);
        c.UnsafeCount.ShouldBe(3);

        GC.KeepAlive(x);
    }

    [TestMethod]
    public void EnumerationCleaning()
    {
        var c = new WeakCollection<object>();
        object x = new();

        int noGcAddCountSinceLastClean;
        int noGcUnsafeCount;

        using (NoGCRegion.Enter(1000))
        {
            c.Add(x);
            c.Add(x);
            c.Add(x);

            AddCollectableItems(c, 3);

            noGcAddCountSinceLastClean = c.AddCountSinceLastClean;
            noGcUnsafeCount = c.UnsafeCount;
        }

        noGcAddCountSinceLastClean.ShouldBe(6);
        noGcUnsafeCount.ShouldBe(6);

        Helpers.CollectAndWait();

        foreach (object o in c)
        { }

        c.Remove(x).ShouldBeTrue();
        c.Remove(x).ShouldBeTrue();

#if NET48 // NS2.0 target does not support removing stale entries as items are encountered.
        c.AddCountSinceLastClean.ShouldBe(6);
        c.UnsafeCount.ShouldBe(4);
#else
        c.AddCountSinceLastClean.ShouldBe(0);
        c.UnsafeCount.ShouldBe(1);
#endif

        GC.KeepAlive(x);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddCollectableItems(WeakCollection<object> c, int count)
    {
        for (int i = 0; i < count; i++)
            c.Add(new object());
    }
}
