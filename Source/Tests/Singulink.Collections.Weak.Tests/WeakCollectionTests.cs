using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.Collections.Weak.Tests
{
    [TestClass]
    public class WeakCollectionTests
    {
        [TestMethod]
        public void Clean()
        {
            var c = new WeakCollection<object>();
            object x = new object();

            using (NoGCRegion.Enter(1000)) {
                c.Add(x);
                c.Add(x);
                c.Add(x);

                AddCollectableItems(c, 3);

                Assert.AreEqual(6, c.AddCountSinceLastClean);
                Assert.AreEqual(6, c.UnsafeCount);
            }

            Helpers.CollectAndWait();

            c.Clean();
            Assert.AreEqual(0, c.AddCountSinceLastClean);
            Assert.AreEqual(3, c.UnsafeCount);

            GC.KeepAlive(x);
        }

        [TestMethod]
        public void EnumerationCleaning()
        {
            var c = new WeakCollection<object>();
            object x = new object();

            using (NoGCRegion.Enter(1000)) {
                c.Add(x);
                c.Add(x);
                c.Add(x);

                AddCollectableItems(c, 3);

                Assert.AreEqual(6, c.AddCountSinceLastClean);
                Assert.AreEqual(6, c.UnsafeCount);
            }

            Helpers.CollectAndWait();

            foreach (object o in c) { }

            Assert.IsTrue(c.Remove(x));
            Assert.IsTrue(c.Remove(x));

#if NET48 // NS2.0 target does not support removing stale entries as items are encountered.
            Assert.AreEqual(6, c.AddCountSinceLastClean);
            Assert.AreEqual(4, c.UnsafeCount);
#else
            Assert.AreEqual(0, c.AddCountSinceLastClean);
            Assert.AreEqual(1, c.UnsafeCount);
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
}