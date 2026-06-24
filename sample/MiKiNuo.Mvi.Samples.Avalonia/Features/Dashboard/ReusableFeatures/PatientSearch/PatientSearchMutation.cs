using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索变更。
/// </summary>
public abstract record PatientSearchMutation : IMviMutation<PatientSearchState>
{
    /// <summary>
    /// 表示设置检索关键字的变更。
    /// </summary>
    /// <param name="Value">检索关键字。</param>
    [MviMutation(Path = "QueryText")]
    public sealed record SetQueryText(string Value) : PatientSearchMutation;

    /// <summary>
    /// 表示设置可检索状态的变更。
    /// </summary>
    /// <param name="Value">是否允许检索。</param>
    [MviMutation(Path = "CanSearch")]
    public sealed record SetCanSearch(bool Value) : PatientSearchMutation;

    /// <summary>
    /// 表示设置可选择患者状态的变更。
    /// </summary>
    /// <param name="Value">是否允许选择患者。</param>
    [MviMutation(Path = "CanSelectPatient")]
    public sealed record SetCanSelectPatient(bool Value) : PatientSearchMutation;

    /// <summary>
    /// 表示设置状态文本的变更。
    /// </summary>
    /// <param name="Value">状态文本。</param>
    [MviMutation(Path = "StatusText")]
    public sealed record SetStatusText(string Value) : PatientSearchMutation;

    /// <summary>
    /// 表示设置检索结果摘要的变更。
    /// </summary>
    /// <param name="Value">检索结果摘要。</param>
    [MviMutation(Path = "ResultSummary")]
    public sealed record SetResultSummary(string Value) : PatientSearchMutation;

    /// <summary>
    /// 表示设置选中患者姓名的变更。
    /// </summary>
    /// <param name="Value">患者姓名。</param>
    [MviMutation(Path = "SelectedPatientName")]
    public sealed record SetSelectedPatientName(string Value) : PatientSearchMutation;

    /// <summary>
    /// 表示设置选中患者编号的变更。
    /// </summary>
    /// <param name="Value">患者编号。</param>
    [MviMutation(Path = "SelectedPatientNo")]
    public sealed record SetSelectedPatientNo(string Value) : PatientSearchMutation;
}
