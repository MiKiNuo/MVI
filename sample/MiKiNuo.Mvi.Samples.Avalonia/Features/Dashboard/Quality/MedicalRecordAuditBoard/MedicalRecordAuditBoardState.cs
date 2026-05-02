using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.MedicalRecordAuditBoard;

/// <summary>
/// 表示病历质控 MVI状态。
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
public sealed record MedicalRecordAuditBoardState(
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
    public static MedicalRecordAuditBoardState Initial { get; } = new(
        "病历质控 MVI",
        "待整改 31 份",
        "科室整改",
        "选择病历缺陷后会创建整改任务，并联动风险事件和 KPI。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "选择缺陷病历",
        "批量退回科室",
        true,
        true);
}
