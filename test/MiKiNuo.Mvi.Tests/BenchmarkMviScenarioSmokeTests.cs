using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;
/// <summary>基准场景必须能完成当前内核上的实际操作。</summary>
public sealed class BenchmarkMviScenarioSmokeTests
{
    /// <summary>所有并发参数均完整执行两批，清理后派发必须失败。</summary>
    /// <param name="threadCount">本批并发 worker 数。</param>
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    public async Task ConcurrentBenchmarkCompletesEveryBatchAsync(int threadCount)
    {
        await using ConcurrentDispatchBenchmarks benchmark = new() { ThreadCount = threadCount };
        benchmark.Setup();
        await Assert.That(await benchmark.ConcurrentDispatchBatchAsync()).IsEqualTo(threadCount * 100);
        await Assert.That(await benchmark.ConcurrentDispatchBatchAsync()).IsEqualTo(threadCount * 100);
        await benchmark.CleanupAsync();
        await benchmark.DisposeAsync();
        try { await benchmark.ConcurrentDispatchBatchAsync(); }
        catch (ObjectDisposedException) { return; }
        throw new InvalidOperationException("基准清理后不应继续接纳派发。");
    }

    /// <summary>初始化前以及重复异步清理均安全。</summary>
    [Test] public async Task ConcurrentBenchmarkCleanupBeforeSetupIsSafeAsync()
    {
        await using ConcurrentDispatchBenchmarks benchmark = new();
        await benchmark.CleanupAsync();
        await benchmark.DisposeAsync();
    }

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
