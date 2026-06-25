using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 搜索面板规约器。
/// </summary>
public sealed class EventBindingSearchReducer
    : MviReducerBase<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<EventBindingSearchState, EventBindingSearchEffect> Reduce(
        EventBindingSearchState state,
        EventBindingSearchIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            EventBindingSearchIntent.ChangeQuery changeQuery => MviReduceResult.State<EventBindingSearchState, EventBindingSearchEffect>(
                state with
                {
                    QueryText = changeQuery.Payload.Text,
                    EventCount = state.EventCount + 1,
                    StatusText = $"搜索文本变化：{changeQuery.Payload.Text}",
                }),
            _ => MviReduceResult.State<EventBindingSearchState, EventBindingSearchEffect>(state),
        };
    }
}
