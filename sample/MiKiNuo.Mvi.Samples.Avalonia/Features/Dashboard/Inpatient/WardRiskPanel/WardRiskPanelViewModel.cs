﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.WardRiskPanel;

/// <summary>
/// 表示病区风险 MVI ViewModel。
/// </summary>
public sealed partial class WardRiskPanelViewModel
    : MviViewModelBase<WardRiskPanelState, WardRiskPanelIntent, WardRiskPanelEffect>
{
    /// <summary>
    /// 初始化病区风险 MVI ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public WardRiskPanelViewModel(IMviStore<WardRiskPanelState, WardRiskPanelIntent, WardRiskPanelEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(WardRiskPanelState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取核心指标。
    /// </summary>
    [MviBind(nameof(WardRiskPanelState.MainValue))]
    public partial string MainValue { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(WardRiskPanelState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>
    /// 获取详情文本。
    /// </summary>
    [MviBind(nameof(WardRiskPanelState.DetailText))]
    public partial string DetailText { get; private set; }

    /// <summary>
    /// 获取动作日志。
    /// </summary>
    [MviBind(nameof(WardRiskPanelState.ActionLog))]
    public partial string ActionLog { get; private set; }

    /// <summary>
    /// 获取主动作文本。
    /// </summary>
    [MviBind(nameof(WardRiskPanelState.PrimaryActionText))]
    public partial string PrimaryActionText { get; private set; }

    /// <summary>
    /// 获取辅助动作文本。
    /// </summary>
    [MviBind(nameof(WardRiskPanelState.SecondaryActionText))]
    public partial string SecondaryActionText { get; private set; }

    /// <summary>
    /// 获取是否允许执行主动作。
    /// </summary>
    [MviBind(nameof(WardRiskPanelState.CanPrimaryAction))]
    public partial bool CanPrimaryAction { get; private set; }

    /// <summary>
    /// 获取是否允许执行辅助动作。
    /// </summary>
    [MviBind(nameof(WardRiskPanelState.CanSecondaryAction))]
    public partial bool CanSecondaryAction { get; private set; }

    /// <summary>
    /// 获取主动作命令。
    /// </summary>
    [MviCommand(typeof(WardRiskPanelIntent.ExecutePrimaryAction), CanExecuteProperty = nameof(CanPrimaryAction), IsAsync = true)]
    public partial IMviAsyncCommand PrimaryActionCommand { get; private set; }

    /// <summary>
    /// 获取辅助动作命令。
    /// </summary>
    [MviCommand(typeof(WardRiskPanelIntent.ExecuteSecondaryAction), CanExecuteProperty = nameof(CanSecondaryAction), IsAsync = true)]
    public partial IMviAsyncCommand SecondaryActionCommand { get; private set; }
}
