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
    /// 验证 [MviFeature] 生成容器装配登录复刻对象图：Store、ViewModel 与认证服务均为单例。
    /// </summary>
    [Test]
    public async Task FeatureContainer_Should_AssembleLoginReplicaAsSingletonsAsync()
    {
        GeneratedMviContainer container = new();

        IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect> firstStore =
            container.Resolve<IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect>>();
        IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect> secondStore =
            container.Resolve<IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect>>();
        BenchLoginViewModel firstViewModel = container.Resolve<BenchLoginViewModel>();
        BenchLoginViewModel secondViewModel = container.Resolve<BenchLoginViewModel>();

        await Assert.That(ReferenceEquals(firstStore, secondStore)).IsTrue();
        await Assert.That(ReferenceEquals(firstViewModel, secondViewModel)).IsTrue();
    }

    /// <summary>
    /// 验证登录复刻场景完成完整 MVI 回环：提交 → 副作用 → 假认证 → 成功回流，ViewModel 属性同步。
    /// </summary>
    [Test]
    public async Task FeatureContainer_LoginReplica_Should_CompleteLoginLoopAsync()
    {
        GeneratedMviContainer container = new();
        IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect> store =
            container.Resolve<IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect>>();
        BenchLoginViewModel viewModel = container.Resolve<BenchLoginViewModel>();
        BenchLoginEffectDispatcher dispatcher = container.Resolve<BenchLoginEffectDispatcher>();

        await store.DispatchAsync(new BenchLoginIntent.ChangeUserName("bench"));
        await store.DispatchAsync(new BenchLoginIntent.ChangePassword("pass"));
        await store.DispatchAsync(new BenchLoginIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsFalse();
        await Assert.That(store.CurrentState.ErrorMessage).IsNull();
        await Assert.That(store.CurrentState.DisplayName).IsEqualTo("Bench User");
        await Assert.That(dispatcher.HandledCount).IsEqualTo(1);
        await Assert.That(viewModel.UserName).IsEqualTo("bench");
    }

    /// <summary>
    /// 验证登录复刻场景的失败路径：错误口令回流失败意图，错误消息写回状态。
    /// </summary>
    [Test]
    public async Task FeatureContainer_LoginReplica_Should_ReflectFailureAsync()
    {
        GeneratedMviContainer container = new();
        IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect> store =
            container.Resolve<IMviStore<BenchLoginState, BenchLoginIntent, BenchLoginEffect>>();

        await store.DispatchAsync(new BenchLoginIntent.ChangeUserName("bench"));
        await store.DispatchAsync(new BenchLoginIntent.ChangePassword("fail"));
        await store.DispatchAsync(new BenchLoginIntent.Submit());

        await Assert.That(store.CurrentState.IsBusy).IsFalse();
        await Assert.That(store.CurrentState.ErrorMessage).IsEqualTo("认证失败。");
        await Assert.That(store.CurrentState.CanSubmit).IsTrue();
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
