using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 详情面板规约器。
/// </summary>
public sealed class EventBindingDetailReducer
    : MviReducerBase<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> Reduce(
        EventBindingDetailState state,
        EventBindingDetailIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            EventBindingDetailIntent.Prepare prepare => MviReduceResult.State<EventBindingDetailState, EventBindingDetailEffect>(
                state with
                {
                    PrepareCount = state.PrepareCount + 1,
                    StatusText = $"准备动作：{prepare.Payload.SourceName ?? "Unknown"}",
                }),
            _ => MviReduceResult.State<EventBindingDetailState, EventBindingDetailEffect>(state),
        };
    }
}
