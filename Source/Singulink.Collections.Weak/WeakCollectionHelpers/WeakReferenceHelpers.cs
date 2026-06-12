using System.Runtime.CompilerServices;

namespace Singulink.Collections.WeakCollectionHelpers;

// Small shared helpers for working with nullable weak references.
internal static class WeakReferenceHelpers
{
    // Returns the target if the weak reference is non-null and its target is still alive, otherwise null.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TValue? TryGetValue<TValue>(WeakReference<TValue>? wr) where TValue : class
    {
        if (wr is null) return null;
        if (!wr.TryGetTarget(out var result)) return null;
        return result;
    }
}
