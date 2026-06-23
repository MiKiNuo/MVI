﻿﻿﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索 MVI ViewModel。
/// </summary>
public sealed partial class PatientSearchViewModel
    : MviViewModelBase<PatientSearchState, PatientSearchIntent, PatientSearchEffect>
{
    /// <summary>
    /// 初始化可复用患者检索 MVI ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public PatientSearchViewModel(IMviStore<PatientSearchState, PatientSearchIntent, PatientSearchEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(PatientSearchState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取所属页面键。
    /// </summary>
    [MviBind(nameof(PatientSearchState.PageKey))]
    public partial string PageKey { get; private set; }

    /// <summary>
    /// 获取或设置检索关键字。
    /// </summary>
    [MviBind(nameof(PatientSearchState.QueryText), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(PatientSearchIntent.ChangeQueryText))]
    public partial string QueryText { get; set; }

    /// <summary>
    /// 获取选中患者姓名。
    /// </summary>
    [MviBind(nameof(PatientSearchState.SelectedPatientName))]
    public partial string SelectedPatientName { get; private set; }

    /// <summary>
    /// 获取选中患者编号。
    /// </summary>
    [MviBind(nameof(PatientSearchState.SelectedPatientNo))]
    public partial string SelectedPatientNo { get; private set; }

    /// <summary>
    /// 获取检索结果摘要。
    /// </summary>
    [MviBind(nameof(PatientSearchState.ResultSummary))]
    public partial string ResultSummary { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(PatientSearchState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>
    /// 获取是否允许检索。
    /// </summary>
    [MviBind(nameof(PatientSearchState.CanSearch))]
    public partial bool CanSearch { get; private set; }

    /// <summary>
    /// 获取是否允许选择患者。
    /// </summary>
    [MviBind(nameof(PatientSearchState.CanSelectPatient))]
    public partial bool CanSelectPatient { get; private set; }

    /// <summary>
    /// 获取执行检索命令。
    /// </summary>
    [MviCommand(typeof(PatientSearchIntent.SearchPatient), CanExecuteProperty = nameof(CanSearch), IsAsync = true)]
    public partial IMviAsyncCommand SearchCommand { get; private set; }

    /// <summary>
    /// 获取选择患者命令。
    /// </summary>
    [MviCommand(typeof(PatientSearchIntent.SelectFirstPatient), CanExecuteProperty = nameof(CanSelectPatient), IsAsync = true)]
    public partial IMviAsyncCommand SelectPatientCommand { get; private set; }
}
