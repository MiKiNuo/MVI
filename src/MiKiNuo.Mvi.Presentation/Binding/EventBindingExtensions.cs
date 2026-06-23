using MiKiNuo.Mvi.Application.MVI.EventBinding;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Presentation.Disposables;

namespace MiKiNuo.Mvi.Presentation.Binding;

/// <summary>
/// 表示事件源到 Intent 派发器的链式绑定扩展。
/// </summary>
/// <remarks>
/// 把 <see cref="IEventSource{TEvent}"/> + <see cref="EventBinding{TEvent}"/> + <see cref="MviDisposableBag"/>
/// 三步操作收敛为单次 <see cref="BindTo{TEvent}"/> 调用，
/// 与 Godot <c>BindButton</c> / <c>BindEvent</c> 命令绑定风格保持一致。
/// </remarks>
public static class EventBindingExtensions
{
    /// <summary>
    /// 把事件源绑定到 Intent 派发器，订阅生命周期由 <paramref name="bindings"/> 管理。
    /// </summary>
    /// <typeparam name="TEvent">事件数据类型。</typeparam>
    /// <param name="source">事件源。</param>
    /// <param name="dispatcher">Intent 派发器。</param>
    /// <param name="mapper">事件到 Intent 的映射器。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    public static void BindTo<TEvent>(
        this IEventSource<TEvent> source,
        IMviIntentDispatcher dispatcher,
        Func<TEvent, IMviIntent> mapper,
        MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(bindings);

        EventBinding<TEvent> binding = new(source, mapper);
        IDisposable subscription = binding.Attach(dispatcher.Dispatch);
        bindings.Add(subscription);
    }
}
