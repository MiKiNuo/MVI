using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;
namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;
/// <summary>只测量纯 Mutation 规约，不包含命令或业务层。</summary>
[MemoryDiagnoser]
public class ReducerBenchmarks
{
    private readonly MinimalReducer _minimal = new();
    private readonly BenchLoginReducer _login = new();
    private readonly MinimalMutation _increment = new();
    private readonly BenchLoginMutation _started = new(true);
    /// <summary>规约一个计数转换。</summary>
    [Benchmark(Baseline = true)] public MinimalState MinimalReduceIncrement() => _minimal.Reduce(MinimalState.Initial, _increment);
    /// <summary>规约登录开始转换。</summary>
    [Benchmark] public BenchLoginState LoginReduceStarted() => _login.Reduce(BenchLoginState.Initial, _started);
}
