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
        public static readonly FieldInfo? _CwtWrapperField
            = _ContainerValuesField.FieldType.GetField("_cwt", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly Type[]? _CwtWrapperGenericArgs = _CwtWrapperField?.FieldType.GetGenericArguments();
        public static readonly MethodInfo? _CwtWrapperHasAnyMethod
            = _CwtWrapperField?.FieldType.GetMethod("HasAny", [_CwtWrapperGenericArgs![0]]);
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
        public static readonly FieldInfo? _CwtWrapperField
            = _ContainerValuesField.FieldType.GetField("_cwt", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly Type[]? _CwtWrapperGenericArgs = _CwtWrapperField?.FieldType.GetGenericArguments();
        public static readonly MethodInfo? _CwtWrapperHasAnyMethod
            = _CwtWrapperField?.FieldType.GetMethod("HasAny", [_CwtWrapperGenericArgs![0]]);
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

    // NOTE: this method is dangerous to call if there can be concurrent mutations to the collection; hence you should always call it with a ForceGC beforehand
    // and GC.KeepAlive's afterwards for any values you want to keep alive.
    public static bool? CwtContainsValue<T>(WeakList<T> list, T value) where T : class
    {
        if (WeakListHelpers<T>._CwtWrapperField is null)
            return null; // .NET path uses DependentHandle; there is no wrapper.

        object? cwtWrapper = WeakListHelpers<T>._CwtWrapperField.GetValue(WeakListHelpers<T>._ContainerValuesField.GetValue(list));

        if (cwtWrapper is null)
            return false;

        return (bool)WeakListHelpers<T>._CwtWrapperHasAnyMethod!.Invoke(cwtWrapper, [value])!;
    }

    // This API does not have the same correctness concerns as the WeakList version, since WVD is non-locking and thus it uses the lock on the wrapper for all
    // mutations; however, to get useful results you likely want to use it under similar conditions.
    public static bool? CwtContainsValue<TKey, TValue>(WeakValueDictionary<TKey, TValue> dictionary, TValue value)
        where TKey : notnull
        where TValue : class
    {
        if (WeakValueDictionaryHelpers<TKey, TValue>._CwtWrapperField is null)
            return null; // .NET path uses DependentHandle; there is no wrapper.

        object? cwtWrapper = WeakValueDictionaryHelpers<TKey, TValue>._CwtWrapperField.GetValue(
            WeakValueDictionaryHelpers<TKey, TValue>._ContainerValuesField.GetValue(dictionary));

        if (cwtWrapper is null)
            return false;

        return (bool)WeakValueDictionaryHelpers<TKey, TValue>._CwtWrapperHasAnyMethod!.Invoke(cwtWrapper, [value])!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Consume<T>(ref T value) { }
}
