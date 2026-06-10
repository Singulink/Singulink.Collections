using System.Reflection;
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
        // The node state now lives in a NodeState struct stored in the Node's '_impl' field.
        public static readonly FieldInfo _ImplField
            = typeof(WeakList<T>.Node).GetField("_impl", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly MethodInfo _GetInternalNodeHelperMethod
            = _ImplField.FieldType.GetMethod("GetInternalNodeHelper", BindingFlags.Public | BindingFlags.Instance)!;
        public static readonly FieldInfo _InternalNodeField
            = _ImplField.FieldType.GetField("_internalNode", BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    public static object? GetInternalNode<T>(WeakList<T>.Node node) where T : class
    {
        object impl = GetInternalNodeHelpers<T>._ImplField.GetValue(node)!;
        return GetInternalNodeHelpers<T>._InternalNodeField.GetValue(impl);
    }

    public static object? GetInternalNodeFinalizeHelper<T>(WeakList<T>.Node node) where T : class
    {
        object impl = GetInternalNodeHelpers<T>._ImplField.GetValue(node)!;
        return GetInternalNodeHelpers<T>._GetInternalNodeHelperMethod.Invoke(impl, []);
    }

    /// <summary>
    /// Returns whether the netstandard CWT-based tracking table currently has an entry for <paramref name="value"/>, or <see langword="null"/>
    /// when the running configuration does not use the CWT path (e.g. the .NET DependentHandle path, where there is no CWT).
    /// </summary>
    public static bool? CwtContainsValue<T>(WeakList<T> list, T value) where T : class
    {
        var containerValuesField = typeof(WeakList<T>).GetField("_containerValues", BindingFlags.NonPublic | BindingFlags.Instance);

        if (containerValuesField is null)
            return null;

        object? containerValues = containerValuesField.GetValue(list);

        if (containerValues is null)
            return null;

        var field = containerValues.GetType().GetField("_cwt", BindingFlags.NonPublic | BindingFlags.Instance);

        if (field is null)
            return null; // .NET path uses DependentHandle; there is no CWT.

        object? cwt = field.GetValue(containerValues);

        if (cwt is null)
            return false;

        var tryGetValue = cwt.GetType().GetMethod("TryGetValue")!;
        object?[] args = [value, null];

        return (bool)tryGetValue.Invoke(cwt, args)!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Consume<T>(ref T value) { }
}