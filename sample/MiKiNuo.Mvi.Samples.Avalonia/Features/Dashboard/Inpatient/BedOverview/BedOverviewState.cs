using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedOverview;

/// <summary>
/// 表示床位总览 MVI状态。
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
public sealed record BedOverviewState(
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
    public static BedOverviewState Initial { get; } = new(
        "床位总览 MVI",
        "开放床位 186 / 220",
        "床位紧张",
        "点击急诊转入住院会触发床位候选患者选择，父页面、入院流程、护理任务和风险面板都会收到 Mediator 请求。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "急诊转入住院",
        "锁定 ICU 床位",
        true,
        true);
}
