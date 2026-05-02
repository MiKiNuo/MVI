using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.CriticalValueMonitor;

/// <summary>
/// 表示危急值闭环 MVI状态。
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
public sealed record CriticalValueMonitorState(
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
    public static CriticalValueMonitorState Initial { get; } = new(
        "危急值闭环 MVI",
        "危急值 5 条",
        "待医生确认",
        "根据标本结果触发危急值通知，确认后反向更新医嘱和 TAT。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "确认危急值",
        "电话通知医生",
        true,
        true);
}
