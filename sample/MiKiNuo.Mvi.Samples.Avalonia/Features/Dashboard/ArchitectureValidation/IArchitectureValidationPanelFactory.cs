using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心 2 个复用组件 ViewModel（患者检索 / 审计时间线）的工厂。
/// <para>
/// 父 <see cref="ArchitectureValidationViewModel"/> 仅持此工厂（不直接持有任何子 VM 引用），
/// 由 <see cref="ArchitectureValidationViewModel.CreatePatientSearchViewModel"/>、
/// <see cref="ArchitectureValidationViewModel.CreateAuditTimelineViewModel"/> 等方法按需解析。
/// </para>
/// <para>
/// 4 张指标卡片（中间件 / 复用 / 中介者 / 副作用）不走本工厂——它们共用 <c>CardStoreFactory</c>，
/// 由 <see cref="ArchitectureValidationViewModel.CreateMiddlewareMetricViewModel"/> 等方法解析，
/// 这与 <c>BusinessCompositePageViewModel</c> 走 <c>CardStoreFactory</c> 的范式保持一致。
/// </para>
/// </summary>
public interface IArchitectureValidationPanelFactory
{
    /// <summary>
    /// 解析复用患者检索子组件 ViewModel。
    /// </summary>
    /// <param name="contextName">上下文名称（用于面板标题）。</param>
    /// <returns>患者检索 <see cref="PatientSearchViewModel"/> 实例（缓存）。</returns>
    public object CreatePatientSearchViewModel(string contextName);

    /// <summary>
    /// 解析复用审计时间线子组件 ViewModel。
    /// </summary>
    /// <param name="contextName">上下文名称（用于面板标题）。</param>
    /// <returns>审计时间线 <see cref="AuditTimelineViewModel"/> 实例（缓存）。</returns>
    public object CreateAuditTimelineViewModel(string contextName);
}
