using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示审计时间线规约器。
/// </summary>
public sealed class AuditTimelineReducer
    : MviReducerBase<AuditTimelineState, AuditTimelineIntent, AuditTimelineEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<AuditTimelineState, AuditTimelineEffect> Reduce(
        AuditTimelineState state,
        AuditTimelineIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            AuditTimelineIntent.AppendEntry appendEntry => HandleAppendEntry(state, appendEntry),
            AuditTimelineIntent.ClearEntries => HandleClearEntries(state),
            _ => MviReduceResult.State<AuditTimelineState, AuditTimelineEffect>(state),
        };
    }

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

    private static MviReduceResult<AuditTimelineState, AuditTimelineEffect> HandleClearEntries(
        AuditTimelineState state)
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
