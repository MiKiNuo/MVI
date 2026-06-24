using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定详情面板意图处理器。
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
            EventBindingDetailIntent.PressDetail pressDetail => HandlePressDetail(state, pressDetail),
            EventBindingDetailIntent.Refresh refresh => HandleRefresh(state, refresh),
            _ => MviHandleResult.Empty<EventBindingDetailMutation, EventBindingDetailEffect>(),
        };
        return new ValueTask<MviHandleResult<EventBindingDetailMutation, EventBindingDetailEffect>>(result);
    }

    private static MviHandleResult<EventBindingDetailMutation, EventBindingDetailEffect> HandlePressDetail(
        EventBindingDetailState state,
        EventBindingDetailIntent.PressDetail intent)
    {
        string pointerText = $"Pointer {intent.Payload.Button} @ {intent.Payload.PositionX:0},{intent.Payload.PositionY:0}";

        EventBindingDetailMutation[] mutations =
        {
            new EventBindingDetailMutation.SetLastPointerText(pointerText),
            new EventBindingDetailMutation.SetStatusText("详情区域收到 PointerPressed。"),
        };
        EventBindingDetailEffect[] effects = { new EventBindingDetailEffect.NotifyDetailEvent("PointerPressed", pointerText) };
        return MviHandleResult.MutationsAndEffects<EventBindingDetailMutation, EventBindingDetailEffect>(mutations, effects);
    }

    private static MviHandleResult<EventBindingDetailMutation, EventBindingDetailEffect> HandleRefresh(
        EventBindingDetailState state,
        EventBindingDetailIntent.Refresh intent)
    {
        int refreshCount = state.RefreshCount + 1;
        string sourceName = intent.Payload.SourceName ?? "Unknown";

        EventBindingDetailMutation[] mutations =
        {
            new EventBindingDetailMutation.SetRefreshCount(refreshCount),
            new EventBindingDetailMutation.SetStatusText($"刷新动作：{intent.Payload.SourceName}"),
        };
        EventBindingDetailEffect[] effects = { new EventBindingDetailEffect.NotifyDetailEvent("Action", sourceName) };
        return MviHandleResult.MutationsAndEffects<EventBindingDetailMutation, EventBindingDetailEffect>(mutations, effects);
    }
}
