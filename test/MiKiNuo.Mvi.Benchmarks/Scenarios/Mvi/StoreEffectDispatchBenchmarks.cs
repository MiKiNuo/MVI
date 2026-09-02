using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示副作用数量扫描基准：
/// 0/1/4 个无操作副作用下派发意图（无中间件），
/// 给出"每个副作用派发加多少纳秒"的线性回归答案。
/// </summary>
[MemoryDiagnoser]
public class StoreEffectDispatchBenchmarks : IDisposable
{
    private MviStore<MinimalState, MinimalIntent, MinimalEffect> _store = null!;
    private readonly MinimalIntent.EmitNops _emitNops = new();

    /// <summary>
    /// 获取或设置每次派发产出的无操作副作用数量。
    /// </summary>
    [Params(0, 1, 4)]
    public int EffectCount { get; set; }

    /// <summary>
    /// 按参数构建对应副作用数量的 Store。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _store = new MviStore<MinimalState, MinimalIntent, MinimalEffect>(
            MinimalState.Initial,
            new MinimalReducer(EffectCount),
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
    /// 派发一个产出副作用的意图走完整管线，含锁外副作用逐个派发。
    /// </summary>
    /// <returns>表示异步派发过程的任务。</returns>
    [Benchmark]
    public async Task DispatchEmitNopsAsync()
    {
        await _store.DispatchAsync(_emitNops).ConfigureAwait(false);
    }
}
