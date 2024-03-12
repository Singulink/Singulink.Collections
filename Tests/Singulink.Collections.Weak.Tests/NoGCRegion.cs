#pragma warning disable CA1815 // Override equals and operator equals on value types

using System.Runtime.CompilerServices;

namespace Singulink.Collections.Weak.Tests;

#if NET || RELEASE

public readonly struct NoGCRegion : IDisposable
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NoGCRegion Enter(long memoryNeeded)
    {
        for (int i = 0; i < 1000; i++)
        {
            if (GC.TryStartNoGCRegion(memoryNeeded, true))
                return default;

            Thread.Sleep(1);
        }

        throw new InvalidOperationException("Failed to enter no GC region.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        GC.EndNoGCRegion();
    }
}

#endif