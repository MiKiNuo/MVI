﻿﻿﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;

/// <summary>
/// 表示可复用审计时间线 MVI ViewModel。
/// </summary>
public sealed partial class AuditTimelineViewModel
    : MviViewModelBase<AuditTimelineState, AuditTimelineIntent, AuditTimelineEffect>
{
    /// <summary>
    /// 初始化可复用审计时间线 MVI ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public AuditTimelineViewModel(IMviStore<AuditTimelineState, AuditTimelineIntent, AuditTimelineEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(AuditTimelineState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取所属页面键。
    /// </summary>
    [MviBind(nameof(AuditTimelineState.PageKey))]
    public partial string PageKey { get; private set; }

    /// <summary>
    /// 获取最新审计事件。
    /// </summary>
    [MviBind(nameof(AuditTimelineState.LatestEvent))]
    public partial string LatestEvent { get; private set; }

    /// <summary>
    /// 获取审计条目数量。
    /// </summary>
    [MviBind(nameof(AuditTimelineState.EntryCount))]
    public partial int EntryCount { get; private set; }

    /// <summary>
    /// 获取审计条目文本。
    /// </summary>
    [MviBind(nameof(AuditTimelineState.EntriesText))]
    public partial string EntriesText { get; private set; }

    /// <summary>
    /// 获取是否允许清空审计条目。
    /// </summary>
    [MviBind(nameof(AuditTimelineState.CanClear))]
    public partial bool CanClear { get; private set; }

    /// <summary>
    /// 获取清空审计命令。
    /// </summary>
    [MviCommand(typeof(AuditTimelineIntent.ClearEntries), CanExecuteProperty = nameof(CanClear), IsAsync = true)]
    public partial IMviAsyncCommand ClearCommand { get; private set; }
}
