using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.MedicationSafetyPanel;

/// <summary>
/// 表示用药安全 MVI状态。
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
public sealed record MedicationSafetyPanelState(
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
    public static MedicationSafetyPanelState Initial { get; } = new(
        "用药安全 MVI",
        "风险 14 条",
        "药师复核",
        "联动处方审核和库存监控，提示抗菌药、过敏、相互作用与替代用药。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "完成药师复核",
        "升级用药会诊",
        true,
        true);
}
