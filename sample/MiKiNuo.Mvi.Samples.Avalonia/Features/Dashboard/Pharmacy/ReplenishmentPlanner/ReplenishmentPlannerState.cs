using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.ReplenishmentPlanner;

/// <summary>
/// 表示补货计划 MVI状态。
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
public sealed record ReplenishmentPlannerState(
    string Title,
    string MainValue,
    string StatusText,
    string DetailText,
    string ActionLog,
    string PrimaryActionText,
    string SecondaryActionText,
    bool CanPrimaryAction,
    bool CanSecondaryAction) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static ReplenishmentPlannerState Initial { get; } = new(
        "补货计划 MVI",
        "采购 8 单",
        "待确认",
        "根据库存预警和处方消耗生成补货计划，确认采购后反向通知库存组件。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "确认采购单",
        "切换供应商",
        true,
        true);
}
