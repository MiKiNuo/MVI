using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列副作用。
/// </summary>
public abstract partial record PatientQueueEffect : IMviEffect
{
    /// <summary>
    /// 表示患者被选中的副作用。
    /// </summary>
    /// <param name="PatientName">患者姓名。</param>
    public sealed partial record PatientSelected(string PatientName) : PatientQueueEffect;
}
