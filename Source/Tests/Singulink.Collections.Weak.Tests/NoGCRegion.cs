using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CA1815 // Override equals and operator equals on value types

namespace Singulink.Collections.Weak.Tests
{
    public struct NoGCRegion : IDisposable
    {
        public static NoGCRegion Enter(long memoryNeeded)
        {
            Assert.AreEqual(true, GC.TryStartNoGCRegion(memoryNeeded));
            return default;
        }

        public void Dispose()
        {
            GC.EndNoGCRegion();
        }
    }
}