using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
using R3;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示 R3 状态发布吞吐基准：
/// 向 Store 同款 <see cref="ReactiveProperty{T}"/> 发布 1000 个互异状态，
/// 订阅者数量扫描（1/5）给出"每个订阅者加多少纳秒"的边际成本。
/// 报告值为每 1000 次发布的总成本。
/// </summary>
[MemoryDiagnoser]
public class R3StatePublishBenchmarks : IDisposable
{
    private R3StateStreamScenario _scenario = null!;

    /// <summary>
    /// 获取或设置状态流订阅者数量。
    /// </summary>
    [Params(1, 5)]
    public int SubscriberCount { get; set; }

    /// <summary>
    /// 构建场景并按参数挂载无操作订阅者。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _scenario = new R3StateStreamScenario();
        for (int index = 0; index < SubscriberCount; index++)
        {
            _scenario.Subscribe(static _ => { });
        }
    }

    /// <summary>
    /// 清理场景资源。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    /// <summary>
    /// 释放场景资源。
    /// </summary>
    public void Dispose()
    {
        _scenario?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 发布 1000 个互异状态并通知全部订阅者。
    /// </summary>
    [Benchmark]
    public void Publish1000States()
    {
        _scenario.Publish(1000);
    }
}

/// <summary>
/// 表示 R3 订阅成本基准：测量"挂载一个订阅者再卸载"的往返成本
/// （对应 View 重新绑定时的订阅生命周期开销）。
/// </summary>
[MemoryDiagnoser]
public class R3SubscribeBenchmarks : IDisposable
{
    private ReactiveProperty<MinimalState> _property = null!;

    /// <summary>
    /// 构建无订阅者的状态属性。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _property = new ReactiveProperty<MinimalState>(MinimalState.Initial);
    }

    /// <summary>
    /// 清理状态属性资源。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    /// <summary>
    /// 释放状态属性资源。
    /// </summary>
    public void Dispose()
    {
        _property?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 订阅并立即取消订阅：测量订阅往返成本（订阅时含一次当前值重放）。
    /// </summary>
    [Benchmark]
    public void SubscribeAndDispose()
    {
        IDisposable subscription = _property.Subscribe(static _ => { });
        subscription.Dispose();
    }
}
