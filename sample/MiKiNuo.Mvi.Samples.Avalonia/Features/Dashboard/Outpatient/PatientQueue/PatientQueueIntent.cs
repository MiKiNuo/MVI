using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列意图。
/// </summary>
public abstract partial record PatientQueueIntent : IMviIntent
{
    /// <summary>
    /// 表示接诊下一位患者意图。
    /// </summary>
    public sealed partial record CallNext : PatientQueueIntent;
}
