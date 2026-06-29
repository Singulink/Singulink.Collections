using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Singulink.Collections;

// Note: this project must be run like: dotnet build -c NS20 && dotnet build -c NS21 && dotnet run -c Release -f net10.0 -p:DryRun=true && dotnet run -c Release -f net10.0 -p:DryRun=true && dotnet run -c Release -f net10.0

BenchmarkRunner.Run<Benchs>(args: args);

/*
    Note: the setup here is somewhat finicky to get the .NET Standard versions being used.
    When making changes to the config, add the following code (adjusted as required) to the start of ManualAdd and run just 1 benchmark at 1 size
        (e.g., AddRemoveNodeAtStart with N=1) with Job.ShortRun instead of Job.Default:

#pragma warning disable SA1134
#if NET10_0
        _ = ((Func<object>)([MethodImpl(MethodImplOptions.NoInlining)] () => new long[1000]))();
#elif NET9_0
        _ = ((Func<object>)([MethodImpl(MethodImplOptions.NoInlining)] () => new long[2000]))();
#elif NET8_0
        _ = ((Func<object>)([MethodImpl(MethodImplOptions.NoInlining)] () => new long[3000]))();
#elif NETSTANDARD2_1_OR_GREATER
        _ = ((Func<object>)([MethodImpl(MethodImplOptions.NoInlining)] () => new long[4000]))();
#elif NETSTANDARD
        _ = ((Func<object>)([MethodImpl(MethodImplOptions.NoInlining)] () => new long[5000]))();
#endif

    This allows validating that it is indeed running with the correct build of the library, as the allocated memory is substantially larger than what would
    occur naturally & differs between the target frameworks.
*/

public class MyConfig : ManualConfig
{
    public MyConfig()
    {
#if DRY
        var baseJob = Job.Dry;
#else
        var baseJob = Job.Default;
#endif

        AddJob(baseJob
            .WithRuntime(CoreRuntime.Core10_0)
            .WithId(".NET 10.0")
#if DRY
            .WithMsBuildArguments("/p:DryRun=true")
#endif
            .AsBaseline());

#if !DRY
        AddJob(baseJob
            .WithRuntime(CoreRuntime.Core90)
            .WithId(".NET 9.0"));

        AddJob(baseJob
            .WithRuntime(CoreRuntime.Core80)
            .WithId(".NET 8.0"));
#endif

        AddJob(baseJob
            .WithRuntime(CoreRuntime.Core10_0)
            .WithId(".NET 10.0 (.NET Standard 2.1)")
            .WithCustomBuildConfiguration("NS21")
#if DRY
            .WithMsBuildArguments("/p:Configurations=NS21", "/p:Optimize=true", "/p:Deterministic=true", "/p:DryRun=true"));
#else
            .WithMsBuildArguments("/p:Configurations=NS21", "/p:Optimize=true", "/p:Deterministic=true"));
#endif

        AddJob(baseJob
            .WithRuntime(CoreRuntime.Core10_0)
            .WithId(".NET 10.0 (.NET Standard 2.0)")
            .WithCustomBuildConfiguration("NS20")
#if DRY
            .WithMsBuildArguments("/p:Configurations=NS20", "/p:Optimize=true", "/p:Deterministic=true", "/p:DryRun=true"));
#else
            .WithMsBuildArguments("/p:Configurations=NS20", "/p:Optimize=true", "/p:Deterministic=true"));
#endif

#if !DRY
#if NET
        if (OperatingSystem.IsWindows())
#endif
        {
            AddJob(baseJob
                .WithRuntime(ClrRuntime.Net48)
                .WithId(".NET Framework 4.8"));
        }
#endif

        WithOrderer(new JobOrderer(".NET 10.0", ".NET 9.0", ".NET 8.0", ".NET 10.0 (.NET Standard 2.1)", ".NET 10.0 (.NET Standard 2.0)", ".NET Framework 4.8"));

        HideColumns(Column.Runtime);
        HideColumns(Column.Arguments);
        HideColumns(Column.BuildConfiguration);

        WithOption(ConfigOptions.KeepBenchmarkFiles, true);
    }
}

public class JobOrderer : DefaultOrderer
{
    private readonly string[] _order;

    public JobOrderer(params string[] order)
    {
        _order = order;
    }

    protected override IEnumerable<BenchmarkCase> GetSummaryOrderForGroup(System.Collections.Immutable.ImmutableArray<BenchmarkCase> benchmarksCase, Summary summary)
    {
        return benchmarksCase.OrderBy((x) =>
        {
            int index = Array.IndexOf(_order, x.Job.Id);
            return index < 0 ? int.MaxValue : index;
        });
    }
}

