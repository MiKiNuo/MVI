using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.RiskEventBoard;

/// <summary>
/// 表示风险事件 MVI 意图。
/// </summary>
public abstract partial record RiskEventBoardIntent : IMviIntent
{
    /// <summary>
    /// 表示修改事件标题意图。
    /// </summary>
    /// <param name="EventTitle">事件标题。</param>
    public sealed partial record ChangeEventTitle(string EventTitle) : RiskEventBoardIntent;

    /// <summary>
    /// 表示修改责任科室意图。
    /// </summary>
    /// <param name="DepartmentName">责任科室。</param>
    public sealed partial record ChangeDepartmentName(string DepartmentName) : RiskEventBoardIntent;

    /// <summary>
    /// 表示修改风险等级意图。
    /// </summary>
    /// <param name="SeverityLevel">风险等级。</param>
    public sealed partial record ChangeSeverityLevel(string SeverityLevel) : RiskEventBoardIntent;

    /// <summary>
    /// 表示修改责任人意图。
    /// </summary>
    /// <param name="ResponsibleOwner">责任人。</param>
    public sealed partial record ChangeResponsibleOwner(string ResponsibleOwner) : RiskEventBoardIntent;

    /// <summary>
    /// 表示修改整改措施意图。
    /// </summary>
    /// <param name="CorrectiveAction">整改措施。</param>
    public sealed partial record ChangeCorrectiveAction(string CorrectiveAction) : RiskEventBoardIntent;

    /// <summary>
    /// 表示提交风险事件意图。
    /// </summary>
    public sealed partial record SubmitRiskEventForm : RiskEventBoardIntent;

    /// <summary>
    /// 表示执行主业务动作意图。
    /// </summary>
    public sealed partial record ExecutePrimaryAction : RiskEventBoardIntent;

    /// <summary>
    /// 表示执行辅助业务动作意图。
    /// </summary>
    public sealed partial record ExecuteSecondaryAction : RiskEventBoardIntent;

    /// <summary>
    /// 表示应用来自父页面或兄弟 MVI 的外部更新意图。
    /// </summary>
    /// <param name="Message">外部更新消息。</param>
    public sealed partial record ApplyExternalUpdate(string Message) : RiskEventBoardIntent;
}
