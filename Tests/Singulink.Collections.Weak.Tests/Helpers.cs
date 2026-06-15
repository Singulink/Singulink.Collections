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

    // This method ensures that we aren't storing any old junk in registers or on the first part of the stack.
    // This solves some test failures where we happened to end up with a reference being kept alive longer than they should be randomly (reproducible on .NET 10
    // with adjustments to some tests), and for runtimes with a non-precise GC (such as legacy mono, which is used for net48 target on macOS for example).
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static long NoInlineClobber(long a, long b, long c, long d, long e, long f, long g, long h)
    {
        Span<long> s = stackalloc long[128];
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void NotInlined(Action a)
    {
        a();
        NoInlineClobber(0, 0, 0, 0, 0, 0, 0, 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static T NotInlined<T>(Func<T> f)
    {
        T result = f();
        NoInlineClobber(0, 0, 0, 0, 0, 0, 0, 0);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void NotInlined<TState>(TState state, Action<TState> a)
    {
        a(state);
        NoInlineClobber(0, 0, 0, 0, 0, 0, 0, 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static T NotInlined<TState, T>(TState state, Func<TState, T> f)
    {
        T result = f(state);
        NoInlineClobber(0, 0, 0, 0, 0, 0, 0, 0);
        return result;
    }

    private static class WeakListHelpers<T> where T : class
    {
        public static readonly FieldInfo _ImplField
            = typeof(WeakList<T>.Node).GetField("_impl", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly MethodInfo _GetInternalNodeHelperMethod
            = _ImplField.FieldType.GetMethod("GetInternalNodeHelper", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly FieldInfo _InternalNodeField
            = _ImplField.FieldType.GetField("_internalNode", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly FieldInfo _ContainerValuesField
            = typeof(WeakList<T>).GetField("_containerValues", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly FieldInfo? _CwtField
            = _ContainerValuesField.FieldType.GetField("_cwt", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly MethodInfo? _CwtTryGetValueMethod
            = _CwtField?.FieldType.GetMethod("TryGetValue");
    }

    private static class WeakValueDictionaryHelpers<TKey, TValue>
        where TKey : notnull
        where TValue : class
    {
        public static readonly FieldInfo _LookupField
            = typeof(WeakValueDictionary<TKey, TValue>).GetField("_lookup", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly FieldInfo _ContainerValuesField
            = typeof(WeakValueDictionary<TKey, TValue>).GetField("_containerValues", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly Type _NodeType = _LookupField.FieldType.GetGenericArguments()[1];
        public static readonly MethodInfo _TryGetValueMethod
            = _LookupField.FieldType.GetMethod("TryGetValue", [typeof(TKey), _NodeType.MakeByRefType()])!;
        public static readonly FieldInfo _ImplField
            = _NodeType.GetField("_impl", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly MethodInfo _GetInternalNodeHelperMethod
            = _ImplField.FieldType.GetMethod("GetInternalNodeHelper", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly FieldInfo _InternalNodeField
            = _ImplField.FieldType.GetField("_internalNode", BindingFlags.NonPublic | BindingFlags.Instance)!;
        public static readonly FieldInfo? _CwtField
            = _ContainerValuesField.FieldType.GetField("_cwt", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly MethodInfo? _CwtTryGetValueMethod
            = _CwtField?.FieldType.GetMethod("TryGetValue");
    }

    public static object? GetInternalNode<T>(WeakList<T>.Node node) where T : class
    {
        object impl = WeakListHelpers<T>._ImplField.GetValue(node)!;
        return WeakListHelpers<T>._InternalNodeField.GetValue(impl);
    }

    public static object? GetInternalNodeFinalizeHelper<T>(WeakList<T>.Node node) where T : class
    {
        object impl = WeakListHelpers<T>._ImplField.GetValue(node)!;
        return WeakListHelpers<T>._GetInternalNodeHelperMethod.Invoke(impl, []);
    }

    public static object? GetNode<TKey, TValue>(WeakValueDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
        where TValue : class
    {
        object? lookup = WeakValueDictionaryHelpers<TKey, TValue>._LookupField.GetValue(dictionary);

        if (lookup is null)
            return null;

        object?[] args = [key, null];
        return (bool)WeakValueDictionaryHelpers<TKey, TValue>._TryGetValueMethod.Invoke(lookup, args)! ? args[1] : null;
    }

    public static object? GetInternalNode<TKey, TValue>(WeakValueDictionary<TKey, TValue> dictionary, object node)
        where TKey : notnull
        where TValue : class
    {
        object impl = WeakValueDictionaryHelpers<TKey, TValue>._ImplField.GetValue(node)!;
        return WeakValueDictionaryHelpers<TKey, TValue>._InternalNodeField.GetValue(impl);
    }

    public static object? GetInternalNodeFinalizeHelper<TKey, TValue>(WeakValueDictionary<TKey, TValue> dictionary, object node)
        where TKey : notnull
        where TValue : class
    {
        object impl = WeakValueDictionaryHelpers<TKey, TValue>._ImplField.GetValue(node)!;
        return WeakValueDictionaryHelpers<TKey, TValue>._GetInternalNodeHelperMethod.Invoke(impl, []);
    }

    public static bool? CwtContainsValue<T>(WeakList<T> list, T value) where T : class
    {
        if (WeakListHelpers<T>._CwtField is null)
            return null; // .NET path uses DependentHandle; there is no CWT.

        object? cwt = WeakListHelpers<T>._CwtField.GetValue(WeakListHelpers<T>._ContainerValuesField.GetValue(list));

        if (cwt is null)
            return false;

        object?[] args = [value, null];
        return (bool)WeakListHelpers<T>._CwtTryGetValueMethod!.Invoke(cwt, args)!;
    }

    public static bool? CwtContainsValue<TKey, TValue>(WeakValueDictionary<TKey, TValue> dictionary, TValue value)
        where TKey : notnull
        where TValue : class
    {
        if (WeakValueDictionaryHelpers<TKey, TValue>._CwtField is null)
            return null; // .NET path uses DependentHandle; there is no CWT.

        object? cwt = WeakValueDictionaryHelpers<TKey, TValue>._CwtField.GetValue(
            WeakValueDictionaryHelpers<TKey, TValue>._ContainerValuesField.GetValue(dictionary));

        if (cwt is null)
            return false;

        object?[] args = [value, null];
        return (bool)WeakValueDictionaryHelpers<TKey, TValue>._CwtTryGetValueMethod!.Invoke(cwt, args)!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Consume<T>(ref T value) { }
}
