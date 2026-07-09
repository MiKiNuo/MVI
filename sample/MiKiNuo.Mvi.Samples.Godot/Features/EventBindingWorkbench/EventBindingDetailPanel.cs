using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Presentation.Events;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>表示 Godot 详情面板状态。</summary>
/// <param name="PrepareCount">准备次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingDetailState(int PrepareCount, string StatusText) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static EventBindingDetailState Initial { get; } = new(0, "等待 Button.Pressed。");
}

/// <summary>表示 Godot 详情面板意图。</summary>
public abstract partial record EventBindingDetailIntent : IMviIntent
{
    /// <summary>表示准备动作意图。</summary>
    /// <param name="Payload">动作事件载荷。</param>
    public sealed partial record Prepare(MviActionEventPayload Payload) : EventBindingDetailIntent;
}

/// <summary>表示 Godot 详情面板副作用。</summary>
public abstract partial record EventBindingDetailEffect : IMviEffect
{
    /// <summary>表示通知准备动作副作用。</summary>
    /// <param name="SourceName">事件来源名称。</param>
    public sealed partial record NotifyPrepare(string SourceName) : EventBindingDetailEffect;
}

/// <summary>表示 Godot 详情面板副作用分发器。</summary>
public sealed class EventBindingDetailEffectDispatcher : MviEffectDispatcherBase<EventBindingDetailEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>初始化 Godot 详情面板副作用分发器。</summary>
    public EventBindingDetailEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        _mediator = mediator;
    }

    /// <summary>
    /// 分发具体副作用。
    /// </summary>
    /// <param name="effect">副作用（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    protected override async ValueTask DispatchCoreAsync(EventBindingDetailEffect effect, CancellationToken cancellationToken)
    {
        if (effect is EventBindingDetailEffect.NotifyPrepare prepare)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("DetailPanel", "Pressed", prepare.SourceName),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>表示 Godot 详情面板 ViewModel。</summary>
/// <remarks>
/// 事件绑定通过 <see cref="Application.MVI.EventBinding.IEventSource{TEvent}"/> 适配器 +
/// <see cref="Application.MVI.EventBinding.EventBinding{TEvent}"/> 在 View 层完成，
/// 事件直接映射为 <see cref="EventBindingDetailIntent.Prepare"/> 意图派发到 Store，不经过命令层。
/// </remarks>
public sealed partial class EventBindingDetailViewModel
    : MviViewModelBase<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect>
{
    /// <summary>初始化 Godot 详情面板 ViewModel。</summary>
    public EventBindingDetailViewModel(IMviStore<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect> store)
        : base(store)
    {
    }

    /// <summary>获取准备次数。</summary>
    [MviBind(nameof(EventBindingDetailState.PrepareCount))]
    public partial int PrepareCount { get; private set; }
}
