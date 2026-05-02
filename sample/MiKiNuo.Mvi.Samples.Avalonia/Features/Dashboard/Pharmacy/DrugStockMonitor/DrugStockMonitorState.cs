using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.DrugStockMonitor;

/// <summary>
/// 表示库存监控 MVI状态。
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
public sealed record DrugStockMonitorState(
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
    public static DrugStockMonitorState Initial { get; } = new(
        "库存监控 MVI",
        "预警 23 项",
        "需补货",
        "接收处方预占请求，库存低于安全线时通知补货计划和用药安全组件。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "触发低库存",
        "完成入库",
        true,
        true);
}
