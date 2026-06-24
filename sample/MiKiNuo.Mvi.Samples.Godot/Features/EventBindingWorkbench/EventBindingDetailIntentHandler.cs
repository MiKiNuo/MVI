using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 详情面板意图处理器。
/// </summary>
public sealed class EventBindingDetailIntentHandler
    : IMviIntentHandler<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailMutation, EventBindingDetailEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<EventBindingDetailMutation, EventBindingDetailEffect>> HandleAsync(
        EventBindingDetailState state,
        EventBindingDetailIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<EventBindingDetailMutation, EventBindingDetailEffect> result = intent switch
        {
            EventBindingDetailIntent.Prepare prepare => HandlePrepare(prepare),
            _ => MviHandleResult.Empty<EventBindingDetailMutation, EventBindingDetailEffect>(),
        };
        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// 处理准备动作意图。
    /// </summary>
    /// <param name="intent">准备动作意图。</param>
    /// <returns>处理结果。</returns>
    private static MviHandleResult<EventBindingDetailMutation, EventBindingDetailEffect> HandlePrepare(
        EventBindingDetailIntent.Prepare intent)
    {
        string sourceName = intent.Payload.SourceName ?? "Unknown";
        EventBindingDetailMutation[] mutations = new EventBindingDetailMutation[]
        {
            new EventBindingDetailMutation.AddPrepareCount(1),
            new EventBindingDetailMutation.SetStatusText($"准备动作：{sourceName}"),
        };
        EventBindingDetailEffect[] effects = new EventBindingDetailEffect[]
        {
            new EventBindingDetailEffect.NotifyPrepare(sourceName),
        };
        return MviHandleResult.MutationsAndEffects<EventBindingDetailMutation, EventBindingDetailEffect>(mutations, effects);
    }
}
