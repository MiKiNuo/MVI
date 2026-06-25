using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示审计时间线规约器。
/// </summary>
public sealed partial class AuditTimelineReducer
    : MviReducerBase<AuditTimelineState, AuditTimelineIntent, AuditTimelineEffect>
{
    /// <summary>
    /// 处理追加条目意图。
    /// </summary>
    [MviReduce(typeof(AuditTimelineIntent.AppendEntry))]
    private static MviReduceResult<AuditTimelineState, AuditTimelineEffect> HandleAppendEntry(
        AuditTimelineState state,
        AuditTimelineIntent.AppendEntry intent)
    {
        string timestampedMessage = $"{DateTime.Now:HH:mm:ss} · {intent.Message}";
        string entries = state.EntryCount == 0 || state.EntriesText == "暂无审计记录。"
            ? timestampedMessage
            : $"{timestampedMessage}\n{state.EntriesText}";

        AuditTimelineState newState = state with
        {
            LatestEvent = timestampedMessage,
            EntryCount = state.EntryCount + 1,
            EntriesText = entries,
            CanClear = true,
        };
        return MviReduceResult.State<AuditTimelineState, AuditTimelineEffect>(newState);
    }

    /// <summary>
    /// 处理清空条目意图。
    /// </summary>
    [MviReduce(typeof(AuditTimelineIntent.ClearEntries))]
    private static MviReduceResult<AuditTimelineState, AuditTimelineEffect> HandleClearEntries(
        AuditTimelineState state,
        AuditTimelineIntent.ClearEntries intent)
    {
        AuditTimelineState newState = state with
        {
            LatestEvent = "审计记录已清空。",
            EntryCount = 0,
            EntriesText = "暂无审计记录。",
            CanClear = false,
        };
        return MviReduceResult.State<AuditTimelineState, AuditTimelineEffect>(newState);
    }
}
