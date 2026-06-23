﻿﻿﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;

/// <summary>
/// 表示门诊病历编辑 ViewModel。
/// </summary>
public sealed partial class ClinicalEditorViewModel
    : MviViewModelBase<ClinicalEditorState, ClinicalEditorIntent, ClinicalEditorEffect>
{
    /// <summary>
    /// 初始化门诊病历编辑 ViewModel。
    /// </summary>
    /// <param name="store">病历编辑状态存储。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public ClinicalEditorViewModel(IMviStore<ClinicalEditorState, ClinicalEditorIntent, ClinicalEditorEffect> store, IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取患者姓名。
    /// </summary>
    [MviBind(nameof(ClinicalEditorState.PatientName))]
    public partial string PatientName { get; private set; }

    /// <summary>
    /// 获取或设置诊断内容。
    /// </summary>
    [MviBind(
        nameof(ClinicalEditorState.Diagnosis),
        BindingMode = MviBindingMode.TwoWay,
        IntentType = typeof(ClinicalEditorIntent.ChangeDiagnosis))]
    public partial string Diagnosis { get; set; }

    /// <summary>
    /// 获取风险等级。
    /// </summary>
    [MviBind(nameof(ClinicalEditorState.RiskLevel))]
    public partial string RiskLevel { get; private set; }

    /// <summary>
    /// 获取保存提示。
    /// </summary>
    [MviBind(nameof(ClinicalEditorState.SaveMessage))]
    public partial string SaveMessage { get; private set; }

    /// <summary>
    /// 获取是否可以保存。
    /// </summary>
    [MviBind(nameof(ClinicalEditorState.CanSave))]
    public partial bool CanSave { get; private set; }

    /// <summary>
    /// 获取保存草稿命令。
    /// </summary>
    [MviCommand(typeof(ClinicalEditorIntent.SaveDraft), CanExecuteProperty = nameof(CanSave), IsAsync = true)]
    public partial IMviAsyncCommand SaveDraftCommand { get; private set; }
}
