using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索 MVI 副作用。
/// </summary>
public abstract partial record PatientSearchEffect : IMviEffect
{
    /// <summary>
    /// 表示请求父页面协调患者上下文副作用。
    /// </summary>
    /// <param name="PageKey">页面键。</param>
    /// <param name="PatientName">患者姓名。</param>
    /// <param name="PatientNo">患者编号。</param>
    public sealed partial record RequestPatientContext(string PageKey, string PatientName, string PatientNo) : PatientSearchEffect;
}
