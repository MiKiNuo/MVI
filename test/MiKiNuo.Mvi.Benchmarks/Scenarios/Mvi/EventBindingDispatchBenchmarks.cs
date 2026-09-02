using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示事件绑定派发基准：
/// 对照"裸 Store 串行派发 1000 次"（基线）与"经事件绑定触发 1000 个事件"
/// （事件源回调 → Intent 映射 → 派发器转发 → Store 管线），
/// 差值即事件绑定层每个事件的额外成本。报告值为每 1000 个事件的成本。
/// </summary>
[MemoryDiagnoser]
public class EventBindingDispatchBenchmarks : IDisposable
{
    private const int EventsPerOperation = 1000;

    private EventBindingScenario _scenario = null!;
    private MviStore<MinimalState, MinimalIntent, MinimalEffect> _bareStore = null!;
    private readonly MinimalIntent.Increment _increment = new();

    /// <summary>
    /// 构建事件绑定场景与无绑定的裸 Store 基线。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _scenario = new EventBindingScenario();
        _bareStore = new MviStore<MinimalState, MinimalIntent, MinimalEffect>(
            MinimalState.Initial,
            new MinimalReducer(0),
            new MinimalEffectDispatcher());
    }

    /// <summary>
    /// 清理场景与裸 Store 资源。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    /// <summary>
    /// 释放场景与裸 Store 资源。
    /// </summary>
    public void Dispose()
    {
        _scenario?.Dispose();
        _bareStore?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 裸 Store 串行派发 1000 个意图：不含事件层的事件派发基线。
    /// </summary>
    /// <returns>表示串行派发过程的任务。</returns>
    [Benchmark(Baseline = true)]
    public async Task DirectDispatch1000Async()
    {
        for (int index = 0; index < EventsPerOperation; index++)
        {
            await _bareStore.DispatchAsync(_increment).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 经事件绑定触发 1000 个事件：事件源回调 → Intent 映射 → 派发器 → Store 管线。
    /// </summary>
    [Benchmark]
    public void Raise1000EventsThroughBinding()
    {
        _scenario.RaiseEvents(EventsPerOperation);
    }
}
