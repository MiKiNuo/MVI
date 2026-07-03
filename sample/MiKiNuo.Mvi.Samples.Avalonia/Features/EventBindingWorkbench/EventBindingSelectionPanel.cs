using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Presentation.Events;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板状态。
/// </summary>
/// <param name="PatientIds">可选患者编号。</param>
/// <param name="SelectedPatientId">选中患者编号。</param>
/// <param name="SelectedIndex">选中索引。</param>
/// <param name="EventCount">事件次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingSelectionState(
    IReadOnlyList<string> PatientIds,
    string SelectedPatientId,
    int SelectedIndex,
    int EventCount,
    string StatusText) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static EventBindingSelectionState Initial { get; } = new(
        new[] { "P10001", "P10002", "P10003" },
        "-",
        -1,
        0,
        "等待 SelectionChanged 事件。");
}

/// <summary>
/// 表示事件绑定选择面板意图。
/// </summary>
public abstract partial record EventBindingSelectionIntent : IMviIntent
{
    /// <summary>
    /// 表示选择变化意图。
    /// </summary>
    /// <param name="Payload">选择变化载荷。</param>
    public sealed partial record ChangeSelection(MviSelectionChangedEventPayload Payload) : EventBindingSelectionIntent;
}

/// <summary>
/// 表示事件绑定选择面板副作用。
/// </summary>
public abstract partial record EventBindingSelectionEffect : IMviEffect
{
    /// <summary>
    /// 表示通知选择变化副作用。
    /// </summary>
    /// <param name="PatientId">患者编号。</param>
    public sealed partial record NotifySelectionChanged(string PatientId) : EventBindingSelectionEffect;
}

/// <summary>
/// 表示事件绑定选择面板副作用分发器。
/// </summary>
public sealed class EventBindingSelectionEffectDispatcher : MviEffectDispatcherBase<EventBindingSelectionEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化事件绑定选择面板副作用分发器。
    /// </summary>
    /// <param name="mediator">中介者。</param>
    public EventBindingSelectionEffectDispatcher(IMviMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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
                new EventBindingWorkbenchInteractionRequest("SelectionPanel", "SelectionChanged", selectionChanged.PatientId),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// 表示事件绑定选择面板 ViewModel。
/// </summary>
public sealed partial class EventBindingSelectionViewModel
    : MviViewModelBase<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>
    /// 初始化事件绑定选择面板 ViewModel。
    /// </summary>
    /// <param name="store">选择面板 Store。</param>
    /// <param name="uiDispatcher">UI 调度器（可选）。</param>
    public EventBindingSelectionViewModel(
        IMviStore<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取可选患者编号。
    /// </summary>
    [MviBind(nameof(EventBindingSelectionState.PatientIds))]
    public partial IReadOnlyList<string> PatientIds { get; private set; }

    /// <summary>
    /// 获取选中患者编号。
    /// </summary>
    [MviBind(nameof(EventBindingSelectionState.SelectedPatientId))]
    public partial string SelectedPatientId { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(EventBindingSelectionState.StatusText))]
    public partial string StatusText { get; private set; }
}
