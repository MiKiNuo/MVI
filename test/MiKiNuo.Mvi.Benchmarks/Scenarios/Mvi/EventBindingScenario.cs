using MiKiNuo.Mvi.Application.MVI.EventBinding;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi;

/// <summary>
/// 表示事件绑定基准场景：把委托事件源经 <see cref="EventBinding{TEvent}"/>
/// 映射为 Intent 派发到 Store，测量"事件 → 映射 → 派发"全链的 UI 线程成本。
/// </summary>
public sealed class EventBindingScenario : IDisposable
{
    private readonly MviStore<MinimalState, MinimalIntent, MinimalEffect> _store;
    private readonly MinimalStoreIntentDispatcher _dispatcher;
    private readonly IDisposable _bindingSubscription;
    private Action<int>? _eventHandler;
    private bool _isDisposed;

    /// <summary>
    /// 初始化事件绑定基准场景：手工装配 Store、事件源与绑定。
    /// </summary>
    public EventBindingScenario()
    {
        _store = new MviStore<MinimalState, MinimalIntent, MinimalEffect>(
            MinimalState.Initial,
            new MinimalReducer(0),
            new MinimalEffectDispatcher());
        _dispatcher = new MinimalStoreIntentDispatcher(_store);

        DelegateEventSource<int> source = new(handler =>
        {
            _eventHandler = handler;
            return new ActionDisposable(() => _eventHandler = null);
        });

        EventBinding<int> binding = new(source, static value => new MinimalIntent.Increment());
        _bindingSubscription = binding.Attach(_dispatcher);
    }

    /// <summary>
    /// 获取累计触发的事件数量。
    /// </summary>
    public int RaisedEventCount { get; private set; }

    /// <summary>
    /// 获取 Store 当前计数（等于已完成的 Intent 派发数）。
    /// </summary>
    public int CurrentCounter => _store.CurrentState.Counter;

    /// <summary>
    /// 触发指定数量的事件，每个事件映射为一个增量意图并派发到 Store。
    /// </summary>
    /// <param name="count">事件数量。</param>
    public void RaiseEvents(int count)
    {
        for (int index = 0; index < count; index++)
        {
            RaisedEventCount++;
            _eventHandler?.Invoke(RaisedEventCount);
        }
    }

    /// <summary>
    /// 等待 Store 计数达到期望值：绑定派发为即发即忘，异步完成时需轮询对账。
    /// </summary>
    /// <param name="expectedCounter">期望的 Store 计数。</param>
    /// <returns>表示等待过程的任务。</returns>
    /// <exception cref="TimeoutException">超过 10 秒仍未达到期望计数时抛出。</exception>
    public async Task WaitUntilCounterAsync(int expectedCounter)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (_store.CurrentState.Counter < expectedCounter)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException(
                    $"等待 Store 计数达到 {expectedCounter} 超时，当前为 {_store.CurrentState.Counter}。");
            }

            await Task.Delay(5).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 释放绑定订阅与 Store 资源。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _bindingSubscription.Dispose();
        _store.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 表示把非泛型 Intent 派发适配回强类型 Store 的最小派发器，
    /// 语义与 ViewModel 基类的 DispatchCoreAsync 转发一致。
    /// </summary>
    private sealed class MinimalStoreIntentDispatcher : IMviIntentDispatcher
    {
        private readonly MviStore<MinimalState, MinimalIntent, MinimalEffect> _store;

        /// <summary>
        /// 初始化最小 Intent 派发器。
        /// </summary>
        /// <param name="store">目标状态存储。</param>
        public MinimalStoreIntentDispatcher(MviStore<MinimalState, MinimalIntent, MinimalEffect> store)
        {
            ArgumentNullException.ThrowIfNull(store);
            _store = store;
        }

        /// <summary>
        /// 派发失败时触发（与 <see cref="MviComponent"/> 契约一致）。
        /// </summary>
        public event EventHandler<IntentDispatchExceptionEventArgs>? DispatchFailed;

        /// <summary>
        /// 把非泛型意图转发到强类型 Store，失败时报告并重抛。
        /// </summary>
        /// <param name="intent">意图。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>表示异步派发过程的任务。</returns>
        public async ValueTask DispatchAsync(
            IMviIntent intent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(intent);
            if (intent is not MinimalIntent minimalIntent)
            {
                throw new ArgumentException(
                    $"意图类型不匹配：期望 {typeof(MinimalIntent).FullName}，实际 {intent.GetType().FullName}。",
                    nameof(intent));
            }

            try
            {
                await _store.DispatchAsync(minimalIntent, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                DispatchFailed?.Invoke(this, new IntentDispatchExceptionEventArgs(exception, intent));
                throw;
            }
        }
    }
}
