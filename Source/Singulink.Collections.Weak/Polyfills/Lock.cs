#if NET9_0_OR_GREATER

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.Lock))]

#else

namespace System.Threading;

#pragma warning disable CS9216 // Don't cast Lock to object (intentional in this polyfill since Monitor is the implementation)

internal sealed class Lock
{
    public bool TryEnter() => Monitor.TryEnter(this);

    public void Enter() => Monitor.Enter(this);

    public void Exit() => Monitor.Exit(this);

    public bool IsHeldByCurrentThread => Monitor.IsEntered(this);

    public Scope EnterScope()
    {
        Monitor.Enter(this);
        return new Scope(this);
    }

    public ref struct Scope
    {
        private Lock? _locker;

        internal Scope(Lock locker) { _locker = locker; }

        public void Dispose()
        {
            if (_locker is null) return;
            Monitor.Exit(_locker);
            _locker = null;
        }
    }
}

#endif