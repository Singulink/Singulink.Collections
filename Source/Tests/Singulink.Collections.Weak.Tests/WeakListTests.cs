using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.Collections.Weak.Tests
{
    [TestClass]
    public class WeakListTests
    {
        [TestMethod]
        public void Clean()
        {
            var c = new WeakList<object>();
            object x = new object();

            using (NoGCRegion.Enter(1000)) {
                AddCollectableItems(c, 3);

                c.InsertFirst(x);
                c.InsertAfter(x, x);
                c.InsertBefore(x, x);

                Assert.AreEqual(6, c.AddCountSinceLastClean);
                Assert.AreEqual(6, c.UnsafeCount);
                Assert.IsTrue(c.Take(3).SequenceEqual(new[] { x, x, x }));
            }

            Helpers.CollectAndWait();

            c.Clean();
            Assert.AreEqual(0, c.AddCountSinceLastClean);
            Assert.AreEqual(3, c.UnsafeCount);

            c.Remove(x);
            c.Remove(x);
            Assert.AreEqual(1, c.UnsafeCount);

            GC.KeepAlive(x);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddCollectableItems(WeakList<object> c, int count)
        {
            for (int i = 0; i < count; i++)
                c.Add(new object());
        }
    }
}