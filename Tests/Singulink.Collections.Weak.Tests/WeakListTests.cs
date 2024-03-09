using System.Runtime.CompilerServices;

namespace Singulink.Collections.Weak.Tests;

[PrefixTestClass]
public class WeakListTests
{
    [TestMethod]
    public void Clean()
    {
        var c = new WeakList<object>();
        object x = new();

        int noGcAddCountSinceLastClean;
        int noGcUnsafeCount;

        using (NoGCRegion.Enter(1000))
        {
            AddCollectableItems(c, 3);

            c.InsertFirst(x);
            c.InsertAfter(x, x);
            c.InsertBefore(x, x);

            noGcAddCountSinceLastClean = c.AddCountSinceLastClean;
            noGcUnsafeCount = c.UnsafeCount;
        }

        noGcAddCountSinceLastClean.ShouldBe(6);
        noGcUnsafeCount.ShouldBe(6);
        c.Take(3).ShouldBe([x, x, x]);

        Helpers.CollectAndWait();

        c.Clean();
        c.AddCountSinceLastClean.ShouldBe(0);
        c.UnsafeCount.ShouldBe(3);

        c.Remove(x);
        c.Remove(x);
        c.UnsafeCount.ShouldBe(1);

        GC.KeepAlive(x);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddCollectableItems(WeakList<object> c, int count)
    {
        for (int i = 0; i < count; i++)
            c.Add(new object());
    }
}
