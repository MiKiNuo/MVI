using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Application.MVI.EventBinding;

/// <summary>
/// 表示事件源接口，封装平台原生事件并提供统一的订阅能力。
/// </summary>
/// <typeparam name="TEvent">事件数据类型。</typeparam>
/// <remarks>
/// 每个平台（Avalonia / Godot）实现自己的 <see cref="IEventSource{TEvent}"/> 适配器，
/// 把原生事件转换为统一的订阅模型。
/// </remarks>
public interface IEventSource<TEvent>
{
    /// <summary>
    /// 订阅事件源。
    /// </summary>
    /// <param name="handler">事件处理器。</param>
    /// <returns>用于取消订阅的可释放资源。</returns>
    public IDisposable Subscribe(Action<TEvent> handler);
}

/// <summary>
/// 表示 Intent 源接口，提供统一的 Intent 订阅能力。
/// </summary>
/// <remarks>
/// 用于把多个事件绑定汇总为统一的 Intent 流，供 Store 直接订阅。
/// </remarks>
public interface IIntentSource
{
    /// <summary>
    /// 订阅 Intent 源。
    /// </summary>
    /// <param name="handler">Intent 处理器。</param>
    /// <returns>用于取消订阅的可释放资源。</returns>
    public IDisposable Subscribe(Action<IMviIntent> handler);
}

/// <summary>
/// 表示事件绑定接口，把事件源 + Intent 映射器组合为可附加到组件的绑定。
/// </summary>
public interface IEventBinding
{
    /// <summary>
    /// 附加绑定到派发回调。
    /// </summary>
    /// <param name="dispatch">Intent 派发回调。</param>
    /// <returns>用于取消绑定的可释放资源。</returns>
    public IDisposable Attach(Action<IMviIntent> dispatch);
}

/// <summary>
/// 表示泛型事件绑定实现，把事件源 + Intent 映射器组合为事件绑定。
/// </summary>
/// <typeparam name="TEvent">事件数据类型。</typeparam>
public sealed class EventBinding<TEvent> : IEventBinding
{
    private readonly IEventSource<TEvent> _source;
    private readonly Func<TEvent, IMviIntent> _mapper;

    /// <summary>
    /// 初始化事件绑定。
    /// </summary>
    /// <param name="source">事件源。</param>
    /// <param name="mapper">事件到 Intent 的映射器。</param>
    public EventBinding(IEventSource<TEvent> source, Func<TEvent, IMviIntent> mapper)
    {
        _source = source;
        _mapper = mapper;
    }

    /// <summary>
    /// 附加绑定到派发回调并返回可释放资源。
    /// </summary>
    /// <param name="dispatch">Intent 派发回调。</param>
    /// <returns>用于取消绑定的可释放资源。</returns>
    public IDisposable Attach(Action<IMviIntent> dispatch)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        return _source.Subscribe(e => dispatch(_mapper(e)));
    }
}

/// <summary>
/// 表示 MVI 组件基类，管理事件绑定生命周期并派发 Intent。
/// </summary>
/// <remarks>
/// ViewModel 基类继承本类，通过 <see cref="Bind(IEventBinding)"/> 注册事件绑定，
/// 绑定触发时调用 <see cref="Dispatch(IMviIntent)"/> 派发 Intent 到 Store。
/// </remarks>
public abstract class MviComponent : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private bool _isDisposed;

    /// <summary>
    /// 注册事件绑定，绑定触发时自动派发 Intent。
    /// </summary>
    /// <param name="binding">事件绑定。</param>
    protected void Bind(IEventBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        _disposables.Add(binding.Attach(Dispatch));
    }

    /// <summary>
    /// 注册事件绑定（供 View 层调用）。
    /// </summary>
    /// <param name="binding">事件绑定。</param>
    public void AddEventBinding(IEventBinding binding) => Bind(binding);

    /// <summary>
    /// 派发 Intent 到 Store。
    /// </summary>
    /// <param name="intent">意图。</param>
    protected abstract void Dispatch(IMviIntent intent);

    /// <summary>
    /// 释放所有事件绑定资源。
    /// </summary>
    public virtual void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        for (int index = _disposables.Count - 1; index >= 0; index--)
        {
            _disposables[index].Dispose();
        }

        _disposables.Clear();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 表示基于委托的事件源适配器，把任意订阅逻辑封装为事件源。
/// </summary>
/// <typeparam name="TEvent">事件数据类型。</typeparam>
public sealed class DelegateEventSource<TEvent> : IEventSource<TEvent>
{
    private readonly Func<Action<TEvent>, IDisposable> _subscribeFunc;

    /// <summary>
    /// 初始化委托事件源。
    /// </summary>
    /// <param name="subscribeFunc">订阅函数。</param>
    public DelegateEventSource(Func<Action<TEvent>, IDisposable> subscribeFunc)
    {
        _subscribeFunc = subscribeFunc;
    }

    /// <summary>
    /// 订阅事件源并返回可释放资源。
    /// </summary>
    /// <param name="handler">事件处理器。</param>
    /// <returns>用于取消订阅的可释放资源。</returns>
    public IDisposable Subscribe(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _subscribeFunc(handler);
    }
}

/// <summary>
/// 表示基于一次性清理动作的可释放资源实现。
/// </summary>
public sealed class ActionDisposable : IDisposable
{
    private Action? _disposeAction;

    /// <summary>
    /// 初始化可释放资源。
    /// </summary>
    /// <param name="disposeAction">清理动作。</param>
    public ActionDisposable(Action disposeAction)
    {
        _disposeAction = disposeAction;
    }

    /// <summary>
    /// 执行一次性清理动作。
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
    }
}
