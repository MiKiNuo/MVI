using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 搜索面板意图处理器。
/// </summary>
public sealed class EventBindingSearchIntentHandler
    : IMviIntentHandler<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>
    /// 处理意图并产生动作副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>动作副作用集合。</returns>
    public ValueTask<IReadOnlyList<EventBindingSearchEffect>> HandleAsync(
        EventBindingSearchState state,
        EventBindingSearchIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        IReadOnlyList<EventBindingSearchEffect> effects = intent switch
        {
            EventBindingSearchIntent.ChangeQuery changeQuery => new EventBindingSearchEffect[]
            {
                new EventBindingSearchEffect.NotifyQueryChanged(changeQuery.Payload.Text),
            },
            _ => Array.Empty<EventBindingSearchEffect>(),
        };
        return new ValueTask<IReadOnlyList<EventBindingSearchEffect>>(effects);
    }
}
