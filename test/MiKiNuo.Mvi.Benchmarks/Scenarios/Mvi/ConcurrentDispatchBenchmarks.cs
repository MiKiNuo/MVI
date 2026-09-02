using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示并发派发基准：
/// 1/2/4/8 线程同时对同一 Store 派发无副作用意图，
/// 验证 SemaphoreSlim 派发门（ADR-0002：锁只护 Reduce）在争抢下的吞吐。
/// 每次操作为一整批（线程数 × 100 个派发），报告值除以批量即得单派发成本。
/// </summary>
[MemoryDiagnoser]
public class ConcurrentDispatchBenchmarks : IDisposable
{
    private const int DispatchesPerThread = 100;

    private MviStore<MinimalState, MinimalIntent, MinimalEffect> _store = null!;

    /// <summary>
    /// 获取或设置并发争抢派发门的线程数。
    /// </summary>
    [Params(1, 2, 4, 8)]
    public int ThreadCount { get; set; }

    /// <summary>
    /// 构建无中间件、无副作用的共享 Store。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _store = new MviStore<MinimalState, MinimalIntent, MinimalEffect>(
            MinimalState.Initial,
            new MinimalReducer(0),
            new MinimalEffectDispatcher());
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
    /// 并发派发一整批意图并等待全部完成。
    /// </summary>
    /// <returns>表示整批派发过程的任务。</returns>
    [Benchmark]
    public async Task ConcurrentDispatchBatchAsync()
    {
        Task[] workers = new Task[ThreadCount];
        for (int index = 0; index < ThreadCount; index++)
        {
            workers[index] = DispatchIncrementManyAsync();
        }

        await Task.WhenAll(workers).ConfigureAwait(false);
    }

    /// <summary>
    /// 单工作线程串行派发 100 个意图。
    /// </summary>
    /// <returns>表示串行派发过程的任务。</returns>
    private async Task DispatchIncrementManyAsync()
    {
        for (int index = 0; index < DispatchesPerThread; index++)
        {
            await _store.DispatchAsync(new MinimalIntent.Increment()).ConfigureAwait(false);
        }
    }
}
