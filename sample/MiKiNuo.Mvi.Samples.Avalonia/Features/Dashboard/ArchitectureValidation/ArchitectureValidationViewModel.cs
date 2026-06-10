﻿using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心 ViewModel。
/// <para>
/// 6 个子组件 ViewModel（2 个复用组件 + 4 个指标卡）由 DI 容器在构造本 VM 时静态注入，
/// 不会随 State 变化而重建（State 仅保留上下文/状态/日志 3 个字段）。
/// </para>
/// </summary>
public sealed partial class ArchitectureValidationViewModel
    : MviViewModelBase<ArchitectureValidationState, ArchitectureValidationIntent, ArchitectureValidationEffect>
{
    /// <summary>
    /// 初始化架构验证中心 ViewModel。
    /// </summary>
    /// <param name="store">架构验证中心状态存储。</param>
    /// <param name="patientSearchViewModel">复用患者检索子组件 ViewModel。</param>
    /// <param name="auditTimelineViewModel">复用审计时间线子组件 ViewModel。</param>
    /// <param name="middlewareMetricViewModel">中间件指标卡片 ViewModel。</param>
    /// <param name="reuseMetricViewModel">复用指标卡片 ViewModel。</param>
    /// <param name="mediatorMetricViewModel">中介者指标卡片 ViewModel。</param>
    /// <param name="effectMetricViewModel">副作用指标卡片 ViewModel。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public ArchitectureValidationViewModel(
        IMviStore<ArchitectureValidationState, ArchitectureValidationIntent, ArchitectureValidationEffect> store,
        PatientSearchViewModel patientSearchViewModel,
        AuditTimelineViewModel auditTimelineViewModel,
        CardViewModel middlewareMetricViewModel,
        CardViewModel reuseMetricViewModel,
        CardViewModel mediatorMetricViewModel,
        CardViewModel effectMetricViewModel,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(patientSearchViewModel);
        ArgumentNullException.ThrowIfNull(auditTimelineViewModel);
        ArgumentNullException.ThrowIfNull(middlewareMetricViewModel);
        ArgumentNullException.ThrowIfNull(reuseMetricViewModel);
        ArgumentNullException.ThrowIfNull(mediatorMetricViewModel);
        ArgumentNullException.ThrowIfNull(effectMetricViewModel);

        PatientSearchViewModel = patientSearchViewModel;
        AuditTimelineViewModel = auditTimelineViewModel;
        MiddlewareMetricViewModel = middlewareMetricViewModel;
        ReuseMetricViewModel = reuseMetricViewModel;
        MediatorMetricViewModel = mediatorMetricViewModel;
        EffectMetricViewModel = effectMetricViewModel;
    }

    /// <summary>
    /// 获取复用患者检索 ViewModel（构造函数注入，不在 State 中）。
    /// </summary>
    public PatientSearchViewModel PatientSearchViewModel { get; }

    /// <summary>
    /// 获取复用审计时间线 ViewModel（构造函数注入，不在 State 中）。
    /// </summary>
    public AuditTimelineViewModel AuditTimelineViewModel { get; }

    /// <summary>
    /// 获取中间件指标卡片 ViewModel（构造函数注入，不在 State 中）。
    /// </summary>
    public CardViewModel MiddlewareMetricViewModel { get; }

    /// <summary>
    /// 获取复用指标卡片 ViewModel（构造函数注入，不在 State 中）。
    /// </summary>
    public CardViewModel ReuseMetricViewModel { get; }

    /// <summary>
    /// 获取中介者指标卡片 ViewModel（构造函数注入，不在 State 中）。
    /// </summary>
    public CardViewModel MediatorMetricViewModel { get; }

    /// <summary>
    /// 获取副作用指标卡片 ViewModel（构造函数注入，不在 State 中）。
    /// </summary>
    public CardViewModel EffectMetricViewModel { get; }

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
