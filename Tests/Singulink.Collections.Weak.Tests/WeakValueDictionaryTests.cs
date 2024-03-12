using System.Runtime.CompilerServices;

namespace Singulink.Collections.Weak.Tests;

#if NET || RELEASE

[PrefixTestClass]
public class WeakValueDictionaryTests
{
    [TestMethod]
    public void Clean()
    {
        var c = new WeakValueDictionary<int, object>();
        object x = new();

        int noGcAddCountSinceLastClean;
        int noGcUnsafeCount;

        using (NoGCRegion.Enter(1000))
        {
            c.Add(0, x);
            c.Add(1, x);
            c.Add(2, x);

            AddCollectableItems(c, 3, 3);

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
        var c = new WeakValueDictionary<int, object>();
        object x = new();

        int noGcAddCountSinceLastClean;
        int noGcUnsafeCount;
        bool noGcContainsKey4;

        using (NoGCRegion.Enter(1000))
        {
            c.Add(0, x);
            c.Add(1, x);
            c.Add(2, x);

            AddCollectableItems(c, 3, 3);

            noGcAddCountSinceLastClean = c.AddCountSinceLastClean;
            noGcUnsafeCount = c.UnsafeCount;

            noGcContainsKey4 = c.ContainsKey(4);
        }

        noGcAddCountSinceLastClean.ShouldBe(6);
        noGcUnsafeCount.ShouldBe(6);

        noGcContainsKey4.ShouldBeTrue();
        c.ContainsKey(1).ShouldBeTrue();

        Helpers.CollectAndWait();

        c.ContainsKey(4).ShouldBeFalse();
        c.ContainsKey(1).ShouldBeTrue();

        foreach (object o in c) { }

        c.Remove(0).ShouldBeTrue();
        c.Remove(1).ShouldBeTrue();
        c.Remove(4).ShouldBeFalse();

#if NET48 // NS2.0 target does not support removing stale entries as items are encountered.
        c.AddCountSinceLastClean.ShouldBe(6);
        c.UnsafeCount.ShouldBe(3);
#else
        c.AddCountSinceLastClean.ShouldBe(0);
        c.UnsafeCount.ShouldBe(1);
#endif

        GC.KeepAlive(x);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddCollectableItems(WeakValueDictionary<int, object> c, int startKey, int count)
    {
        for (int i = 0; i < count; i++)
            c.Add(startKey++, new object());
    }
}

#endif