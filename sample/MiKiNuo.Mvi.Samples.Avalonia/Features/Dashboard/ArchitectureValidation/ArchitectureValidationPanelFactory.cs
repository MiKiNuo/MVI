using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示 <see cref="IArchitectureValidationPanelFactory"/> 的标准实现。
/// <para>
/// 2 个复用组件（患者检索 / 审计时间线）与上下文名绑定——
/// 因为它们的 ViewModel 构造参数会随上下文变化，故按 contextName 缓存：
/// 同一上下文命中缓存，避免每次重建；不同上下文各持一份实例。
/// </para>
/// </summary>
public sealed class ArchitectureValidationPanelFactory : IArchitectureValidationPanelFactory
{
    private readonly Func<string, PatientSearchViewModel> _patientSearchFactory;
    private readonly Func<string, AuditTimelineViewModel> _auditTimelineFactory;
    private readonly Dictionary<string, PatientSearchViewModel> _patientSearchCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AuditTimelineViewModel> _auditTimelineCache = new(StringComparer.Ordinal);

    /// <summary>
    /// 初始化架构验证中心复用组件 ViewModel 工厂。
    /// </summary>
    /// <param name="patientSearchFactory">按 contextName 构造患者检索 ViewModel 的委托。</param>
    /// <param name="auditTimelineFactory">按 contextName 构造审计时间线 ViewModel 的委托。</param>
    public ArchitectureValidationPanelFactory(
        Func<string, PatientSearchViewModel> patientSearchFactory,
        Func<string, AuditTimelineViewModel> auditTimelineFactory)
    {
        ArgumentNullException.ThrowIfNull(patientSearchFactory);
        ArgumentNullException.ThrowIfNull(auditTimelineFactory);

        _patientSearchFactory = patientSearchFactory;
        _auditTimelineFactory = auditTimelineFactory;
    }

    /// <summary>
    /// 解析复用患者检索子组件 ViewModel。
    /// </summary>
    /// <param name="contextName">上下文名称。</param>
    /// <returns>患者检索 ViewModel 实例。</returns>
    public object CreatePatientSearchViewModel(string contextName)
    {
        ArgumentNullException.ThrowIfNull(contextName);
        if (!_patientSearchCache.TryGetValue(contextName, out PatientSearchViewModel? viewModel))
        {
            viewModel = _patientSearchFactory(contextName);
            _patientSearchCache[contextName] = viewModel;
        }

        return viewModel;
    }

    /// <summary>
    /// 解析复用审计时间线子组件 ViewModel。
    /// </summary>
    /// <param name="contextName">上下文名称。</param>
    /// <returns>审计时间线 ViewModel 实例。</returns>
    public object CreateAuditTimelineViewModel(string contextName)
    {
        ArgumentNullException.ThrowIfNull(contextName);
        if (!_auditTimelineCache.TryGetValue(contextName, out AuditTimelineViewModel? viewModel))
        {
            viewModel = _auditTimelineFactory(contextName);
            _auditTimelineCache[contextName] = viewModel;
        }

        return viewModel;
    }
}
