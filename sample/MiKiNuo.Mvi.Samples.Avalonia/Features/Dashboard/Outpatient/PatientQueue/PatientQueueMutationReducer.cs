using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列变更规约器。
/// </summary>
public sealed partial class PatientQueueMutationReducer
    : MviMutationReducerBase<PatientQueueState, PatientQueueMutation, PatientQueueEffect>
{
    /// <summary>
    /// 应用设置当前接诊索引变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientQueueState, PatientQueueEffect> HandleSetCurrentIndex(
        PatientQueueState state,
        PatientQueueMutation.SetCurrentIndex mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientQueueState, PatientQueueEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置当前患者姓名变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientQueueState, PatientQueueEffect> HandleSetSelectedPatientName(
        PatientQueueState state,
        PatientQueueMutation.SetSelectedPatientName mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientQueueState, PatientQueueEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置队列摘要变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientQueueState, PatientQueueEffect> HandleSetQueueSummary(
        PatientQueueState state,
        PatientQueueMutation.SetQueueSummary mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientQueueState, PatientQueueEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置可接诊下一位变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientQueueState, PatientQueueEffect> HandleSetCanCallNext(
        PatientQueueState state,
        PatientQueueMutation.SetCanCallNext mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientQueueState, PatientQueueEffect>(state.Apply(mutation));
    }
}
