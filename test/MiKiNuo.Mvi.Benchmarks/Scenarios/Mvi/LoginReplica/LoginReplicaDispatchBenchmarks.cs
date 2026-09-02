using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

/// <summary>
/// 表示登录复刻场景手工装配派发基准：
/// 手工 new Store/Reducer/EffectDispatcher/假认证服务（不经容器），
/// 测量真实登录业务形态下的 MVI 管线水位，作为生成装配的对照。
/// </summary>
[MemoryDiagnoser]
public class LoginReplicaDispatchBenchmarks : IDisposable
{
    private MviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect> _store = null!;

    /// <summary>
    /// 手工装配登录复刻 Store 与全部协作对象。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _store = new MviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect>(
            BenchLoginState.Initial,
            new BenchLoginReducer(),
            new BenchLoginEffectDispatcher(new BenchAuthService()));
    }

    /// <summary>
    /// 清理 Store 资源。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    /// <summary>
    /// 释放 Store 资源。
    /// </summary>
    public void Dispose()
    {
        _store?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 派发单字段变更意图：规约 + 状态发布，无副作用路径。
    /// </summary>
    /// <returns>表示异步派发过程的任务。</returns>
    [Benchmark]
    public async Task DispatchChangeUserNameAsync()
    {
        await _store.DispatchAsync(new BenchLoginIntent.ChangeUserName("bench-user"))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 完整登录回环：凭证变更 → 提交 → 副作用 → 假认证 → 成功回流，
    /// 每次操作含 4 个意图派发与 1 个副作用执行。
    /// </summary>
    /// <returns>表示完整回环的任务。</returns>
    [Benchmark]
    public async Task DispatchFullLoginLoopAsync()
    {
        await _store.DispatchAsync(new BenchLoginIntent.ChangeUserName("bench"))
            .ConfigureAwait(false);
        await _store.DispatchAsync(new BenchLoginIntent.ChangePassword("pass"))
            .ConfigureAwait(false);
        await _store.DispatchAsync(new BenchLoginIntent.Submit())
            .ConfigureAwait(false);
    }
}
