using System.Runtime.CompilerServices;

namespace Singulink.Collections.Weak.Tests;

internal static class Helpers
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WeakReference<object> GetWeakRef()
    {
        return new WeakReference<object>(new object());
    }

    public static void CollectAndWait()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}