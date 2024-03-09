#pragma warning disable CA1815 // Override equals and operator equals on value types

namespace Singulink.Collections.Weak.Tests;

public struct NoGCRegion : IDisposable
{
    public static NoGCRegion Enter(long memoryNeeded)
    {
        GC.TryStartNoGCRegion(memoryNeeded).ShouldBeTrue();
        return default;
    }

    public void Dispose()
    {
        GC.EndNoGCRegion();
    }
}