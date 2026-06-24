using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合根意图处理器。
/// </summary>
public sealed class EventBindingWorkbenchIntentHandler
    : IMviIntentHandler<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchMutation, EventBindingWorkbenchEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<EventBindingWorkbenchMutation, EventBindingWorkbenchEffect>> HandleAsync(
        EventBindingWorkbenchState state,
        EventBindingWorkbenchIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<EventBindingWorkbenchMutation, EventBindingWorkbenchEffect> result = intent switch
        {
            EventBindingWorkbenchIntent.RecordInteraction recordInteraction => HandleRecordInteraction(recordInteraction),
            _ => MviHandleResult.Empty<EventBindingWorkbenchMutation, EventBindingWorkbenchEffect>(),
        };
        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// 处理记录子组件交互意图。
    /// </summary>
    /// <param name="intent">记录交互意图。</param>
    /// <returns>处理结果。</returns>
    private static MviHandleResult<EventBindingWorkbenchMutation, EventBindingWorkbenchEffect> HandleRecordInteraction(
        EventBindingWorkbenchIntent.RecordInteraction intent)
    {
        string interactionText = $"{intent.SourceComponent}/{intent.ActionKey}: {intent.ContextText}";
        return MviHandleResult.Mutations<EventBindingWorkbenchMutation, EventBindingWorkbenchEffect>(
            new EventBindingWorkbenchMutation.SetLastInteractionText(interactionText),
            new EventBindingWorkbenchMutation.AddInteractionCount(1));
    }
}
