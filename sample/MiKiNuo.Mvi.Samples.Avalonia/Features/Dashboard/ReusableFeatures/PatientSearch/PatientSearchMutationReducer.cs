using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索变更规约器。
/// </summary>
public sealed partial class PatientSearchMutationReducer
    : MviMutationReducerBase<PatientSearchState, PatientSearchMutation, PatientSearchEffect>
{
    /// <summary>
    /// 应用设置检索关键字变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientSearchState, PatientSearchEffect> HandleSetQueryText(
        PatientSearchState state,
        PatientSearchMutation.SetQueryText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置可检索状态变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientSearchState, PatientSearchEffect> HandleSetCanSearch(
        PatientSearchState state,
        PatientSearchMutation.SetCanSearch mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置可选择患者状态变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientSearchState, PatientSearchEffect> HandleSetCanSelectPatient(
        PatientSearchState state,
        PatientSearchMutation.SetCanSelectPatient mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置状态文本变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientSearchState, PatientSearchEffect> HandleSetStatusText(
        PatientSearchState state,
        PatientSearchMutation.SetStatusText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置检索结果摘要变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientSearchState, PatientSearchEffect> HandleSetResultSummary(
        PatientSearchState state,
        PatientSearchMutation.SetResultSummary mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置选中患者姓名变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientSearchState, PatientSearchEffect> HandleSetSelectedPatientName(
        PatientSearchState state,
        PatientSearchMutation.SetSelectedPatientName mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置选中患者编号变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<PatientSearchState, PatientSearchEffect> HandleSetSelectedPatientNo(
        PatientSearchState state,
        PatientSearchMutation.SetSelectedPatientNo mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state.Apply(mutation));
    }
}
