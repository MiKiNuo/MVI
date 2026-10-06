using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示外部调用数量扫描基准：
/// 0/1/4 个无操作外部调用下派发意图（无中间件），
/// 给出"每个外部调用派发加多少纳秒"的线性回归答案。
/// </summary>
[MemoryDiagnoser]
public class StoreHandlerDispatchBenchmarks : IDisposable
{
    private MviStore<MinimalState, MinimalIntent> _store = null!;
    private readonly MinimalIntent.EmitNops _emitNops = new();

    /// <summary>
    /// 获取或设置每次派发产出的无操作外部调用数量。
    /// </summary>
    [Params(0, 1, 4)]
    public int ExternalCallCount { get; set; }

    /// <summary>
    /// 按参数构建对应外部调用数量的 Store。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _store = new MviStore<MinimalState, MinimalIntent>(
            MinimalState.Initial,
            new MinimalHandler(ExternalCallCount),
            new MinimalReducer());
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
    /// 派发一个产出外部调用的意图走完整管线，含锁外外部调用逐个派发。
    /// </summary>
    /// <returns>表示异步派发过程的任务。</returns>
    [Benchmark]
    public async Task DispatchHandlerAsync()
    {
        await _store.DispatchAsync(_emitNops).ConfigureAwait(false);
    }
}