[MemoryDiagnoser]
[Config(typeof(MyConfig))]
public class Benchs
{
#if DRY
    [Benchmark]
    public void DryRun() { }
#else
    [Params(0, 1, 3, 10, 100, 1000, 10000)]
    public int N { get; set; }

    // A key deliberately outside the pre-populated range (0..N-1) so single-op dictionary benchmarks don't disturb the existing entries.
    private const int SpareKey = -1;

    private readonly Random _random = new();
    private WeakList<object> _list = null!;
    private WeakValueDictionary<int, object> _dictionary = null!;
    private readonly object _value = new();
    private object[] _values = null!;
    private WeakList<object>.Node[] _nodes = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _list = new();
        _values = [.. Enumerable.Range(0, N).Select(_ => new object())];
        _nodes = new WeakList<object>.Node[N];
        int i = 0;

        foreach (object x in _values)
            _nodes[i++] = _list.AddLast(x);

        _dictionary = [];

        for (int k = 0; k < N; k++)
            _dictionary.TryAdd(k, _values[k]);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _list.Dispose();
        GC.KeepAlive(_values);
        GC.KeepAlive(_value);
    }

    [Benchmark]
    public void WeakList_AddRemoveNodeAtStart()
    {
        WeakList<object> list = _list;
        var node = list.AddFirst(_value);
        list.Remove(node);
    }

    [Benchmark]
    public void WeakList_AddRemoveNodeAtEnd()
    {
        WeakList<object> list = _list;
        var node = list.AddLast(_value);
        list.Remove(node);
    }

#if NET
    private static ref T GetArrayDataReference<T>(T[] array) => ref MemoryMarshal.GetArrayDataReference(array);
#else
    private static ref T GetArrayDataReference<T>(T[] array) => ref MemoryMarshal.GetReference((ReadOnlySpan<T>)array);
