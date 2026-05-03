using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示可复用审计时间线 MVI 规约器。
/// </summary>
public sealed partial class AuditTimelineReducer
    : MviReducerBase<AuditTimelineState, AuditTimelineIntent, AuditTimelineEffect>
{
    /// <summary>
    /// 处理追加审计记录意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">追加审计记录意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AuditTimelineState, AuditTimelineEffect> Reduce(
        AuditTimelineState state,
        AuditTimelineIntent.AppendEntry intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        string timestampedMessage = $"{DateTime.Now:HH:mm:ss} · {intent.Message}";
        string entries = state.EntryCount == 0 || state.EntriesText == "暂无审计记录。"
            ? timestampedMessage
            : $"{timestampedMessage}\n{state.EntriesText}";

        return MviReduceResult.State<AuditTimelineState, AuditTimelineEffect>(state with
        {
            LatestEvent = timestampedMessage,
            EntryCount = state.EntryCount + 1,
            EntriesText = entries,
            CanClear = true
        });
    }

    /// <summary>
    /// 处理清空审计记录意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">清空审计记录意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AuditTimelineState, AuditTimelineEffect> Reduce(
        AuditTimelineState state,
        AuditTimelineIntent.ClearEntries intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<AuditTimelineState, AuditTimelineEffect>(state with
        {
            LatestEvent = "审计记录已清空。",
            EntryCount = 0,
            EntriesText = "暂无审计记录。",
            CanClear = false
        });
    }
}
