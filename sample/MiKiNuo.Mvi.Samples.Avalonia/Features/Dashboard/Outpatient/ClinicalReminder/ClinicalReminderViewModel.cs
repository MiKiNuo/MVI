﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒 ViewModel。
/// </summary>
public sealed partial class ClinicalReminderViewModel
    : MviViewModelBase<ClinicalReminderState, ClinicalReminderIntent, ClinicalReminderEffect>
{
    /// <summary>
    /// 初始化临床提醒 ViewModel。
    /// </summary>
    /// <param name="store">临床提醒状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public ClinicalReminderViewModel(IMviStore<ClinicalReminderState, ClinicalReminderIntent, ClinicalReminderEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取患者姓名。
    /// </summary>
    [MviBind(nameof(ClinicalReminderState.PatientName))]
    public partial string PatientName { get; private set; }

    /// <summary>
    /// 获取提醒集合。
    /// </summary>
    [MviBind(nameof(ClinicalReminderState.Alerts))]
    public partial IReadOnlyList<string> Alerts { get; private set; }

    /// <summary>
    /// 获取首要提醒。
    /// </summary>
    [MviBind(nameof(ClinicalReminderState.PrimaryAlert))]
    public partial string PrimaryAlert { get; private set; }

    /// <summary>
    /// 获取是否存在提醒。
    /// </summary>
    [MviBind(nameof(ClinicalReminderState.HasAlert))]
    public partial bool HasAlert { get; private set; }

    /// <summary>
    /// 获取处理首要提醒命令。
    /// </summary>
    [MviCommand(typeof(ClinicalReminderIntent.ResolvePrimaryAlert), CanExecuteProperty = nameof(HasAlert), IsAsync = true)]
    public partial IMviAsyncCommand ResolvePrimaryAlertCommand { get; private set; }
}
