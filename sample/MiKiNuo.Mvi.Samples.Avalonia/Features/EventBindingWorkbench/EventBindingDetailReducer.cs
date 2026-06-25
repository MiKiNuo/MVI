using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定详情面板规约器。
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
            EventBindingDetailIntent.PressDetail pressDetail => HandlePressDetail(state, pressDetail),
            EventBindingDetailIntent.Refresh refresh => HandleRefresh(state, refresh),
            _ => MviReduceResult.State<EventBindingDetailState, EventBindingDetailEffect>(state),
        };
    }

    private static MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> HandlePressDetail(
        EventBindingDetailState state,
        EventBindingDetailIntent.PressDetail intent)
    {
        string pointerText = $"Pointer {intent.Payload.Button} @ {intent.Payload.PositionX:0},{intent.Payload.PositionY:0}";
        EventBindingDetailState newState = state with
        {
            LastPointerText = pointerText,
            StatusText = "详情区域收到 PointerPressed。",
        };
        return MviReduceResult.State<EventBindingDetailState, EventBindingDetailEffect>(newState);
    }

    private static MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> HandleRefresh(
        EventBindingDetailState state,
        EventBindingDetailIntent.Refresh intent)
    {
        int refreshCount = state.RefreshCount + 1;
        EventBindingDetailState newState = state with
        {
            RefreshCount = refreshCount,
            StatusText = $"刷新动作：{intent.Payload.SourceName}",
        };
        return MviReduceResult.State<EventBindingDetailState, EventBindingDetailEffect>(newState);
    }
}
