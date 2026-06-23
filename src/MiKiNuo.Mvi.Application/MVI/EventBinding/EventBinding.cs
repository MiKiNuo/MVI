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
/// 表示 MVI 组件基类，提供 Intent 派发入口并管理组件生命周期。
/// </summary>
/// <remarks>
/// ViewModel 基类继承本类，通过 <see cref="GetIntentDispatcher"/> 暴露派发入口；
/// View 层创建 <see cref="EventBinding{TEvent}"/> 后调用
/// <see cref="EventBinding{TEvent}.Attach"/> 注册到 View 侧的 <c>MviDisposableBag</c>，
/// 实现"事件订阅生命周期随 View 重新绑定自动回收"。
/// </remarks>
public abstract class MviComponent : IDisposable, IMviIntentDispatcher
{
    private bool _isDisposed;

    /// <summary>
    /// 显式实现 <see cref="IMviIntentDispatcher.Dispatch"/>，转发到 protected 抽象方法。
    /// </summary>
    /// <param name="intent">意图。</param>
    void IMviIntentDispatcher.Dispatch(IMviIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        Dispatch(intent);
    }

    /// <summary>
    /// 获取 Intent 派发器，供 View 层把事件绑定映射为 Intent 派发。
    /// </summary>
    /// <returns>当前组件的 Intent 派发器。</returns>
    public IMviIntentDispatcher GetIntentDispatcher() => this;

    /// <summary>
    /// 派发 Intent 到 Store。
    /// </summary>
    /// <param name="intent">意图。</param>
    protected abstract void Dispatch(IMviIntent intent);

    /// <summary>
    /// 释放组件资源。
    /// </summary>
    public virtual void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
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
