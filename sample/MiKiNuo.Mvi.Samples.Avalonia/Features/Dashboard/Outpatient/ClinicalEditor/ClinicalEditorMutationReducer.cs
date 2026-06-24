using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑变更规约器。
/// </summary>
public sealed partial class ClinicalEditorMutationReducer
    : MviMutationReducerBase<ClinicalEditorState, ClinicalEditorMutation, ClinicalEditorEffect>
{
    /// <summary>
    /// 应用设置患者姓名变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleSetPatientName(
        ClinicalEditorState state,
        ClinicalEditorMutation.SetPatientName mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置诊断内容变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleSetDiagnosis(
        ClinicalEditorState state,
        ClinicalEditorMutation.SetDiagnosis mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置风险等级变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleSetRiskLevel(
        ClinicalEditorState state,
        ClinicalEditorMutation.SetRiskLevel mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置保存提示变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleSetSaveMessage(
        ClinicalEditorState state,
        ClinicalEditorMutation.SetSaveMessage mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置可保存状态变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleSetCanSave(
        ClinicalEditorState state,
        ClinicalEditorMutation.SetCanSave mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(state.Apply(mutation));
    }
}
