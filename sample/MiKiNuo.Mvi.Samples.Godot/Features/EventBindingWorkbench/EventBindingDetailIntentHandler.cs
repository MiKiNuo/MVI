using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 详情面板意图处理器。
/// </summary>
public sealed class EventBindingDetailIntentHandler
    : IMviIntentHandler<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect>
{
    /// <summary>
    /// 处理意图并产生动作副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>动作副作用集合。</returns>
    public ValueTask<IReadOnlyList<EventBindingDetailEffect>> HandleAsync(
        EventBindingDetailState state,
        EventBindingDetailIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        IReadOnlyList<EventBindingDetailEffect> effects = intent switch
        {
            EventBindingDetailIntent.Prepare prepare => new EventBindingDetailEffect[]
            {
                new EventBindingDetailEffect.NotifyPrepare(prepare.Payload.SourceName ?? "Unknown"),
            },
            _ => Array.Empty<EventBindingDetailEffect>(),
        };
        return new ValueTask<IReadOnlyList<EventBindingDetailEffect>>(effects);
    }
}
