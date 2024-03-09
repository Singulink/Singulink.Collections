#pragma warning disable CA1815 // Override equals and operator equals on value types

using System.Security.Cryptography;

namespace Singulink.Collections.Weak.Tests;

public struct NoGCRegion : IDisposable
{
    public static NoGCRegion Enter(long memoryNeeded)
    {
        for (int i = 0; i < 1000; i++)
        {
            if (GC.TryStartNoGCRegion(memoryNeeded, true))
                return default;

            Thread.Sleep(1);
        }

        throw new InvalidOperationException("Could not enter no GC region.");
    }

    public void Dispose()
    {
        GC.EndNoGCRegion();
    }
}