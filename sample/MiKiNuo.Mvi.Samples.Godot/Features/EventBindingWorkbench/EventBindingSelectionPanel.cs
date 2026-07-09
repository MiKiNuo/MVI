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

/// <summary>表示 Godot 选择面板状态。</summary>
/// <param name="SelectedMissionId">选中任务编号。</param>
/// <param name="SelectedIndex">选中索引。</param>
/// <param name="EventCount">事件次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingSelectionState(
    string SelectedMissionId,
    int SelectedIndex,
    int EventCount,
    string StatusText) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static EventBindingSelectionState Initial { get; } = new("-", -1, 0, "等待 ItemList.ItemSelected。");
}

/// <summary>表示 Godot 选择面板意图。</summary>
public abstract partial record EventBindingSelectionIntent : IMviIntent
{
    /// <summary>表示选择变化意图。</summary>
    /// <param name="Payload">选择变化载荷。</param>
    public sealed partial record ChangeSelection(MviSelectionChangedEventPayload Payload) : EventBindingSelectionIntent;
}

/// <summary>表示 Godot 选择面板副作用。</summary>
public abstract partial record EventBindingSelectionEffect : IMviEffect
{
    /// <summary>表示通知选择变化副作用。</summary>
    /// <param name="MissionId">任务编号。</param>
    public sealed partial record NotifySelectionChanged(string MissionId) : EventBindingSelectionEffect;
}

/// <summary>表示 Godot 选择面板副作用分发器。</summary>
public sealed class EventBindingSelectionEffectDispatcher : MviEffectDispatcherBase<EventBindingSelectionEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>初始化 Godot 选择面板副作用分发器。</summary>
    public EventBindingSelectionEffectDispatcher(IMviMediator mediator)
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
    protected override async ValueTask DispatchCoreAsync(EventBindingSelectionEffect effect, CancellationToken cancellationToken)
    {
        if (effect is EventBindingSelectionEffect.NotifySelectionChanged selectionChanged)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("SelectionPanel", "ItemSelected", selectionChanged.MissionId),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>表示 Godot 选择面板 ViewModel。</summary>
/// <remarks>
/// 事件绑定通过 <see cref="Application.MVI.EventBinding.IEventSource{TEvent}"/> 适配器 +
/// <see cref="Application.MVI.EventBinding.EventBinding{TEvent}"/> 在 View 层完成，
/// 事件直接映射为 <see cref="EventBindingSelectionIntent.ChangeSelection"/> 意图派发到 Store，不经过命令层。
/// </remarks>
public sealed partial class EventBindingSelectionViewModel
    : MviViewModelBase<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>初始化 Godot 选择面板 ViewModel。</summary>
    public EventBindingSelectionViewModel(IMviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> store)
        : base(store)
    {
    }

    /// <summary>获取选中任务编号。</summary>
    [MviBind(nameof(EventBindingSelectionState.SelectedMissionId))]
    public partial string SelectedMissionId { get; private set; }
}
