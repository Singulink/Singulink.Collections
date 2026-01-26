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
#elif NET6_0
        _ = ((Func<object>)([MethodImpl(MethodImplOptions.NoInlining)] () => new long[4000]))();
#elif NETSTANDARD2_1_OR_GREATER
        _ = ((Func<object>)([MethodImpl(MethodImplOptions.NoInlining)] () => new long[5000]))();
#elif NETSTANDARD
        _ = ((Func<object>)([MethodImpl(MethodImplOptions.NoInlining)] () => new long[6000]))();
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

        AddJob(baseJob
            .WithRuntime(CoreRuntime.Core60)
            .WithId(".NET 6.0"));
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

        WithOrderer(new JobOrderer(".NET 10.0", ".NET 9.0", ".NET 8.0", ".NET 6.0", ".NET 10.0 (.NET Standard 2.1)", ".NET 10.0 (.NET Standard 2.0)", ".NET Framework 4.8"));

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
    [Params(0, 1, 3, 10, 30, 100, 300, 1000, 3000, 10000)]
    public int N { get; set; }

    private readonly Random _random = new();
    private ConcurrentWeakList<object> _list = null!;
    private readonly object _value = new();
    private object[] _values = null!;
    private ConcurrentWeakList<object>.Node[] _nodes = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _list = new();
        _values = [.. Enumerable.Range(0, N).Select(_ => new object())];
        _nodes = new ConcurrentWeakList<object>.Node[N];
        int i = 0;
        foreach (object x in _values) _nodes[i++] = _list.AddLast(x);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _list.Dispose();
        GC.KeepAlive(_values);
        GC.KeepAlive(_value);
    }

    [Benchmark]
    public void AddRemoveNodeAtStart()
    {
        ConcurrentWeakList<object> list = _list;
        var node = list.AddFirst(_value);
        list.Remove(node);
    }

    [Benchmark]
    public void AddRemoveNodeAtEnd()
    {
        ConcurrentWeakList<object> list = _list;
        var node = list.AddLast(_value);
        list.Remove(node);
    }

    [Benchmark]
    public void AddRemoveNodeRandomPosition()
    {
        ConcurrentWeakList<object> list = _list;
        var node = list.UnsafeInsertAt(_value, _random.Next(0, N + 1));
        list.Remove(node);
    }

    [Benchmark]
    public void AddRemoveNodeRandomPositionEach()
    {
        ConcurrentWeakList<object> list = _list;
        int n = N;
        if (n == 0) return;
        int idx = _random.Next(0, n);
#if NET
        static ref T GetArrayDataReference<T>(T[] array) => ref MemoryMarshal.GetArrayDataReference(array);
#else
        static ref T GetArrayDataReference<T>(T[] array) => ref MemoryMarshal.GetReference((ReadOnlySpan<T>)array);
#endif
        ref var nodeSlot = ref Unsafe.Add(ref GetArrayDataReference(_nodes), (uint)idx)!;
        object oldValue = Unsafe.Add(ref GetArrayDataReference(_values), (uint)idx)!;
        list.Remove(nodeSlot);
        nodeSlot = list.UnsafeInsertAt(oldValue, _random.Next(0, n));
    }

    [Benchmark]
    public void Enumerate()
    {
        ConcurrentWeakList<object> list = _list;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
        foreach (object x in list)
#pragma warning restore IDE0059 // Unnecessary assignment of a value
        {
        }
    }

    [Benchmark]
    public void CreateAddNodesClearDispose()
    {
        ConcurrentWeakList<object> list = new();

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
    public void CreateAddPreexistingNodesClearDispose()
    {
        ConcurrentWeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object value = _value;
        if (n > 0) _ = nodes[n - 1];

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(value);
        }

        list.Clear();

        GC.KeepAlive(value);

        list.Dispose();
    }

    [Benchmark]
    public void CreateAddNodesDispose()
    {
        ConcurrentWeakList<object> list = new();

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
    public void CreateAddPreexistingNodesDispose()
    {
        ConcurrentWeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object value = _value;
        if (n > 0) _ = nodes[n - 1];

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(value);
        }

        list.Dispose();

        GC.KeepAlive(value);
    }

    [Benchmark]
    public void CreateAddNodesGCAutoClean()
    {
        ConcurrentWeakList<object> list = new();

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
    public void CreateAddPreexistingNodesGCAutoClean()
    {
        ConcurrentWeakList<object> list = new();

        int n = N;
        var nodes = _nodes;
        object value = _value;
        if (n > 0) _ = nodes[n - 1];

        for (int i = 0; i < n; i++)
        {
            nodes[i] = list.AddLast(value);
        }

        GC.KeepAlive(value);
    }

    [Benchmark]
    public void CreateAddSelfGCAutoClean()
    {
        ConcurrentWeakList<object> list = new();

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
#endif
}
