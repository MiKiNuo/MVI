using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示 Reducer 纯函数基准：测量规约本身（含 record 状态重建）的底噪，
/// 作为 Store 派发基准的对照下限。
/// </summary>
[MemoryDiagnoser]
public class ReducerBenchmarks
{
    private readonly MinimalReducer _minimalReducer = new(0);
    private readonly MinimalState _minimalState = MinimalState.Initial;
    private readonly MinimalIntent.Increment _increment = new();
    private readonly BenchLoginReducer _loginReducer = new();
    private readonly BenchLoginState _loginState = BenchLoginState.Initial with
    {
        UserName = "bench",
        Password = "pass",
        CanSubmit = true,
    };
    private readonly BenchLoginIntent.ChangeUserName _changeUserName = new("bench-user");

    /// <summary>
    /// 最小场景规约：单字段 record 重建，无副作用。
    /// </summary>
    /// <returns>规约结果。</returns>
    [Benchmark(Baseline = true)]
    public MviReduceResult<MinimalState, MinimalEffect> MinimalReduceIncrement()
    {
        return _minimalReducer.Reduce(_minimalState, _increment);
    }

    /// <summary>
    /// 登录复刻场景规约：六字段 record 重建加守卫计算。
    /// </summary>
    /// <returns>规约结果。</returns>
    [Benchmark]
    public MviReduceResult<BenchLoginState, BenchLoginEffect> LoginReplicaReduceChangeUserName()
    {
        return _loginReducer.Reduce(_loginState, _changeUserName);
    }
}
