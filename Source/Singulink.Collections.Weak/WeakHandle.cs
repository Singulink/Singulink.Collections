using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Singulink.Collections;

#pragma warning disable SA1132 // Do not combine fields
#pragma warning disable SA1401 // Fields should be private

// Similar to WeakReference<T>, but automatically pools a small amount & avoids allocating a wrapper class, since the containing class disposes it, allows
// using interlocked operations, and doesn't let GC see it directly (useful for values in finalizers).
internal struct WeakHandle(IntPtr handle)
{
    public IntPtr Handle = handle; // Actual handle value to support interlocked operations.

#if NET10_0_OR_GREATER
    private readonly WeakGCHandle<object?> AsGCHandle() => WeakGCHandle<object?>.FromIntPtr(Handle);
#else
    private readonly GCHandle AsGCHandle()
    {
        IntPtr handle = Handle;

        if (handle == IntPtr.Zero)
            return default;

        return GCHandle.FromIntPtr(handle);
    }
#endif

    private readonly void DisposeReal()
    {
        var handle = AsGCHandle();
#if NET10_0_OR_GREATER
        handle.Dispose();
#else
        handle.Free();
#endif
    }

    public readonly void SetTarget(object? target)
    {
#if NET10_0_OR_GREATER
        AsGCHandle().SetTarget(target);
#else
        var handle = AsGCHandle();
        handle.Target = target;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T? TryGetTarget<T>() where T : class?
    {
        var handle = AsGCHandle();

        if (!handle.IsAllocated)
            return null;

        object? result;

#if NET10_0_OR_GREATER
        if (!handle.TryGetTarget(out result))
            result = null;
#else
        result = handle.Target;
#endif

        Debug.Assert(result is T or null, "Stored target should be of the correct type or null.");
        return Unsafe.As<T?>(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WeakHandle Alloc(object? target)
    {
        // Try to get from per-thread cache:
        var perThreadCache = _perThreadCache;

        if (perThreadCache is not null && (uint)perThreadCache.Count > 0)
        {
            Debug.Assert(perThreadCache.Count > 0 && perThreadCache.Count <= PerThreadWeakHandleHolder.NumHandles, "Index should be in range.");
            ref IntPtr handleRef = ref Unsafe.Add(ref perThreadCache.Handle0, (IntPtr)(nint)(nuint)(uint)--perThreadCache.Count);
            var handle = new WeakHandle(handleRef);
            handleRef = IntPtr.Zero;
            handle.SetTarget(target);
            GC.KeepAlive(perThreadCache);
            return handle;
        }

        // Try to get from the shared cache:
        return Fallback(target);
        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakHandle Fallback(object? target)
        {
            IntPtr handleValue;
            var shared = _sharedCache;
#if NET
            ReadOnlySpan<IntPtr> values = MemoryMarshal.CreateReadOnlySpan(ref shared.Handle0, SharedWeakHandleHolder.NumHandles);
            int potentialIndex = values.IndexOfAnyExcept(IntPtr.Zero);

            if (potentialIndex >= 0)
            {
                Debug.Assert(potentialIndex < SharedWeakHandleHolder.NumHandles, "Index should be in range.");
                ref IntPtr handleRef = ref Unsafe.Add(ref shared.Handle0, (uint)potentialIndex);
#else
            for (int i = 0; i < SharedWeakHandleHolder.NumHandles; i++)
            {
                ref IntPtr handleRef = ref Unsafe.Add(ref shared.Handle0, i);
#endif
                handleValue = Interlocked.Exchange(ref handleRef, IntPtr.Zero);

                if (handleValue != IntPtr.Zero)
                {
                    var handle = new WeakHandle(handleValue);
                    handle.SetTarget(target);
                    GC.KeepAlive(shared);
                    return handle;
                }
            }

            // Otherwise, just make a new one:
#if NET10_0_OR_GREATER
            handleValue = WeakGCHandle<object?>.ToIntPtr(new WeakGCHandle<object?>(target));
#else
            handleValue = GCHandle.ToIntPtr(GCHandle.Alloc(target, GCHandleType.Weak));
#endif
            return new WeakHandle(handleValue);
        }
    }

    // Does not dispose in a thread-safe way.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        // Handle freeing:
        if (Handle != IntPtr.Zero)
        {
            // Check if per-thread cache has a slot available:

            var perThreadCache = _perThreadCache;

            if (perThreadCache is null)
            {
                perThreadCache = new PerThreadWeakHandleHolder();
                _perThreadCache = perThreadCache;
            }

            if ((uint)perThreadCache.Count < PerThreadWeakHandleHolder.NumHandles)
            {
                Debug.Assert(perThreadCache.Count < PerThreadWeakHandleHolder.NumHandles, "Index should be in range.");
                ref IntPtr handleRef = ref Unsafe.Add(ref perThreadCache.Handle0, (IntPtr)(nint)(nuint)(uint)perThreadCache.Count++);
                SetTarget(null);
                handleRef = Handle;
                GC.KeepAlive(perThreadCache);
                goto done;
            }

            // Try to return to the shared cache:
            Fallback(this);
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Fallback(WeakHandle handle)
            {
                IntPtr handleValue = handle.Handle;
                var shared = _sharedCache;
#if NET
                Span<IntPtr> values = MemoryMarshal.CreateSpan(ref shared.Handle0, SharedWeakHandleHolder.NumHandles);
                int potentialIndex = values.IndexOf(IntPtr.Zero);

                if (potentialIndex >= 0)
                {
                    handle.SetTarget(null);
                    Debug.Assert(potentialIndex < SharedWeakHandleHolder.NumHandles, "Index should be in range.");
                    ref IntPtr handleRef = ref Unsafe.Add(ref shared.Handle0, (uint)potentialIndex);
#else
                handle.SetTarget(null);
                for (int i = 0; i < SharedWeakHandleHolder.NumHandles; i++)
                {
                    ref IntPtr handleRef = ref Unsafe.Add(ref shared.Handle0, i);
#endif
                    IntPtr oldValue = Interlocked.CompareExchange(ref handleRef, handleValue, IntPtr.Zero);

                    if (oldValue == IntPtr.Zero)
                    {
                        GC.KeepAlive(shared);
                        return;
                    }
                }

                // Otherwise, dispose for real:
                handle.DisposeReal();
            }
        }

        // Set to default:
        done:
        this = default;
    }

    private static readonly SharedWeakHandleHolder _sharedCache = new();

    [ThreadStatic]
    private static PerThreadWeakHandleHolder? _perThreadCache;

    [StructLayout(LayoutKind.Sequential)]
    private sealed class PerThreadWeakHandleHolder
    {
        public const int NumHandles = 4;
        public IntPtr Handle0, Handle1, Handle2, Handle3;
        public int Count;

        // Finalizer to clean up when thread exits:
        ~PerThreadWeakHandleHolder()
        {
            for (int i = 0; i < Count; i++)
            {
                IntPtr handleValue = Unsafe.Add(ref Handle0, i);
                var handle = new WeakHandle(handleValue);
                handle.DisposeReal();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private sealed class SharedWeakHandleHolder
    {
        public const int NumHandles = 8;
        public IntPtr Handle0, Handle1, Handle2, Handle3, Handle4, Handle5, Handle6, Handle7;

        // Finalizer to clean up when process exits or if we get unloaded:
        ~SharedWeakHandleHolder()
        {
            for (int i = 0; i < NumHandles; i++)
            {
                IntPtr handleValue = Unsafe.Add(ref Handle0, i);

                if (handleValue != IntPtr.Zero)
                {
                    var handle = new WeakHandle(handleValue);
                    handle.DisposeReal();
                }
            }
        }
    }
}
