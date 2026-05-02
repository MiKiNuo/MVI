using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心 ViewModel。
/// </summary>
public sealed partial class ArchitectureValidationViewModel
    : MviViewModelBase<ArchitectureValidationState, ArchitectureValidationIntent, ArchitectureValidationEffect>
{
    /// <summary>
    /// 初始化架构验证中心 ViewModel。
    /// </summary>
    /// <param name="store">架构验证中心状态存储。</param>
    public ArchitectureValidationViewModel(IMviStore<ArchitectureValidationState, ArchitectureValidationIntent, ArchitectureValidationEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取页面标题。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取页面说明。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.Description))]
    public partial string Description { get; private set; }

    /// <summary>
    /// 获取复用患者检索 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.PatientSearchViewModel))]
    public partial object PatientSearchViewModel { get; private set; }

    /// <summary>
    /// 获取复用审计时间线 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.AuditTimelineViewModel))]
    public partial object AuditTimelineViewModel { get; private set; }

    /// <summary>
    /// 获取中间件指标卡片 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.MiddlewareMetricViewModel))]
    public partial object MiddlewareMetricViewModel { get; private set; }

    /// <summary>
    /// 获取复用指标卡片 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.ReuseMetricViewModel))]
    public partial object ReuseMetricViewModel { get; private set; }

    /// <summary>
    /// 获取中介者指标卡片 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.MediatorMetricViewModel))]
    public partial object MediatorMetricViewModel { get; private set; }

    /// <summary>
    /// 获取副作用指标卡片 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.EffectMetricViewModel))]
    public partial object EffectMetricViewModel { get; private set; }

    /// <summary>
    /// 获取当前业务上下文。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.ActiveContext))]
    public partial string ActiveContext { get; private set; }

    /// <summary>
    /// 获取当前流程状态。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.FlowStatus))]
    public partial string FlowStatus { get; private set; }

    /// <summary>
    /// 获取交互日志。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.InteractionLog))]
    public partial string InteractionLog { get; private set; }
}
