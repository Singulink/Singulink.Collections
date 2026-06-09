using System.Runtime.CompilerServices;

namespace Singulink.Collections;

/// <content>
/// Contains the locking implementation for <see cref="WeakList{T}"/>.
/// </content>
public sealed partial class WeakList<T>
{
    private ref struct LockScope(Lock locker, WeakList<T> list)
    {
        private Lock? _locker = locker;
        private WeakList<T>? _list = list;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_locker is null) return;
            if (_locker.IsHeldByCurrentThread) _locker.Exit();
            _locker = null;

            // Keep list alive until after we exit the lock - this is important for many of the algorithms that use the lock:
            GC.KeepAlive(_list);
            _list = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LockScope EnterLock(out bool wasDisposed)
    {
        if (_head == null)
        {
            wasDisposed = true;
            return default;
        }

        SpinWait sw = default;
        while (true)
        {
            if (_locker.TryEnter())
            {
                wasDisposed = _head == null;
                if (wasDisposed) _locker.Exit();
                return wasDisposed ? default : new LockScope(_locker, this);
            }

            sw.SpinOnce();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LockScope TryEnterLock(out bool wasDisposed, out bool entered)
    {
        if (_head == null)
        {
            wasDisposed = true;
            entered = false;
            return default;
        }

        if (_locker.TryEnter())
        {
            wasDisposed = _head == null;
            entered = !wasDisposed;
            if (wasDisposed) _locker.Exit();
            return wasDisposed ? default : new LockScope(_locker, this);
        }

        wasDisposed = false;
        entered = false;
        return default;
    }
}
