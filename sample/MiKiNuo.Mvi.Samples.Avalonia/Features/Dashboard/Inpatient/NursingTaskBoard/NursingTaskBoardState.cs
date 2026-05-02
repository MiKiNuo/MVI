using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.NursingTaskBoard;

/// <summary>
/// 表示护理任务 MVI状态。
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
public sealed record NursingTaskBoardState(
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
    public static NursingTaskBoardState Initial { get; } = new(
        "护理任务 MVI",
        "待闭环 124 项",
        "高负载",
        "根据入院确认自动生成首诊护理、跌倒评估、压疮评估和腕带核对任务。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "完成首诊护理",
        "升级护士长",
        true,
        true);
}
