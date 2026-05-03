using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索 MVI 规约器。
/// </summary>
public sealed partial class PatientSearchReducer
    : MviReducerBase<PatientSearchState, PatientSearchIntent, PatientSearchEffect>
{
    /// <summary>
    /// 处理检索关键字变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">检索关键字变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<PatientSearchState, PatientSearchEffect> Reduce(
        PatientSearchState state,
        PatientSearchIntent.ChangeQueryText intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        bool canSearch = !string.IsNullOrWhiteSpace(intent.QueryText);
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state with
        {
            QueryText = intent.QueryText,
            CanSearch = canSearch,
            CanSelectPatient = false,
            StatusText = canSearch ? "可检索患者，等待执行查询。" : "等待录入患者关键字。",
            ResultSummary = canSearch ? $"已录入关键字：{intent.QueryText}" : "请输入姓名、住院号、门诊号或身份证后检索。"
        });
    }

    /// <summary>
    /// 处理执行患者检索意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">执行患者检索意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<PatientSearchState, PatientSearchEffect> Reduce(
        PatientSearchState state,
        PatientSearchIntent.SearchPatient intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (!state.CanSearch)
        {
            return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state with
            {
                StatusText = "检索被拒绝：患者关键字不能为空。"
            });
        }

        string normalizedQuery = state.QueryText.Trim();
        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state with
        {
            SelectedPatientName = normalizedQuery,
            SelectedPatientNo = $"P{DateTime.Now:HHmmss}",
            ResultSummary = $"命中 3 条患者记录，默认定位：{normalizedQuery}，最近一次就诊为今日门诊/住院业务。",
            StatusText = "患者检索完成，可将患者上下文传递给当前业务页面。",
            CanSelectPatient = true
        });
    }

    /// <summary>
    /// 处理选择第一位患者意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">选择患者意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<PatientSearchState, PatientSearchEffect> Reduce(
        PatientSearchState state,
        PatientSearchIntent.SelectFirstPatient intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (!state.CanSelectPatient)
        {
            return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state with
            {
                StatusText = "请先检索患者，再选择患者上下文。"
            });
        }

        PatientSearchState nextState = state with
        {
            StatusText = $"已选择患者 {state.SelectedPatientName}，正在请求父页面协调兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<PatientSearchState, PatientSearchEffect>(
            nextState,
            new PatientSearchEffect.RequestPatientContext(state.PageKey, state.SelectedPatientName, state.SelectedPatientNo));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">外部更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<PatientSearchState, PatientSearchEffect> Reduce(
        PatientSearchState state,
        PatientSearchIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<PatientSearchState, PatientSearchEffect>(state with
        {
            ResultSummary = intent.Message,
            StatusText = "患者检索组件收到父页面或兄弟 MVI 的外部更新。"
        });
    }
}
