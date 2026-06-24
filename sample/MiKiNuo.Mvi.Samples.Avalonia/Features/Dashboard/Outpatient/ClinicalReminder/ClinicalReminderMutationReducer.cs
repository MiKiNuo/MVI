using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒变更规约器。
/// </summary>
public sealed partial class ClinicalReminderMutationReducer
    : MviMutationReducerBase<ClinicalReminderState, ClinicalReminderMutation, ClinicalReminderEffect>
{
    /// <summary>
    /// 应用设置患者姓名变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ClinicalReminderState, ClinicalReminderEffect> HandleSetPatientName(
        ClinicalReminderState state,
        ClinicalReminderMutation.SetPatientName mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ClinicalReminderState, ClinicalReminderEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置提醒列表变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ClinicalReminderState, ClinicalReminderEffect> HandleSetAlerts(
        ClinicalReminderState state,
        ClinicalReminderMutation.SetAlerts mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ClinicalReminderState, ClinicalReminderEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置首要提醒变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ClinicalReminderState, ClinicalReminderEffect> HandleSetPrimaryAlert(
        ClinicalReminderState state,
        ClinicalReminderMutation.SetPrimaryAlert mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ClinicalReminderState, ClinicalReminderEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置是否有提醒变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ClinicalReminderState, ClinicalReminderEffect> HandleSetHasAlert(
        ClinicalReminderState state,
        ClinicalReminderMutation.SetHasAlert mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ClinicalReminderState, ClinicalReminderEffect>(state.Apply(mutation));
    }
}
