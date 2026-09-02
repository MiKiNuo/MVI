using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
using R3;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示 R3 状态流基准场景：包装 <see cref="ReactiveProperty{T}"/>（Store 同款状态发布通道），
/// 测量状态发布吞吐与订阅者数量的关系。
/// </summary>
public sealed class R3StateStreamScenario : IDisposable
{
    private readonly ReactiveProperty<MinimalState> _state = new(MinimalState.Initial);
    private readonly List<IDisposable> _subscriptions = new();
    private int _publishedCount;
    private bool _isDisposed;

    /// <summary>
    /// 获取当前订阅者数量。
    /// </summary>
    public int SubscriberCount => _subscriptions.Count;

    /// <summary>
    /// 获取累计发布的状态数量。
    /// </summary>
    public int PublishedCount => _publishedCount;

    /// <summary>
    /// 获取当前状态。
    /// </summary>
    public MinimalState CurrentState => _state.Value;

    /// <summary>
    /// 订阅状态流，订阅生命周期由场景统一管理。
    /// </summary>
    /// <param name="onNext">状态通知回调。</param>
    public void Subscribe(Action<MinimalState> onNext)
    {
        ArgumentNullException.ThrowIfNull(onNext);
        _subscriptions.Add(_state.Subscribe(onNext));
    }

    /// <summary>
    /// 发布指定数量的互异状态：每次发布计数加一并构造新状态实例，
    /// 与 Store 规约后发布新状态的形态一致。
    /// </summary>
    /// <param name="count">发布的状态数量。</param>
    public void Publish(int count)
    {
        for (int index = 0; index < count; index++)
        {
            _publishedCount++;
            _state.Value = MinimalState.Initial with { Counter = _publishedCount };
        }
    }

    /// <summary>
    /// 释放状态属性与全部订阅。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        foreach (IDisposable subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        _state.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
