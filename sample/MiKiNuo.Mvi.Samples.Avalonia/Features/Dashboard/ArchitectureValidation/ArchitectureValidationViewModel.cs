﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

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
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public ArchitectureValidationViewModel(IMviStore<ArchitectureValidationState, ArchitectureValidationIntent, ArchitectureValidationEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
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
    public partial PatientSearchViewModel PatientSearchViewModel { get; private set; }

    /// <summary>
    /// 获取复用审计时间线 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.AuditTimelineViewModel))]
    public partial AuditTimelineViewModel AuditTimelineViewModel { get; private set; }

    /// <summary>
    /// 获取中间件指标卡片 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.MiddlewareMetricViewModel))]
    public partial CardViewModel MiddlewareMetricViewModel { get; private set; }

    /// <summary>
    /// 获取复用指标卡片 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.ReuseMetricViewModel))]
    public partial CardViewModel ReuseMetricViewModel { get; private set; }

    /// <summary>
    /// 获取中介者指标卡片 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.MediatorMetricViewModel))]
    public partial CardViewModel MediatorMetricViewModel { get; private set; }

    /// <summary>
    /// 获取副作用指标卡片 ViewModel。
    /// </summary>
    [MviBind(nameof(ArchitectureValidationState.EffectMetricViewModel))]
    public partial CardViewModel EffectMetricViewModel { get; private set; }

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
