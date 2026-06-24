using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板意图处理器。
/// </summary>
public sealed class EventBindingSearchIntentHandler
    : IMviIntentHandler<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchMutation, EventBindingSearchEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<EventBindingSearchMutation, EventBindingSearchEffect>> HandleAsync(
        EventBindingSearchState state,
        EventBindingSearchIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<EventBindingSearchMutation, EventBindingSearchEffect> result = intent switch
        {
            EventBindingSearchIntent.ChangeQuery changeQuery => HandleChangeQuery(state, changeQuery),
            _ => MviHandleResult.Empty<EventBindingSearchMutation, EventBindingSearchEffect>(),
        };
        return new ValueTask<MviHandleResult<EventBindingSearchMutation, EventBindingSearchEffect>>(result);
    }

    private static MviHandleResult<EventBindingSearchMutation, EventBindingSearchEffect> HandleChangeQuery(
        EventBindingSearchState state,
        EventBindingSearchIntent.ChangeQuery intent)
    {
        string queryText = intent.Payload.Text;
        string previousQueryText = intent.Payload.PreviousText ?? string.Empty;
        int eventCount = state.EventCount + 1;
        string statusText = $"搜索文本变化：{queryText}";

        EventBindingSearchMutation[] mutations =
        {
            new EventBindingSearchMutation.SetQueryText(queryText),
            new EventBindingSearchMutation.SetPreviousQueryText(previousQueryText),
            new EventBindingSearchMutation.SetEventCount(eventCount),
            new EventBindingSearchMutation.SetStatusText(statusText),
        };
        EventBindingSearchEffect[] effects = { new EventBindingSearchEffect.NotifyQueryChanged(queryText) };
        return MviHandleResult.MutationsAndEffects<EventBindingSearchMutation, EventBindingSearchEffect>(mutations, effects);
    }
}
