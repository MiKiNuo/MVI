using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.QualityKpiBoard;

/// <summary>
/// 表示质量 KPI MVI状态。
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
public sealed record QualityKpiBoardState(
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
    public static QualityKpiBoardState Initial { get; } = new(
        "质量 KPI MVI",
        "综合达标 97.6%",
        "持续监控",
        "接收病历缺陷、风险事件和整改完成结果，实时更新质控指标。",
        "等待父页面或兄弟 MVI 触发业务流。",
        "刷新 KPI",
        "发布院周报",
        true,
        true);
}
