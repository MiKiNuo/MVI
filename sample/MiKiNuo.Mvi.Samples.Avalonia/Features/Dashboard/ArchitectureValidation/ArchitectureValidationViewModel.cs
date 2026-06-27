﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心 ViewModel。
/// <para>
/// 6 个子组件 ViewModel（2 个复用组件 + 4 个指标卡）通过工厂按需解析：
/// </para>
/// <list type="bullet">
/// <item>复用组件（患者检索 / 审计时间线）由 <see cref="IArchitectureValidationPanelFactory"/> 解析，
///   View 通过 <see cref="CreatePatientSearchViewModel(string)"/> / <see cref="CreateAuditTimelineViewModel(string)"/> 工厂方法获取。</item>
/// <item>4 张指标卡片由 <see cref="CardStoreFactory"/> 解析（与业务页 16 张卡片共享同一 store/VM 实例），
///   View 通过 <see cref="CreateMiddlewareMetricViewModel"/> / <see cref="CreateReuseMetricViewModel"/> /
///   <see cref="CreateMediatorMetricViewModel"/> / <see cref="CreateEffectMetricViewModel"/> 工厂方法获取。</item>
/// </list>
/// <para>
/// 本 VM <b>不直接持有任何子 VM 引用</b>，避免"VM-in-VM"反模式。
/// </para>
/// </summary>
public sealed partial class ArchitectureValidationViewModel
    : MviViewModelBase<ArchitectureValidationState, ArchitectureValidationIntent, UnitEffect>
{
    private readonly IArchitectureValidationPanelFactory _panelFactory;
    private readonly CardStoreFactory _cardStoreFactory;

    /// <summary>
    /// 初始化架构验证中心 ViewModel。
    /// </summary>
    /// <param name="store">架构验证中心状态存储。</param>
    /// <param name="panelFactory">复用组件（患者检索 / 审计时间线）ViewModel 工厂。</param>
    /// <param name="cardStoreFactory">指标卡片 MVI Store / ViewModel 工厂（提供 4 张指标卡片）。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public ArchitectureValidationViewModel(
        IMviStore<ArchitectureValidationState, ArchitectureValidationIntent, UnitEffect> store,
        IArchitectureValidationPanelFactory panelFactory,
        CardStoreFactory cardStoreFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(panelFactory);
        ArgumentNullException.ThrowIfNull(cardStoreFactory);

        _panelFactory = panelFactory;
        _cardStoreFactory = cardStoreFactory;
    }

    /// <summary>
    /// 解析复用患者检索子组件 ViewModel（经由 <see cref="IArchitectureValidationPanelFactory"/> 工厂缓存返回）。
    /// </summary>
    /// <param name="contextName">上下文名称（用于面板标题）。</param>
    /// <returns>患者检索 <c>PatientSearchViewModel</c> 实例。</returns>
    public object CreatePatientSearchViewModel(string contextName) =>
        _panelFactory.CreatePatientSearchViewModel(contextName);

    /// <summary>
    /// 解析复用患者检索子组件 ViewModel（使用当前 <see cref="ActiveContext"/> 作为上下文名称）。
    /// 供 <c>[MviSlot]</c> 源生成器 emit 的 <c>OnBindSlots</c> 钩子调用。
    /// </summary>
    /// <returns>患者检索 <c>PatientSearchViewModel</c> 实例。</returns>
    public object CreatePatientSearchViewModel() => CreatePatientSearchViewModel(ActiveContext);

    /// <summary>
    /// 解析复用审计时间线子组件 ViewModel（经由 <see cref="IArchitectureValidationPanelFactory"/> 工厂缓存返回）。
    /// </summary>
    /// <param name="contextName">上下文名称（用于面板标题）。</param>
    /// <returns>审计时间线 <c>AuditTimelineViewModel</c> 实例。</returns>
    public object CreateAuditTimelineViewModel(string contextName) =>
        _panelFactory.CreateAuditTimelineViewModel(contextName);

    /// <summary>
    /// 解析复用审计时间线子组件 ViewModel（使用当前 <see cref="ActiveContext"/> 作为上下文名称）。
    /// 供 <c>[MviSlot]</c> 源生成器 emit 的 <c>OnBindSlots</c> 钩子调用。
    /// </summary>
    /// <returns>审计时间线 <c>AuditTimelineViewModel</c> 实例。</returns>
    public object CreateAuditTimelineViewModel() => CreateAuditTimelineViewModel(ActiveContext);

    /// <summary>
    /// 解析中间件指标卡片 ViewModel（经由 <see cref="CardStoreFactory"/> 共享 store/VM 实例）。
    /// </summary>
    /// <returns>中间件指标 <c>CardViewModel</c> 实例。</returns>
    public object CreateMiddlewareMetricViewModel() => _cardStoreFactory.GetViewModel(PageKey.MiddlewareMetric);

    /// <summary>
    /// 解析复用指标卡片 ViewModel（经由 <see cref="CardStoreFactory"/> 共享 store/VM 实例）。
    /// </summary>
    /// <returns>复用指标 <c>CardViewModel</c> 实例。</returns>
    public object CreateReuseMetricViewModel() => _cardStoreFactory.GetViewModel(PageKey.ReuseMetric);

    /// <summary>
    /// 解析中介者指标卡片 ViewModel（经由 <see cref="CardStoreFactory"/> 共享 store/VM 实例）。
    /// </summary>
    /// <returns>中介者指标 <c>CardViewModel</c> 实例。</returns>
    public object CreateMediatorMetricViewModel() => _cardStoreFactory.GetViewModel(PageKey.MediatorMetric);

    /// <summary>
    /// 解析副作用指标卡片 ViewModel（经由 <see cref="CardStoreFactory"/> 共享 store/VM 实例）。
    /// </summary>
    /// <returns>副作用指标 <c>CardViewModel</c> 实例。</returns>
    public object CreateEffectMetricViewModel() => _cardStoreFactory.GetViewModel(PageKey.EffectMetric);

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
