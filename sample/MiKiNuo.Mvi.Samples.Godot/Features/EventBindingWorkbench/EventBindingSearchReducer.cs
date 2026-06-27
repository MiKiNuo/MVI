using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 搜索面板规约器。
/// </summary>
public sealed partial class EventBindingSearchReducer
    : MviReducerBase<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>处理查询文本变化意图。</summary>
    [MviReduce(typeof(EventBindingSearchIntent.ChangeQuery))]
    private MviReduceResult<EventBindingSearchState, EventBindingSearchEffect> HandleChangeQuery(
        EventBindingSearchState state,
        EventBindingSearchIntent.ChangeQuery intent)
    {
        EventBindingSearchState newState = state with
        {
            QueryText = intent.Payload.Text,
            EventCount = state.EventCount + 1,
            StatusText = $"搜索文本变化：{intent.Payload.Text}",
        };
        return MviReduceResult.StateAndEffect<EventBindingSearchState, EventBindingSearchEffect>(
            newState,
            new EventBindingSearchEffect.NotifyQueryChanged(intent.Payload.Text));
    }
}
