using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.AdmissionCoordinator;

/// <summary>
/// 表示入院流程 MVI 副作用。
/// </summary>
public abstract partial record AdmissionCoordinatorEffect : IMviEffect
{
    /// <summary>
    /// 表示请求提交入院登记副作用。
    /// </summary>
    /// <param name="ContextText">入院登记上下文。</param>
    public sealed partial record RequestAdmissionRegistration(string ContextText) : AdmissionCoordinatorEffect;

    /// <summary>
    /// 表示请求主业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestPrimaryWorkflow(string ContextText) : AdmissionCoordinatorEffect;

    /// <summary>
    /// 表示请求辅助业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestSecondaryWorkflow(string ContextText) : AdmissionCoordinatorEffect;
}
