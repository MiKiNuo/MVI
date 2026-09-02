using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Composition;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;

/// <summary>
/// 表示 [MviFeature] 生成装配端到端基准：
/// 通过生成容器装配 Store/ViewModel（含 [MviBind]/[MviCommand] 生成绑定），
/// 测量真实应用形态下的容器解析与带订阅派发，
/// 与手工装配基准对照可分离"生成装配的额外开销"。
/// </summary>
[MemoryDiagnoser]
public class FeatureEndToEndBenchmarks : IDisposable
{
    private GeneratedMviContainer _container = null!;
    private IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect> _store = null!;

    /// <summary>
    /// 从生成容器装配对象图并完成凭证预热，使 CanSubmit 进入可提交态。
    /// </summary>
    /// <returns>表示异步装配过程的任务。</returns>
    [GlobalSetup]
    public async Task SetupAsync()
    {
        _container = new GeneratedMviContainer();

        // 解析 ViewModel 以挂上状态订阅（真实应用的绑定形态）。
        _ = _container.Resolve<BenchLoginViewModel>();
        _store = _container.Resolve<IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect>>();

        await _store.DispatchAsync(new BenchLoginIntent.ChangeUserName("bench"))
            .ConfigureAwait(false);
        await _store.DispatchAsync(new BenchLoginIntent.ChangePassword("pass"))
            .ConfigureAwait(false);
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
    /// 从生成容器解析 Feature Store：走完约 300 个服务比较后命中 Feature 分支。
    /// </summary>
    /// <returns>解析出的状态存储。</returns>
    [Benchmark]
    public IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect> ContainerResolveStore()
    {
        return _container.Resolve<IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect>>();
    }

    /// <summary>
    /// 带 ViewModel 订阅派发单字段变更意图：状态发布后由生成绑定应用到 ViewModel 属性。
    /// </summary>
    /// <returns>表示异步派发过程的任务。</returns>
    [Benchmark]
    public async Task DispatchChangeUserNameWithViewModelSubscribedAsync()
    {
        await _store.DispatchAsync(new BenchLoginIntent.ChangeUserName("bench-user"))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 带 ViewModel 订阅的完整登录回环：提交 → 副作用 → 假认证 → 成功回流，
    /// 每次操作含 4 个意图派发与 1 个副作用执行。
    /// </summary>
    /// <returns>表示完整回环的任务。</returns>
    [Benchmark]
    public async Task DispatchFullLoginLoopWithViewModelSubscribedAsync()
    {
        await _store.DispatchAsync(new BenchLoginIntent.ChangeUserName("bench"))
            .ConfigureAwait(false);
        await _store.DispatchAsync(new BenchLoginIntent.ChangePassword("pass"))
            .ConfigureAwait(false);
        await _store.DispatchAsync(new BenchLoginIntent.Submit())
            .ConfigureAwait(false);
    }
}
