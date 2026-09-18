using MiKiNuo.Mvi.Application.MVI.Composition;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Composition;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示基准项目 MVI 场景的冒烟测试：
/// 验证最小场景（手工装配）与登录复刻场景（[MviFeature] 生成装配）在测量前真实可用。
/// </summary>
public sealed class BenchmarkMviScenarioSmokeTests
{
    /// <summary>
    /// 验证最小 Store 派发增量意图后状态计数正确推进。
    /// </summary>
    [Test]
    public async Task MinimalStore_Should_DispatchIncrement_And_AdvanceCounterAsync()
    {
        using MviStore<MinimalState, MinimalIntent, MinimalEffect> store = CreateMinimalStore(0, 0);

        await store.DispatchAsync(new MinimalIntent.Increment());
        await store.DispatchAsync(new MinimalIntent.Increment());
        await store.DispatchAsync(new MinimalIntent.Increment());

        await Assert.That(store.CurrentState.Counter).IsEqualTo(3);
    }

    /// <summary>
    /// 验证 Reducer 按配置的副作用数量产出副作用，且分发器逐个执行。
    /// </summary>
    [Test]
    public async Task MinimalStore_Should_DispatchEffects_ByConfiguredCountAsync()
    {
        foreach (int effectCount in new[] { 0, 1, 4 })
        {
            MinimalEffectDispatcher dispatcher = new();
            using MviStore<MinimalState, MinimalIntent, MinimalEffect> store = new(
                MinimalState.Initial, new MinimalReducer(effectCount), dispatcher);

            await store.DispatchAsync(new MinimalIntent.EmitNops());

            await Assert.That(dispatcher.HandledCount).IsEqualTo(effectCount);
            await Assert.That(store.CurrentState.Counter).IsEqualTo(1);
        }
    }

    /// <summary>
    /// 验证 8 层无操作中间件全链穿透后规约仍正确执行，且每层恰好被调用一次。
    /// </summary>
    [Test]
    public async Task MinimalStore_Should_RunEightLayerMiddlewareChainAsync()
    {
        List<NopMiddleware> trackedMiddlewares = new();
        for (int index = 0; index < 8; index++)
        {
            trackedMiddlewares.Add(new NopMiddleware());
        }

        List<IMviMiddleware<MinimalState, MinimalIntent, MinimalEffect>> middlewares = new();
        foreach (NopMiddleware middleware in trackedMiddlewares)
        {
            middlewares.Add(middleware);
        }

        using MviStore<MinimalState, MinimalIntent, MinimalEffect> store = new(
            MinimalState.Initial, new MinimalReducer(0), new MinimalEffectDispatcher(), middlewares);

        await store.DispatchAsync(new MinimalIntent.Increment());

        await Assert.That(store.CurrentState.Counter).IsEqualTo(1);
        foreach (NopMiddleware middleware in trackedMiddlewares)
        {
            await Assert.That(middleware.InvocationCount).IsEqualTo(1);
        }
    }

    /// <summary>
    /// 验证多线程并发派发在 SemaphoreSlim 派发门下不丢失意图，总计数精确等于派发总数。
    /// </summary>
    [Test]
    public async Task MinimalStore_Should_SupportConcurrentDispatchAsync()
    {
        MviStore<MinimalState, MinimalIntent, MinimalEffect> store = CreateMinimalStore(0, 0);
        const int ThreadCount = 4;
        const int DispatchesPerThread = 25;

        Task[] workers = new Task[ThreadCount];
        for (int index = 0; index < ThreadCount; index++)
        {
            workers[index] = DispatchIncrementManyAsync(store, DispatchesPerThread);
        }

        await Task.WhenAll(workers);
        await Assert.That(store.CurrentState.Counter).IsEqualTo(ThreadCount * DispatchesPerThread);

        store.Dispose();
    }

    /// <summary>
    /// 验证 Reducer 纯函数直接调用：增量意图产出新状态，守卫关闭时提交意图被拦截。
    /// </summary>
    [Test]
    public async Task BenchLoginReducer_Should_ReducePurely_And_GuardSubmitAsync()
    {
        BenchLoginReducer reducer = new();
        MviReduceResult<BenchLoginState, BenchLoginEffect> busyResult = reducer.Reduce(
            BenchLoginState.Initial with { UserName = "bench", Password = "pass", CanSubmit = true },
            new BenchLoginIntent.Submit());

        await Assert.That(busyResult.State.IsBusy).IsTrue();

        MviReduceResult<BenchLoginState, BenchLoginEffect> guardedResult = reducer.Reduce(
            BenchLoginState.Initial,
            new BenchLoginIntent.Submit());

        await Assert.That(ReferenceEquals(guardedResult.State, BenchLoginState.Initial)).IsTrue();
    }

