using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板规约器。
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
            EventBindingSearchIntent.ChangeQuery changeQuery => HandleChangeQuery(state, changeQuery),
            _ => MviReduceResult.State<EventBindingSearchState, EventBindingSearchEffect>(state),
        };
    }

    private static MviReduceResult<EventBindingSearchState, EventBindingSearchEffect> HandleChangeQuery(
        EventBindingSearchState state,
        EventBindingSearchIntent.ChangeQuery intent)
    {
        string queryText = intent.Payload.Text;
        string previousQueryText = intent.Payload.PreviousText ?? string.Empty;
        int eventCount = state.EventCount + 1;
        string statusText = $"搜索文本变化：{queryText}";

        EventBindingSearchState newState = state with
        {
            QueryText = queryText,
            PreviousQueryText = previousQueryText,
            EventCount = eventCount,
            StatusText = statusText,
        };
        return MviReduceResult.State<EventBindingSearchState, EventBindingSearchEffect>(newState);
    }
}
