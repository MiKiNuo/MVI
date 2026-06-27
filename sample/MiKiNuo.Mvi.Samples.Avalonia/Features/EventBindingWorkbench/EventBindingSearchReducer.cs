using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板规约器。
/// </summary>
public sealed partial class EventBindingSearchReducer
    : MviReducerBase<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>
    /// 处理查询变更意图。
    /// </summary>
    [MviReduce(typeof(EventBindingSearchIntent.ChangeQuery))]
    private MviReduceResult<EventBindingSearchState, EventBindingSearchEffect> HandleChangeQuery(
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
        return MviReduceResult.StateAndEffect<EventBindingSearchState, EventBindingSearchEffect>(
            newState,
            new EventBindingSearchEffect.NotifyQueryChanged(queryText));
    }
}
