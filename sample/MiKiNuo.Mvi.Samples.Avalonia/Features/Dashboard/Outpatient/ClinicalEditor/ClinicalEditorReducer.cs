using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑规约器。
/// </summary>
public sealed partial class ClinicalEditorReducer
    : MviReducerBase<ClinicalEditorState, ClinicalEditorIntent, ClinicalEditorEffect>
{
    /// <summary>
    /// 处理加载患者意图。
    /// </summary>
    [MviReduce(typeof(ClinicalEditorIntent.LoadPatient))]
    private MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleLoadPatient(
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

    /// <summary>
    /// 处理修改诊断意图。
    /// </summary>
    [MviReduce(typeof(ClinicalEditorIntent.ChangeDiagnosis))]
    private MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleChangeDiagnosis(
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

    /// <summary>
    /// 处理保存草稿意图。
    /// </summary>
    [MviReduce(typeof(ClinicalEditorIntent.SaveDraft))]
    private MviReduceResult<ClinicalEditorState, ClinicalEditorEffect> HandleSaveDraft(
        ClinicalEditorState state,
        ClinicalEditorIntent.SaveDraft intent)
    {
        string message = $"{DateTime.Now:HH:mm:ss} 已保存 {state.PatientName} 的门诊病历草稿。";

        ClinicalEditorState newState = state with { SaveMessage = message };

        return MviReduceResult.State<ClinicalEditorState, ClinicalEditorEffect>(newState);
    }
}
