using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索意图处理器。
/// </summary>
public sealed class PatientSearchIntentHandler
    : IMviIntentHandler<PatientSearchState, PatientSearchIntent, PatientSearchMutation, PatientSearchEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<PatientSearchMutation, PatientSearchEffect>> HandleAsync(
        PatientSearchState state,
        PatientSearchIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<PatientSearchMutation, PatientSearchEffect> result = intent switch
        {
            PatientSearchIntent.ChangeQueryText changeQueryText => HandleChangeQueryText(state, changeQueryText),
            PatientSearchIntent.SearchPatient => HandleSearchPatient(state),
            PatientSearchIntent.SelectFirstPatient => HandleSelectFirstPatient(state),
            PatientSearchIntent.ApplyExternalUpdate applyExternalUpdate => HandleApplyExternalUpdate(state, applyExternalUpdate),
            _ => MviHandleResult.Empty<PatientSearchMutation, PatientSearchEffect>(),
        };
        return new ValueTask<MviHandleResult<PatientSearchMutation, PatientSearchEffect>>(result);
    }

    private static MviHandleResult<PatientSearchMutation, PatientSearchEffect> HandleChangeQueryText(
        PatientSearchState state,
        PatientSearchIntent.ChangeQueryText intent)
    {
        bool canSearch = !string.IsNullOrWhiteSpace(intent.QueryText);
        PatientSearchMutation[] mutations = new PatientSearchMutation[]
        {
            new PatientSearchMutation.SetQueryText(intent.QueryText),
            new PatientSearchMutation.SetCanSearch(canSearch),
            new PatientSearchMutation.SetCanSelectPatient(false),
            new PatientSearchMutation.SetStatusText(canSearch ? "可检索患者，等待执行查询。" : "等待录入患者关键字。"),
            new PatientSearchMutation.SetResultSummary(canSearch ? $"已录入关键字：{intent.QueryText}" : "请输入姓名、住院号、门诊号或身份证后检索。"),
        };
        return MviHandleResult.Mutations<PatientSearchMutation, PatientSearchEffect>(mutations);
    }

    private static MviHandleResult<PatientSearchMutation, PatientSearchEffect> HandleSearchPatient(
        PatientSearchState state)
    {
        if (!state.CanSearch)
        {
            return MviHandleResult.Mutations<PatientSearchMutation, PatientSearchEffect>(
                new PatientSearchMutation.SetStatusText("检索被拒绝：患者关键字不能为空。"));
        }

        string normalizedQuery = state.QueryText.Trim();
        PatientSearchMutation[] mutations = new PatientSearchMutation[]
        {
            new PatientSearchMutation.SetSelectedPatientName(normalizedQuery),
            new PatientSearchMutation.SetSelectedPatientNo($"P{DateTime.Now:HHmmss}"),
            new PatientSearchMutation.SetResultSummary($"命中 3 条患者记录，默认定位：{normalizedQuery}，最近一次就诊为今日门诊/住院业务。"),
            new PatientSearchMutation.SetStatusText("患者检索完成，可将患者上下文传递给当前业务页面。"),
            new PatientSearchMutation.SetCanSelectPatient(true),
        };
        return MviHandleResult.Mutations<PatientSearchMutation, PatientSearchEffect>(mutations);
    }

    private static MviHandleResult<PatientSearchMutation, PatientSearchEffect> HandleSelectFirstPatient(
        PatientSearchState state)
    {
        if (!state.CanSelectPatient)
        {
            return MviHandleResult.Mutations<PatientSearchMutation, PatientSearchEffect>(
                new PatientSearchMutation.SetStatusText("请先检索患者，再选择患者上下文。"));
        }

        PatientSearchMutation[] mutations = new PatientSearchMutation[]
        {
            new PatientSearchMutation.SetStatusText($"已选择患者 {state.SelectedPatientName}，正在请求父页面协调兄弟 MVI。"),
        };
        PatientSearchEffect[] effects = new PatientSearchEffect[]
        {
            new PatientSearchEffect.RequestPatientContext(state.PageKey, state.SelectedPatientName, state.SelectedPatientNo),
        };
        return MviHandleResult.MutationsAndEffects<PatientSearchMutation, PatientSearchEffect>(mutations, effects);
    }

    private static MviHandleResult<PatientSearchMutation, PatientSearchEffect> HandleApplyExternalUpdate(
        PatientSearchState state,
        PatientSearchIntent.ApplyExternalUpdate intent)
    {
        PatientSearchMutation[] mutations = new PatientSearchMutation[]
        {
            new PatientSearchMutation.SetResultSummary(intent.Message),
            new PatientSearchMutation.SetStatusText("患者检索组件收到父页面或兄弟 MVI 的外部更新。"),
        };
        return MviHandleResult.Mutations<PatientSearchMutation, PatientSearchEffect>(mutations);
    }
}
