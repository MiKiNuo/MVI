using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示审计时间线变更规约器。
/// </summary>
public sealed partial class AuditTimelineMutationReducer
    : MviMutationReducerBase<AuditTimelineState, AuditTimelineMutation, AuditTimelineEffect>
{
    /// <summary>
    /// 应用设置最新事件变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<AuditTimelineState, AuditTimelineEffect> HandleSetLatestEvent(
        AuditTimelineState state,
        AuditTimelineMutation.SetLatestEvent mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<AuditTimelineState, AuditTimelineEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置条目数变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<AuditTimelineState, AuditTimelineEffect> HandleSetEntryCount(
        AuditTimelineState state,
        AuditTimelineMutation.SetEntryCount mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<AuditTimelineState, AuditTimelineEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置条目文本变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<AuditTimelineState, AuditTimelineEffect> HandleSetEntriesText(
        AuditTimelineState state,
        AuditTimelineMutation.SetEntriesText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<AuditTimelineState, AuditTimelineEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置可清空状态变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<AuditTimelineState, AuditTimelineEffect> HandleSetCanClear(
        AuditTimelineState state,
        AuditTimelineMutation.SetCanClear mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<AuditTimelineState, AuditTimelineEffect>(state.Apply(mutation));
    }
}
