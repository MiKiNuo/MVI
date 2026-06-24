using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列变更。
/// </summary>
public abstract record PatientQueueMutation : IMviMutation<PatientQueueState>
{
    /// <summary>
    /// 表示设置当前接诊索引的变更。
    /// </summary>
    /// <param name="Value">接诊索引。</param>
    [MviMutation(Path = "CurrentIndex")]
    public sealed record SetCurrentIndex(int Value) : PatientQueueMutation;

    /// <summary>
    /// 表示设置当前患者姓名的变更。
    /// </summary>
    /// <param name="Value">患者姓名。</param>
    [MviMutation(Path = "SelectedPatientName")]
    public sealed record SetSelectedPatientName(string Value) : PatientQueueMutation;

    /// <summary>
    /// 表示设置队列摘要的变更。
    /// </summary>
    /// <param name="Value">队列摘要。</param>
    [MviMutation(Path = "QueueSummary")]
    public sealed record SetQueueSummary(string Value) : PatientQueueMutation;

    /// <summary>
    /// 表示设置可接诊下一位的变更。
    /// </summary>
    /// <param name="Value">是否可接诊下一位。</param>
    [MviMutation(Path = "CanCallNext")]
    public sealed record SetCanCallNext(bool Value) : PatientQueueMutation;
}
