using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列状态。
/// </summary>
/// <param name="Patients">候诊患者集合。</param>
/// <param name="CurrentIndex">当前接诊索引。</param>
/// <param name="SelectedPatientName">当前接诊患者姓名。</param>
/// <param name="QueueSummary">队列摘要。</param>
/// <param name="CanCallNext">是否可以接诊下一位。</param>
public sealed record PatientQueueState(
    IReadOnlyList<string> Patients,
    int CurrentIndex,
    string SelectedPatientName,
    string QueueSummary,
    bool CanCallNext) : IMviState
{
    /// <summary>
    /// 获取初始门诊队列状态。
    /// </summary>
    public static PatientQueueState Initial { get; } = new(
        ["张三 · 男 · 35岁 · 发热", "李梅 · 女 · 29岁 · 复诊", "王强 · 男 · 62岁 · 胸闷", "赵敏 · 女 · 41岁 · 高血压"],
        -1,
        "未接诊",
        "候诊 4 人，平均等待 18 分钟。",
        true);
}
