using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabTurnaroundBoard;

/// <summary>
/// 表示TAT 监控 MVI状态。
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
public sealed record LabTurnaroundBoardState(
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
    public static LabTurnaroundBoardState Initial { get; } = new(
        "TAT 监控 MVI",
        "超时风险 3 批",
        "倒计时",
        "从医嘱和标本组件接收节点时间，计算检验全过程 TAT。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "刷新 TAT",
        "启动超时预警",
        true,
        true);
}