    /// <summary>
    /// 验证 [MviFeature] 实例工厂每次创建独立的登录复刻对象图。
    /// </summary>
    [Test]
    public async Task FeatureContainer_Should_AssembleIndependentLoginReplicaInstancesAsync()
    {
        GeneratedMviContainer container = new();
        using MviCompositionScope scope = new();

        MviFeatureInstance<BenchLoginViewModel> first =
            await container.CreateBenchLoginInstanceAsync(scope.CreateEndpoint(Guid.NewGuid()));
        MviFeatureInstance<BenchLoginViewModel> second =
            await container.CreateBenchLoginInstanceAsync(scope.CreateEndpoint(Guid.NewGuid()));

        await Assert.That(ReferenceEquals(first, second)).IsFalse();
        await Assert.That(ReferenceEquals(first.ViewModel, second.ViewModel)).IsFalse();
        await Assert.That(first.Id).IsNotEqualTo(second.Id);

        await first.DisposeAsync();
        await second.DisposeAsync();
    }

    /// <summary>
    /// 验证登录复刻场景完成完整 MVI 回环：绑定输入 → 提交命令 → 假认证 → 成功回流，ViewModel 属性同步。
    /// </summary>
    [Test]
    public async Task FeatureContainer_LoginReplica_Should_CompleteLoginLoopAsync()
    {
        GeneratedMviContainer container = new();
        using MviCompositionScope scope = new();
        MviFeatureInstance<BenchLoginViewModel> instance =
            await container.CreateBenchLoginInstanceAsync(scope.CreateEndpoint(Guid.NewGuid()));
        BenchLoginViewModel viewModel = instance.ViewModel;

        viewModel.UserName = "bench";
        viewModel.Password = "pass";
        await WaitForConditionAsync(() => viewModel.CanSubmit);
        await viewModel.SubmitCommand.ExecuteAsync(null);

        await Assert.That(viewModel.IsBusy).IsFalse();
        await Assert.That(viewModel.ErrorMessage).IsNull();

        await instance.DisposeAsync();
    }

    /// <summary>
    /// 验证登录复刻场景的失败路径：错误口令回流失败意图，错误消息写回状态。
    /// </summary>
    [Test]
    public async Task FeatureContainer_LoginReplica_Should_ReflectFailureAsync()
    {
        GeneratedMviContainer container = new();
        using MviCompositionScope scope = new();
        MviFeatureInstance<BenchLoginViewModel> instance =
            await container.CreateBenchLoginInstanceAsync(scope.CreateEndpoint(Guid.NewGuid()));
        BenchLoginViewModel viewModel = instance.ViewModel;

        viewModel.UserName = "bench";
        viewModel.Password = "fail";
        await WaitForConditionAsync(() => viewModel.CanSubmit);
        await viewModel.SubmitCommand.ExecuteAsync(null);

        await Assert.That(viewModel.IsBusy).IsFalse();
        await Assert.That(viewModel.ErrorMessage).IsEqualTo("认证失败。");
        await Assert.That(viewModel.CanSubmit).IsTrue();

        await instance.DisposeAsync();
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }
    }

    private static MviStore<MinimalState, MinimalIntent, MinimalEffect> CreateMinimalStore(
        int effectCount,
        int middlewareCount)
    {
        List<IMviMiddleware<MinimalState, MinimalIntent, MinimalEffect>> middlewares = new();
        for (int index = 0; index < middlewareCount; index++)
        {
            middlewares.Add(new NopMiddleware());
        }

        return new MviStore<MinimalState, MinimalIntent, MinimalEffect>(
            MinimalState.Initial,
            new MinimalReducer(effectCount),
            new MinimalEffectDispatcher(),
            middlewares);
    }

    private static async Task DispatchIncrementManyAsync(
        MviStore<MinimalState, MinimalIntent, MinimalEffect> store,
        int count)
    {
        for (int index = 0; index < count; index++)
        {
            await store.DispatchAsync(new MinimalIntent.Increment());
        }
    }
}
