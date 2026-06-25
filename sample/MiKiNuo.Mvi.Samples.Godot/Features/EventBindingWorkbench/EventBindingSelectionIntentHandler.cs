using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 选择面板意图处理器。
/// </summary>
public sealed class EventBindingSelectionIntentHandler
    : IMviIntentHandler<EventBindingSelectionState, EventBindingSelectionIntent, EventBindingSelectionEffect>
{
    /// <summary>
    /// 处理意图并产生动作副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>动作副作用集合。</returns>
    public ValueTask<IReadOnlyList<EventBindingSelectionEffect>> HandleAsync(
        EventBindingSelectionState state,
        EventBindingSelectionIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        IReadOnlyList<EventBindingSelectionEffect> effects = intent switch
        {
            EventBindingSelectionIntent.ChangeSelection changeSelection => new EventBindingSelectionEffect[]
            {
                new EventBindingSelectionEffect.NotifySelectionChanged(
                    changeSelection.Payload.SelectedValue?.ToString() ?? "-"),
            },
            _ => Array.Empty<EventBindingSelectionEffect>(),
        };
        return new ValueTask<IReadOnlyList<EventBindingSelectionEffect>>(effects);
    }
}