#endif

    [Benchmark]
    public void WeakList_AddRemoveNodeRandomPosition()
    {
        // Note: we're using unsafe code here to ensure we're not measuring the array access bounds checks also.
        WeakList<object> list = _list;
        int idx = _random.Next(0, N + 1);
        WeakList<object>.Node? node;
        ref var node0 = ref GetArrayDataReference(_nodes);

        if (N == 0)
            node = _random.Next(2) == 0 ? list.AddFirst(_value) : list.AddLast(_value);
        else if (idx == N)
            node = list.AddAfter(Unsafe.Add(ref node0, (uint)(N - 1)), _value);
        else if (idx == 0)
            node = list.AddBefore(node0, _value);
        else if (_random.Next(2) == 0)
            node = list.AddBefore(Unsafe.Add(ref node0, (uint)idx), _value);
        else
            node = list.AddAfter(Unsafe.Add(ref node0, (uint)(idx - 1)), _value);

        list.Remove(node);
    }

    [Benchmark]
    public void WeakList_AddRemoveNodeRandomPositionEach()
    {
        // Note: we're using unsafe code here to ensure we're not measuring the array access bounds checks also.
        // Note: we're not preserving the order properly in _nodes for this method, but that is fine for this benchmark (others will re-instantiate it).
        WeakList<object> list = _list;
        int n = N;

        if (n == 0)
            return;

        int idx = _random.Next(0, n);
        ref var nodeSlot = ref Unsafe.Add(ref GetArrayDataReference(_nodes), (uint)idx)!;
        object oldValue = Unsafe.Add(ref GetArrayDataReference(_values), (uint)idx)!;

        if (n == 1)
        {
            // Only one node in the list, so there is no other node to position relative to; re-add it at the start/end instead.
            list.Remove(nodeSlot);
            nodeSlot = _random.Next(2) == 0 ? list.AddFirst(oldValue) : list.AddLast(oldValue);
            return;
        }

        int otherNodeIndex = _random.Next(0, n - 1);
        otherNodeIndex += otherNodeIndex >= idx ? 1 : 0; // This particular construction is handled by roslyn to not branch, which reduces potential variation.
        var otherNode = Unsafe.Add(ref GetArrayDataReference(_nodes), (uint)otherNodeIndex);
        list.Remove(nodeSlot);

        if (_random.Next(2) == 0)
            nodeSlot = list.AddBefore(otherNode, oldValue);
        else
            nodeSlot = list.AddAfter(otherNode, oldValue);
    }

    [Benchmark]
    public void WeakList_Enumerate()
    {
        WeakList<object> list = _list;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
        foreach (object x in list)
#pragma warning restore IDE0059 // Unnecessary assignment of a value
        {
        }
    }

    [Benchmark]
    public void WeakList_CreateAddNodesClearDispose()
    {
        WeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object[] values = _values;

        if (n > 0)
        {
            _ = nodes[n - 1];
            _ = values[n - 1];
        }

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(values[i] = new object());
        }

        list.Clear();

        GC.KeepAlive(values);

        list.Dispose();
    }

    [Benchmark]
    public void WeakList_CreateAddPreexistingNodesClearDispose()
    {
        WeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object value = _value;

        if (n > 0)
            _ = nodes[n - 1];

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(value);
        }

        list.Clear();

        GC.KeepAlive(value);

        list.Dispose();
    }

    [Benchmark]
    public void WeakList_CreateAddNodesDispose()
    {
        WeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object[] values = _values;

        if (n > 0)
        {
            _ = nodes[n - 1];
            _ = values[n - 1];
        }

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(values[i] = new object());
        }

        list.Dispose();

        GC.KeepAlive(values);
    }

    [Benchmark]
    public void WeakList_CreateAddPreexistingNodesDispose()
    {
        WeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object value = _value;

        if (n > 0)
            _ = nodes[n - 1];

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(value);
        }

        list.Dispose();

        GC.KeepAlive(value);
    }

    [Benchmark]
    public void WeakList_CreateAddNodesGCAutoClean()
    {
        WeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object[] values = _values;

        if (n > 0)
        {
            _ = nodes[n - 1];
            _ = values[n - 1];
        }

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(values[i] = new object());
        }

        GC.KeepAlive(values);
    }

    [Benchmark]
    public void WeakList_CreateAddPreexistingNodesGCAutoClean()
    {
        WeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object value = _value;

        if (n > 0)
            _ = nodes[n - 1];

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(value);
        }

        GC.KeepAlive(value);
    }

    [Benchmark]
    public void WeakList_CreateAddSelfGCAutoClean()
    {
        WeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object[] values = _values;

        if (n > 0)
        {
            _ = nodes[n - 1];
            _ = values[n - 1];
        }

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(values[i] = new object());
        }

        GC.KeepAlive(values);
    }

    [Benchmark]
    public object? WeakValueDictionary_TryGetValue()
    {
        if (N == 0)
            return null;

        int idx = _random.Next(0, N);
        return _dictionary.TryGetValue(idx, out object result) ? result : null;
    }

    [Benchmark]
    public object? WeakValueDictionary_TryGetValueFailing()
    {
        return _dictionary.TryGetValue(SpareKey, out object result) ? result : null;
    }

    [Benchmark]
    public object? WeakValueDictionary_IndexerSetUnset()
    {
        var dictionary = _dictionary;
        dictionary[SpareKey] = _value;
        return dictionary.Remove(SpareKey, out object result) ? result : null;
    }

    [Benchmark]
    public object? WeakValueDictionary_TryAddRemove()
    {
        var dictionary = _dictionary;
        dictionary.TryAdd(SpareKey, _value);
        return dictionary.Remove(SpareKey, out object result) ? result : null;
    }

    [Benchmark]
    public void WeakValueDictionary_CreateAddClearGCAutoClean()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        int n = N;
        object[] values = _values;

        if (n > 0)
            _ = values[n - 1];

        for (int i = 0; i < n; i++)
        {
            dictionary.TryAdd(i, values[i] = new object());
        }

        dictionary.Clear();

        GC.KeepAlive(values);
    }

    [Benchmark]
    public void WeakValueDictionary_CreateAddNoClearGCAutoClean()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        int n = N;
        object[] values = _values;

        if (n > 0)
            _ = values[n - 1];

        for (int i = 0; i < n; i++)
        {
            dictionary.TryAdd(i, values[i] = new object());
        }

        GC.KeepAlive(values);
    }

    [Benchmark]
    public void WeakValueDictionary_CreateAddClearDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        int n = N;
        object[] values = _values;

        if (n > 0)
            _ = values[n - 1];

        for (int i = 0; i < n; i++)
        {
            dictionary.TryAdd(i, values[i] = new object());
        }

        dictionary.Clear();

        GC.KeepAlive(values);

        dictionary.Dispose();
    }

    [Benchmark]
    public void WeakValueDictionary_CreateAddDispose()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        int n = N;
        object[] values = _values;

        if (n > 0)
            _ = values[n - 1];

        for (int i = 0; i < n; i++)
        {
            dictionary.TryAdd(i, values[i] = new object());
        }

        GC.KeepAlive(values);

        dictionary.Dispose();
    }

    [Benchmark]
    public void WeakValueDictionary_Enumerate()
    {
        var dictionary = _dictionary;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
        foreach (object x in dictionary)
#pragma warning restore IDE0059 // Unnecessary assignment of a value
        {
        }
    }
#endif
}
