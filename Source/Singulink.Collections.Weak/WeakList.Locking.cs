using System.Runtime.CompilerServices;

using Singulink.Collections.WeakCollectionHelpers;

namespace Singulink.Collections;

/// <content>
/// Contains the locking implementation for <see cref="WeakList{T}"/>.
/// </content>
public sealed partial class WeakList<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LockScope EnterLock(out bool wasDisposed)
    {
        return LockScope.EnterLock<T, Node, WeakList<T>, Node.NodeHelpers>(this, out wasDisposed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LockScope TryEnterLock(out bool wasDisposed, out bool entered)
    {
        return LockScope.TryEnterLock<T, Node, WeakList<T>, Node.NodeHelpers>(this, out wasDisposed, out entered);
    }
}
