using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.RiskEventBoard;

/// <summary>
/// 表示风险事件 MVI 状态。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="MainValue">核心指标。</param>
/// <param name="StatusText">状态文本。</param>
/// <param name="DetailText">详情文本。</param>
/// <param name="ActionLog">动作日志。</param>
/// <param name="PrimaryActionText">主动作文本。</param>
/// <param name="SecondaryActionText">辅助动作文本。</param>
/// <param name="CanPrimaryAction">是否允许执行主动作。</param>
/// <param name="CanSecondaryAction">是否允许执行辅助动作。</param>
/// <param name="EventTitle">事件标题。</param>
/// <param name="DepartmentName">责任科室。</param>
/// <param name="SeverityLevel">风险等级。</param>
/// <param name="ResponsibleOwner">责任人。</param>
/// <param name="CorrectiveAction">整改措施。</param>
/// <param name="SubmitRiskEventText">提交事件按钮文本。</param>
/// <param name="CanSubmitRiskEvent">是否允许提交风险事件。</param>
public sealed record RiskEventBoardState(
    string Title,
    string MainValue,
    string StatusText,
    string DetailText,
    string ActionLog,
    string PrimaryActionText,
    string SecondaryActionText,
    bool CanPrimaryAction,
    bool CanSecondaryAction,
    string EventTitle,
    string DepartmentName,
    string SeverityLevel,
    string ResponsibleOwner,
    string CorrectiveAction,
    string SubmitRiskEventText,
    bool CanSubmitRiskEvent) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static RiskEventBoardState Initial { get; } = new(
        "风险事件上报 MVI",
        "待处理 9 件",
        "等待质控员录入风险事件",
        "质控员录入事件、科室、等级、责任人和整改措施，确认后联动 KPI、病历质控和整改闭环 MVI。",
        "等待风险事件录入。",
        "升级高风险事件",
        "关闭低风险事件",
        true,
        true,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        "提交风险事件",
        false);
}
