﻿using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面 ViewModel。
/// <para>
/// 3 个子组件 ViewModel（候诊队列、电子病历编辑、临床提醒）由 DI 容器在构造本 VM 时静态注入，
/// 不会随 State 变化而重建（State 仅持有日志字段）。View 端通过强类型属性读取后传给 ViewRegistry。
/// </para>
/// </summary>
public sealed partial class OutpatientWorkstationViewModel
    : MviViewModelBase<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationEffect>
{
    /// <summary>
    /// 初始化门诊工作站页面 ViewModel。
    /// </summary>
    /// <param name="store">门诊工作站页面状态存储。</param>
    /// <param name="queueViewModel">候诊队列子组件 ViewModel。</param>
    /// <param name="clinicalEditorViewModel">电子病历编辑子组件 ViewModel。</param>
    /// <param name="clinicalReminderViewModel">临床提醒子组件 ViewModel。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public OutpatientWorkstationViewModel(
        IMviStore<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationEffect> store,
        PatientQueueViewModel queueViewModel,
        ClinicalEditorViewModel clinicalEditorViewModel,
        ClinicalReminderViewModel clinicalReminderViewModel,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(queueViewModel);
        ArgumentNullException.ThrowIfNull(clinicalEditorViewModel);
        ArgumentNullException.ThrowIfNull(clinicalReminderViewModel);

        QueueViewModel = queueViewModel;
        ClinicalEditorViewModel = clinicalEditorViewModel;
        ClinicalReminderViewModel = clinicalReminderViewModel;
    }

    /// <summary>
    /// 获取候诊队列 ViewModel（构造函数注入，不在 State 中）。
    /// </summary>
    public PatientQueueViewModel QueueViewModel { get; }

    /// <summary>
    /// 获取电子病历编辑 ViewModel（构造函数注入，不在 State 中）。
    /// </summary>
    public ClinicalEditorViewModel ClinicalEditorViewModel { get; }

    /// <summary>
    /// 获取临床提醒 ViewModel（构造函数注入，不在 State 中）。
    /// </summary>
    public ClinicalReminderViewModel ClinicalReminderViewModel { get; }

    /// <summary>
    /// 获取父子 MVI 与子子 MVI 交互日志。
    /// </summary>
    [MviBind(nameof(OutpatientWorkstationState.InteractionLog))]
    public partial string InteractionLog { get; private set; }
}
