﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面 ViewModel。
/// <para>
/// 3 个子组件 ViewModel（候诊队列、电子病历编辑、临床提醒）由 <see cref="IOutpatientSubPanelFactory"/>
/// 工厂在构造期间静态注入并缓存；本 VM 仅持工厂引用，<b>不直接持有任何子 VM 引用</b>
/// （避免"VM-in-VM"反模式）。View 端通过 <see cref="CreateQueueViewModel"/>、
/// <see cref="CreateEditorViewModel"/>、<see cref="CreateReminderViewModel"/> 三个工厂方法
/// 按需解析子 VM，再交由 ViewRegistry 创建对应 View。
/// </para>
/// </summary>
public sealed partial class OutpatientWorkstationViewModel
    : MviViewModelBase<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationEffect>
{
    private readonly IOutpatientSubPanelFactory _subPanelFactory;

    /// <summary>
    /// 初始化门诊工作站页面 ViewModel。
    /// </summary>
    /// <param name="store">门诊工作站页面状态存储。</param>
    /// <param name="subPanelFactory">3 个子组件 ViewModel 的工厂（候诊队列 / 电子病历编辑 / 临床提醒）。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public OutpatientWorkstationViewModel(
        IMviStore<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationEffect> store,
        IOutpatientSubPanelFactory subPanelFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(subPanelFactory);
        _subPanelFactory = subPanelFactory;
    }

    /// <summary>
    /// 解析候诊队列子组件 ViewModel（经由 <see cref="IOutpatientSubPanelFactory"/> 工厂缓存返回）。
    /// </summary>
    /// <returns>候诊队列 <c>PatientQueueViewModel</c> 实例。</returns>
    public object CreateQueueViewModel() => _subPanelFactory.CreateQueueViewModel();

    /// <summary>
    /// 解析电子病历编辑子组件 ViewModel（经由 <see cref="IOutpatientSubPanelFactory"/> 工厂缓存返回）。
    /// </summary>
    /// <returns>电子病历编辑 <c>ClinicalEditorViewModel</c> 实例。</returns>
    public object CreateEditorViewModel() => _subPanelFactory.CreateEditorViewModel();

    /// <summary>
    /// 解析临床提醒子组件 ViewModel（经由 <see cref="IOutpatientSubPanelFactory"/> 工厂缓存返回）。
    /// </summary>
    /// <returns>临床提醒 <c>ClinicalReminderViewModel</c> 实例。</returns>
    public object CreateReminderViewModel() => _subPanelFactory.CreateReminderViewModel();

    /// <summary>
    /// 获取父子 MVI 与子子 MVI 交互日志。
    /// </summary>
    [MviBind(nameof(OutpatientWorkstationState.InteractionLog))]
    public partial string InteractionLog { get; private set; }
}
