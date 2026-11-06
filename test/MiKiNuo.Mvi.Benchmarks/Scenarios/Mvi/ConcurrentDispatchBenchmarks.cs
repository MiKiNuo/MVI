using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示并发派发基准：
/// 1/2/4/8 个 Task.Run worker 全部抵达异步起跑门后向同一 Store 派发。
/// 每次测量包含任务调度、起跑协调、派发完成和计数校验，非纯锁竞争成本。
/// 每批为 worker 数 × 100 次派发；worker 数不保证同时运行的物理线程数。
/// </summary>
[MemoryDiagnoser]
public class ConcurrentDispatchBenchmarks : IAsyncDisposable
{
    private const int DispatchesPerThread = 100;

    private MviStore<MinimalState, MinimalIntent> _store = null!;

    /// <summary>
    /// 获取或设置并发 worker 数（保留 ThreadCount 参数名）。
    /// </summary>
    [Params(1, 2, 4, 8)]
    public int ThreadCount { get; set; }

    /// <summary>
    /// 构建无中间件、无副作用的共享 Store。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _store = new MviStore<MinimalState, MinimalIntent>(
            MinimalState.Initial,
            new MinimalHandler(),
            new MinimalReducer());
    }

    /// <summary>
    /// 清理 Store 资源。
    /// </summary>
    /// <returns>完整资源清理任务。</returns>
    [GlobalCleanup]
    public Task CleanupAsync() => DisposeAsync().AsTask();

    /// <summary>
    /// 等待 Store 排空并释放资源，支持初始化前和重复清理。
    /// </summary>
    /// <returns>完整资源清理任务。</returns>
    public async ValueTask DisposeAsync()
    {
        if (_store is not null)
        {
            await _store.DisposeAsync().ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 并发派发一整批意图并等待全部完成。
    /// </summary>
    /// <returns>本批实际完成的计数增量。</returns>
    [Benchmark]
    public async Task<int> ConcurrentDispatchBatchAsync()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ThreadCount);
        int expected = checked(ThreadCount * DispatchesPerThread);
        int before = _store.CurrentState.Counter;
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int arrived = 0;
        Task[] workers = new Task[ThreadCount];
        for (int index = 0; index < ThreadCount; index++)
        {
            workers[index] = Task.Run(async () =>
            {
                if (Interlocked.Increment(ref arrived) == workers.Length)
                {
                    ready.SetResult();
                }

                await start.Task.ConfigureAwait(false);
                await DispatchIncrementManyAsync().ConfigureAwait(false);
            });
        }

        await ready.Task.ConfigureAwait(false);
        start.SetResult();
        await Task.WhenAll(workers).ConfigureAwait(false);
        int completed = unchecked(_store.CurrentState.Counter - before);
        if (completed != expected)
        {
            throw new InvalidOperationException($"本批应完成 {expected} 次派发，实际计数增量为 {completed}。");
        }

        return completed;
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
