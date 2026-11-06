using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示事件绑定派发基准：
/// 对照裸 Store 串行派发与事件源回调 → Intent 映射 → 派发器 → Store 整条输入链。
/// 两条路径均逐次创建 Intent，并在计时内确认 1000 次状态更新完成。
/// 报告值为每批总成本（事件路径包含完成等待），差值不是纯 binding 额外成本。
/// </summary>
[MemoryDiagnoser]
public class EventBindingDispatchBenchmarks : IDisposable
{
    private const int EventsPerOperation = 1000;

    private EventBindingScenario _scenario = null!;
    private MviStore<MinimalState, MinimalIntent> _bareStore = null!;

    /// <summary>
    /// 构建事件绑定场景与无绑定的裸 Store 基线。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _scenario = new EventBindingScenario();
        _bareStore = new MviStore<MinimalState, MinimalIntent>(
            MinimalState.Initial,
            new MinimalHandler(),
            new MinimalReducer());
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
    /// <returns>本批实际完成的计数增量。</returns>
    [Benchmark(Baseline = true)]
    public async Task<int> DirectDispatch1000Async()
    {
        int before = _bareStore.CurrentState.Counter;
        for (int index = 0; index < EventsPerOperation; index++)
        {
            await _bareStore.DispatchAsync(new MinimalIntent.Increment()).ConfigureAwait(false);
        }

        int completed = unchecked(_bareStore.CurrentState.Counter - before);
        if (completed != EventsPerOperation)
        {
            throw new InvalidOperationException($"直接派发应完成 {EventsPerOperation} 次，实际为 {completed}。");
        }

        return completed;
    }

    /// <summary>
    /// 经事件绑定触发 1000 个事件：事件源回调 → Intent 映射 → 派发器 → Store 管线。
    /// </summary>
    /// <returns>本批实际完成的计数增量。</returns>
    [Benchmark]
    public async Task<int> Raise1000EventsThroughBindingAsync()
    {
        int before = _scenario.CurrentCounter;
        _scenario.RaiseEvents(EventsPerOperation);
        await _scenario.WaitUntilCounterAsync(_scenario.RaisedEventCount).ConfigureAwait(false);
        int completed = unchecked(_scenario.CurrentCounter - before);
        if (completed != EventsPerOperation || _scenario.CurrentCounter != _scenario.RaisedEventCount)
        {
            throw new InvalidOperationException($"事件派发应完成 {EventsPerOperation} 次，实际为 {completed}。");
        }

        return completed;
    }
}
