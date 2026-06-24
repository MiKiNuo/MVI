using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑意图处理器。
/// </summary>
public sealed class ClinicalEditorIntentHandler
    : IMviIntentHandler<ClinicalEditorState, ClinicalEditorIntent, ClinicalEditorMutation, ClinicalEditorEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<ClinicalEditorMutation, ClinicalEditorEffect>> HandleAsync(
        ClinicalEditorState state,
        ClinicalEditorIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<ClinicalEditorMutation, ClinicalEditorEffect> result = intent switch
        {
            ClinicalEditorIntent.LoadPatient loadPatient => HandleLoadPatient(loadPatient),
            ClinicalEditorIntent.ChangeDiagnosis changeDiagnosis => HandleChangeDiagnosis(state, changeDiagnosis),
            ClinicalEditorIntent.SaveDraft => HandleSaveDraft(state),
            _ => MviHandleResult.Empty<ClinicalEditorMutation, ClinicalEditorEffect>(),
        };

        return ValueTask.FromResult(result);
    }

    private static MviHandleResult<ClinicalEditorMutation, ClinicalEditorEffect> HandleLoadPatient(
        ClinicalEditorIntent.LoadPatient intent)
    {
        string riskLevel = intent.PatientName.Contains("胸闷", StringComparison.Ordinal) ? "高危" : "普通";

        return MviHandleResult.Mutations<ClinicalEditorMutation, ClinicalEditorEffect>(
            new ClinicalEditorMutation.SetPatientName(intent.PatientName),
            new ClinicalEditorMutation.SetDiagnosis(string.Empty),
            new ClinicalEditorMutation.SetRiskLevel(riskLevel),
            new ClinicalEditorMutation.SetSaveMessage("已载入患者上下文，请录入诊断。"),
            new ClinicalEditorMutation.SetCanSave(false));
    }

    private static MviHandleResult<ClinicalEditorMutation, ClinicalEditorEffect> HandleChangeDiagnosis(
        ClinicalEditorState state,
        ClinicalEditorIntent.ChangeDiagnosis intent)
    {
        bool canSave = !string.IsNullOrWhiteSpace(intent.Diagnosis) && state.PatientName != "未选择患者";

        return MviHandleResult.Mutations<ClinicalEditorMutation, ClinicalEditorEffect>(
            new ClinicalEditorMutation.SetDiagnosis(intent.Diagnosis),
            new ClinicalEditorMutation.SetSaveMessage("诊断已变更，等待保存。"),
            new ClinicalEditorMutation.SetCanSave(canSave));
    }

    private static MviHandleResult<ClinicalEditorMutation, ClinicalEditorEffect> HandleSaveDraft(
        ClinicalEditorState state)
    {
        string message = $"{DateTime.Now:HH:mm:ss} 已保存 {state.PatientName} 的门诊病历草稿。";

        return MviHandleResult.Mutations<ClinicalEditorMutation, ClinicalEditorEffect>(
            new ClinicalEditorMutation.SetSaveMessage(message));
    }
}
