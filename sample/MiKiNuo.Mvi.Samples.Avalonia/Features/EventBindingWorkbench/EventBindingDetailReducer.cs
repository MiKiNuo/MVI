using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定详情面板规约器。
/// </summary>
public sealed partial class EventBindingDetailReducer
    : MviReducerBase<EventBindingDetailState, EventBindingDetailIntent, EventBindingDetailEffect>
{
    /// <summary>
    /// 处理详情按下意图。
    /// </summary>
    [MviReduce(typeof(EventBindingDetailIntent.PressDetail))]
    private MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> HandlePressDetail(
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

    /// <summary>
    /// 处理刷新意图。
    /// </summary>
    [MviReduce(typeof(EventBindingDetailIntent.Refresh))]
    private MviReduceResult<EventBindingDetailState, EventBindingDetailEffect> HandleRefresh(
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
