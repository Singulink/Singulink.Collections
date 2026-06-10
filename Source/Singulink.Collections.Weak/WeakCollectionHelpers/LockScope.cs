using System.Runtime.CompilerServices;

// NOTE: lock entry algorithms are expected to look the same as those in WeakList.Locking.cs.

namespace Singulink.Collections.WeakCollectionHelpers;

internal ref struct LockScope(Lock locker, object container)
{
    private Lock? _locker = locker;
    private object? _container = container;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_locker is null) return;
        if (_locker.IsHeldByCurrentThread) _locker.Exit();
        _locker = null;

        // Keep container alive until after we exit the lock - this is important for many of the algorithms that use the lock:
        GC.KeepAlive(_container);
        _container = null;
    }

    /// <summary>
    /// Enters a given container's lock, returning a scope that will exit the lock on dispose.
    /// </summary>
    /// <remarks>
    /// If the container is disposed, <paramref name="wasDisposed" /> will be set to <see langword="true"/> and the returned scope will do nothing on dispose.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LockScope EnterLock<T, TNode, TContainer, TNodeHelpers>(TContainer container, out bool wasDisposed)
        where T : class
        where TNode : class
        where TContainer : class
        where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
    {
        if (default(TNodeHelpers).IsDisposed(container))
        {
            wasDisposed = true;
            return default;
        }

        SpinWait sw = default;
        var locker = default(TNodeHelpers).GetContainerValues(container)._locker;
        while (true)
        {
            if (locker.TryEnter())
            {
                wasDisposed = default(TNodeHelpers).IsDisposed(container);
                if (wasDisposed) locker.Exit();
                return wasDisposed ? default : new LockScope(locker, container);
            }

            sw.SpinOnce();
        }
    }

    /// <summary>
    /// Tries to enter a given container's lock, returning a scope that will exit the lock on dispose if successful.
    /// </summary>
    /// <remarks>
    /// If the container is disposed, <paramref name="wasDisposed" /> will be set to <see langword="true"/> and the returned scope will do nothing on dispose.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LockScope TryEnterLock<T, TNode, TContainer, TNodeHelpers>(TContainer container, out bool wasDisposed, out bool entered)
        where T : class
        where TNode : class
        where TContainer : class
        where TNodeHelpers : struct, INodeHelpers<T, TNode, TContainer, TNodeHelpers>
    {
        if (default(TNodeHelpers).IsDisposed(container))
        {
            wasDisposed = true;
            entered = false;
            return default;
        }

        var locker = default(TNodeHelpers).GetContainerValues(container)._locker;
        if (locker.TryEnter())
        {
            wasDisposed = default(TNodeHelpers).IsDisposed(container);
            entered = !wasDisposed;
            if (wasDisposed) locker.Exit();
            return wasDisposed ? default : new LockScope(locker, container);
        }

        wasDisposed = false;
        entered = false;
        return default;
    }
}
