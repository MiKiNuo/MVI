using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索规约器。
/// </summary>
public sealed class PatientSearchReducer
    : MviReducerBase<PatientSearchState, PatientSearchIntent, PatientSearchEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<PatientSearchState, PatientSearchEffect> Reduce(
        PatientSearchState state,
        PatientSearchIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            PatientSearchIntent.ChangeQueryText changeQueryText => ReduceChangeQueryText(state, changeQueryText),
            PatientSearchIntent.SearchPatient => ReduceSearchPatient(state),
            PatientSearchIntent.SelectFirstPatient => ReduceSelectFirstPatient(state),
            PatientSearchIntent.ApplyExternalUpdate applyExternalUpdate => ReduceApplyExternalUpdate(state, applyExternalUpdate),
            _ => MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state),
        };
    }

    private static MviReduceResult<PatientSearchState, PatientSearchEffect> ReduceChangeQueryText(
        PatientSearchState state,
        PatientSearchIntent.ChangeQueryText intent)
    {
        bool canSearch = !string.IsNullOrWhiteSpace(intent.QueryText);
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(
            state with
            {
                QueryText = intent.QueryText,
                CanSearch = canSearch,
                CanSelectPatient = false,
                StatusText = canSearch ? "可检索患者，等待执行查询。" : "等待录入患者关键字。",
                ResultSummary = canSearch ? $"已录入关键字：{intent.QueryText}" : "请输入姓名、住院号、门诊号或身份证后检索。",
            });
    }

    private static MviReduceResult<PatientSearchState, PatientSearchEffect> ReduceSearchPatient(
        PatientSearchState state)
    {
        if (!state.CanSearch)
        {
            return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(
                state with { StatusText = "检索被拒绝：患者关键字不能为空。" });
        }

        string normalizedQuery = state.QueryText.Trim();
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(
            state with
            {
                SelectedPatientName = normalizedQuery,
                SelectedPatientNo = "P" + DateTime.Now.ToString("HHmmss"),
                ResultSummary = $"命中 3 条患者记录，默认定位：{normalizedQuery}，最近一次就诊为今日门诊/住院业务。",
                StatusText = "患者检索完成，可将患者上下文传递给当前业务页面。",
                CanSelectPatient = true,
            });
    }

    private static MviReduceResult<PatientSearchState, PatientSearchEffect> ReduceSelectFirstPatient(
        PatientSearchState state)
    {
        string statusText = state.CanSelectPatient
            ? $"已选择患者 {state.SelectedPatientName}，正在请求父页面协调兄弟 MVI。"
            : "请先检索患者，再选择患者上下文。";
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(
            state with { StatusText = statusText });
    }

    private static MviReduceResult<PatientSearchState, PatientSearchEffect> ReduceApplyExternalUpdate(
        PatientSearchState state,
        PatientSearchIntent.ApplyExternalUpdate intent)
    {
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(
            state with
            {
                ResultSummary = intent.Message,
                StatusText = "患者检索组件收到父页面或兄弟 MVI 的外部更新。",
            });
    }
}
