using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Singulink.Collections.Weak.ConcurrentTests;

public static class Helpers
{
    public static void ForceGC()
    {
        for (int i = 0; i < 10; i++)
        {
            GC.Collect(int.MaxValue, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NotInlined(Action a) => a();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T NotInlined<T>(Func<T> f) => f();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NotInlined<TState>(TState state, Action<TState> a) => a(state);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T NotInlined<TState, T>(TState state, Func<TState, T> f) => f(state);

    private static class GetInternalNodeHelpers<T> where T : class
    {
        public static readonly MethodInfo _GetInternalNodeHelperMethod
            = typeof(ConcurrentWeakList<T>.Node).GetMethod("GetInternalNodeHelper", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly FieldInfo _InternalNodeField
            = typeof(ConcurrentWeakList<T>.Node).GetField("_internalNode", BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    public static object? GetInternalNode<T>(ConcurrentWeakList<T>.Node node) where T : class
    {
        return GetInternalNodeHelpers<T>._InternalNodeField.GetValue(node);
    }

    public static object? GetInternalNodeFinalizeHelper<T>(ConcurrentWeakList<T>.Node node) where T : class
    {
        return GetInternalNodeHelpers<T>._GetInternalNodeHelperMethod.Invoke(node, []);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Consume<T>(ref T value) { }
}
