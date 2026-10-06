using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>基准场景必须能完成当前内核上的实际操作。</summary>
public sealed class BenchmarkMviScenarioSmokeTests
{
    /// <summary>最小 Handler 和纯 Reducer 对账。</summary>
    [Test] public async Task MinimalHandlerScenarioRunsAsync()
    {
        MinimalHandler handler = new(4);
        await using var store = new MviStore<MinimalState, MinimalIntent>(MinimalState.Initial, handler, new MinimalReducer());
        await store.DispatchAsync(new MinimalIntent.EmitNops());
        await Assert.That(store.CurrentState.Counter).IsEqualTo(1);
        await Assert.That(handler.HandledCount).IsEqualTo(4);
    }
    /// <summary>生成命令经过真实 Store 完成并清除敏感草稿。</summary>
    [Test] public async Task LoginCommandScenarioRunsAsync()
    {
        await using var store = new MviStore<BenchLoginState, BenchLoginIntent>(BenchLoginState.Initial, new BenchLoginHandler(), new BenchLoginReducer());
        using var model = new BenchLoginViewModel(store) { UserName = "user", Password = "password" };
        await model.SubmitCommand.ExecuteAsync(null);
        await Assert.That(store.CurrentState.Completed).IsEqualTo(1);
        await Assert.That(model.Password).IsEqualTo("");
    }
    /// <summary>基准清理与异步释放共用所有权，重复调用不会再次开启 Store。</summary>
    [Test] public async Task LoginBenchmarkCleanupClosesStoreAsync()
    {
        await using LoginReplicaDispatchBenchmarks benchmark = new();
        benchmark.Setup();
        await benchmark.DirectAsync();
        await benchmark.CommandAsync();
        await benchmark.CleanupAsync();
        await benchmark.DisposeAsync();
        try { await benchmark.DirectAsync(); }
        catch (ObjectDisposedException) { return; }
        throw new InvalidOperationException("基准清理后 Store 不应继续接纳派发。");
    }
    /// <summary>初始化尚未执行时清理仍可安全完成，兼容初始化失败后的清理路径。</summary>
    [Test] public async Task LoginBenchmarkCleanupBeforeSetupIsSafeAsync()
    {
        await using LoginReplicaDispatchBenchmarks benchmark = new();
        await benchmark.CleanupAsync();
        await benchmark.DisposeAsync();
    }
}
