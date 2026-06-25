using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑规约器。
/// </summary>
public sealed class ClinicalEditorReducer
    : MviReducerBase<ClinicalEditorState, ClinicalEditorIntent, ClinicalEditorEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> Reduce(
        ClinicalEditorState state,
        ClinicalEditorIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            ClinicalEditorIntent.LoadPatient loadPatient => HandleLoadPatient(state, loadPatient),
            ClinicalEditorIntent.ChangeDiagnosis changeDiagnosis => HandleChangeDiagnosis(state, changeDiagnosis),
            ClinicalEditorIntent.SaveDraft => HandleSaveDraft(state),
            _ => MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(state),
        };
    }

    private static MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleLoadPatient(
        ClinicalEditorState state,
        ClinicalEditorIntent.LoadPatient intent)
    {
        string riskLevel = intent.PatientName.Contains("胸闷", StringComparison.Ordinal) ? "高危" : "普通";

        ClinicalEditorState newState = state with
        {
            PatientName = intent.PatientName,
            Diagnosis = string.Empty,
            RiskLevel = riskLevel,
            SaveMessage = "已载入患者上下文，请录入诊断。",
            CanSave = false,
        };

        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(newState);
    }

    private static MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleChangeDiagnosis(
        ClinicalEditorState state,
        ClinicalEditorIntent.ChangeDiagnosis intent)
    {
        bool canSave = !string.IsNullOrWhiteSpace(intent.Diagnosis) && state.PatientName != "未选择患者";

        ClinicalEditorState newState = state with
        {
            Diagnosis = intent.Diagnosis,
            SaveMessage = "诊断已变更，等待保存。",
            CanSave = canSave,
        };

        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(newState);
    }

    private static MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleSaveDraft(
        ClinicalEditorState state)
    {
        string message = $"{DateTime.Now:HH:mm:ss} 已保存 {state.PatientName} 的门诊病历草稿。";

        ClinicalEditorState newState = state with { SaveMessage = message };

        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(newState);
    }
}
