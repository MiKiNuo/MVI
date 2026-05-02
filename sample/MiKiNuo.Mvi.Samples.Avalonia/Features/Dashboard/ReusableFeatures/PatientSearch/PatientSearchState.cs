using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索 MVI 状态。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="PageKey">所属页面键。</param>
/// <param name="QueryText">检索关键字。</param>
/// <param name="SelectedPatientName">选中患者姓名。</param>
/// <param name="SelectedPatientNo">选中患者编号。</param>
/// <param name="ResultSummary">检索结果摘要。</param>
/// <param name="StatusText">状态文本。</param>
/// <param name="CanSearch">是否允许检索。</param>
/// <param name="CanSelectPatient">是否允许选择患者。</param>
public sealed record PatientSearchState(
    string Title,
    string PageKey,
    string QueryText,
    string SelectedPatientName,
    string SelectedPatientNo,
    string ResultSummary,
    string StatusText,
    bool CanSearch,
    bool CanSelectPatient) : IMviState
{
    /// <summary>
    /// 创建指定页面的初始患者检索状态。
    /// </summary>
    /// <param name="pageKey">所属页面键。</param>
    /// <returns>初始状态。</returns>
    public static PatientSearchState CreateInitial(string pageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);

        return new PatientSearchState(
            "复用患者检索 MVI",
            pageKey,
            string.Empty,
            "未选择患者",
            "-",
            "请输入姓名、住院号、门诊号或身份证后检索。",
            "等待录入患者关键字。",
            false,
            false);
    }
}
