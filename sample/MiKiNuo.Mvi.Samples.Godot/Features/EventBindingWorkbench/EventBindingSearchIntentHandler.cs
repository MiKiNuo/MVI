using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 搜索面板意图处理器。
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
            EventBindingSearchIntent.ChangeQuery changeQuery => HandleChangeQuery(changeQuery),
            _ => MviHandleResult.Empty<EventBindingSearchMutation, EventBindingSearchEffect>(),
        };
        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// 处理查询文本变化意图。
    /// </summary>
    /// <param name="intent">查询变化意图。</param>
    /// <returns>处理结果。</returns>
    private static MviHandleResult<EventBindingSearchMutation, EventBindingSearchEffect> HandleChangeQuery(
        EventBindingSearchIntent.ChangeQuery intent)
    {
        string queryText = intent.Payload.Text;
        EventBindingSearchMutation[] mutations = new EventBindingSearchMutation[]
        {
            new EventBindingSearchMutation.SetQueryText(queryText),
            new EventBindingSearchMutation.AddEventCount(1),
            new EventBindingSearchMutation.SetStatusText($"搜索文本变化：{queryText}"),
        };
        EventBindingSearchEffect[] effects = new EventBindingSearchEffect[]
        {
            new EventBindingSearchEffect.NotifyQueryChanged(queryText),
        };
        return MviHandleResult.MutationsAndEffects<EventBindingSearchMutation, EventBindingSearchEffect>(mutations, effects);
    }
}
