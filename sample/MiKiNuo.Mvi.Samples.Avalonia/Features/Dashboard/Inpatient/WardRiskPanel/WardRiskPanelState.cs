using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.WardRiskPanel;

/// <summary>
/// 表示病区风险 MVI状态。
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
public sealed record WardRiskPanelState(
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
    public static WardRiskPanelState Initial { get; } = new(
        "病区风险 MVI",
        "高危患者 18 人",
        "需评估",
        "联动入院流程和护理任务，自动评估跌倒、压疮、过敏和隔离风险。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "重新评估风险",
        "启动隔离预案",
        true,
        true);
}
