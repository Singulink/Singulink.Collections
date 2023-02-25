using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.Collections.Weak.Tests
{
    [TestClass]
    public class WeakValueDictionaryTests
    {
        [TestMethod]
        public void Clean()
        {
            var c = new WeakValueDictionary<int, object>();
            object x = new object();

            using (NoGCRegion.Enter(1000)) {
                c.Add(0, x);
                c.Add(1, x);
                c.Add(2, x);

                AddCollectableItems(c, 3, 3);

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
            var c = new WeakValueDictionary<int, object>();
            object x = new object();

            using (NoGCRegion.Enter(1000)) {
                c.Add(0, x);
                c.Add(1, x);
                c.Add(2, x);

                AddCollectableItems(c, 3, 3);

                Assert.AreEqual(6, c.AddCountSinceLastClean);
                Assert.AreEqual(6, c.UnsafeCount);
                Assert.IsTrue(c.ContainsKey(1));

                #if DEBUG || !NETFRAMEWORK // Causes entry with key 4 not to collection on NETFW release builds
                Assert.IsTrue(c.ContainsKey(4));
                #endif
            }

            Helpers.CollectAndWait();

            Assert.IsTrue(c.ContainsKey(1));
            Assert.IsFalse(c.ContainsKey(4));

            foreach (object o in c) { }

            Assert.IsTrue(c.Remove(0));
            Assert.IsTrue(c.Remove(1));
            Assert.IsFalse(c.Remove(4));

            #if NETCOREAPP2_2 // NS2.0 target does not support removing stale entries as items are encountered.
            Assert.AreEqual(6, c.AddCountSinceLastClean);
            Assert.AreEqual(4, c.UnsafeCount);
            #else
            Assert.AreEqual(0, c.AddCountSinceLastClean);
            Assert.AreEqual(1, c.UnsafeCount);
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
}