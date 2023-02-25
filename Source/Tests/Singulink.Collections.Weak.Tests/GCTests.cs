using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Singulink.Collections.Weak.Tests
{
    [TestClass]
    public class GCTests
    {
        [TestMethod]
        public void EnterNoGCRegionAndCollect()
        {
            var weakRef = Helpers.GetWeakRef();

            using (NoGCRegion.Enter(1000)) { }

            Helpers.CollectAndWait();
            Assert.IsFalse(weakRef.TryGetTarget(out _));
        }
    }
}