using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示中间件层数扫描基准：
/// 0/1/4/8 层无操作直通中间件下派发无副作用意图，
/// 给出"每层中间件加多少纳秒"的线性回归答案。
/// </summary>
[MemoryDiagnoser]
public class StoreMiddlewareDispatchBenchmarks : IDisposable
{
    private MviStore<MinimalState, MinimalIntent, MinimalEffect> _store = null!;
    private readonly MinimalIntent.Increment _increment = new();

    /// <summary>
    /// 获取或设置无操作中间件层数。
    /// </summary>
    [Params(0, 1, 4, 8)]
    public int MiddlewareCount { get; set; }

    /// <summary>
    /// 按参数构建对应层数中间件的 Store。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        List<IMviMiddleware<MinimalState, MinimalIntent, MinimalEffect>> middlewares = new();
        for (int index = 0; index < MiddlewareCount; index++)
        {
            middlewares.Add(new NopMiddleware());
        }

        _store = new MviStore<MinimalState, MinimalIntent, MinimalEffect>(
            MinimalState.Initial,
            new MinimalReducer(0),
            new MinimalEffectDispatcher(),
            middlewares);
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
    /// 派发一个无副作用意图走完整派发管线：锁 → 中间件链 → 规约 → 状态发布。
    /// </summary>
    /// <returns>表示异步派发过程的任务。</returns>
    [Benchmark]
    public async Task DispatchIncrementAsync()
    {
        await _store.DispatchAsync(_increment).ConfigureAwait(false);
    }
}
