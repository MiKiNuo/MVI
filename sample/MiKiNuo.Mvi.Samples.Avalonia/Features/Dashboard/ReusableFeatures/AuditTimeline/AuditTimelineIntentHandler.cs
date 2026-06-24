using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示审计时间线意图处理器。
/// </summary>
public sealed class AuditTimelineIntentHandler
    : IMviIntentHandler<AuditTimelineState, AuditTimelineIntent, AuditTimelineMutation, AuditTimelineEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<AuditTimelineMutation, AuditTimelineEffect>> HandleAsync(
        AuditTimelineState state,
        AuditTimelineIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<AuditTimelineMutation, AuditTimelineEffect> result = intent switch
        {
            AuditTimelineIntent.AppendEntry appendEntry => HandleAppendEntry(state, appendEntry),
            AuditTimelineIntent.ClearEntries => HandleClearEntries(),
            _ => MviHandleResult.Empty<AuditTimelineMutation, AuditTimelineEffect>(),
        };

        return ValueTask.FromResult(result);
    }

    private static MviHandleResult<AuditTimelineMutation, AuditTimelineEffect> HandleAppendEntry(
        AuditTimelineState state,
        AuditTimelineIntent.AppendEntry intent)
    {
        string timestampedMessage = $"{DateTime.Now:HH:mm:ss} · {intent.Message}";
        string entries = state.EntryCount == 0 || state.EntriesText == "暂无审计记录。"
            ? timestampedMessage
            : $"{timestampedMessage}\n{state.EntriesText}";

        return MviHandleResult.Mutations<AuditTimelineMutation, AuditTimelineEffect>(
            new AuditTimelineMutation.SetLatestEvent(timestampedMessage),
            new AuditTimelineMutation.SetEntryCount(state.EntryCount + 1),
            new AuditTimelineMutation.SetEntriesText(entries),
            new AuditTimelineMutation.SetCanClear(true));
    }

    private static MviHandleResult<AuditTimelineMutation, AuditTimelineEffect> HandleClearEntries()
    {
        return MviHandleResult.Mutations<AuditTimelineMutation, AuditTimelineEffect>(
            new AuditTimelineMutation.SetLatestEvent("审计记录已清空。"),
            new AuditTimelineMutation.SetEntryCount(0),
            new AuditTimelineMutation.SetEntriesText("暂无审计记录。"),
            new AuditTimelineMutation.SetCanClear(false));
    }
}
