using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabOrderComposer;

/// <summary>
/// 表示医嘱开立 MVI ViewModel。
/// </summary>
public sealed partial class LabOrderComposerViewModel
    : MviViewModelBase<LabOrderComposerState, LabOrderComposerIntent, LabOrderComposerEffect>
{
    /// <summary>
    /// 初始化医嘱开立 MVI ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    public LabOrderComposerViewModel(IMviStore<LabOrderComposerState, LabOrderComposerIntent, LabOrderComposerEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取核心指标。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.MainValue))]
    public partial string MainValue { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>
    /// 获取详情文本。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.DetailText))]
    public partial string DetailText { get; private set; }

    /// <summary>
    /// 获取动作日志。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.ActionLog))]
    public partial string ActionLog { get; private set; }

    /// <summary>
    /// 获取主动作文本。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.PrimaryActionText))]
    public partial string PrimaryActionText { get; private set; }

    /// <summary>
    /// 获取辅助动作文本。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.SecondaryActionText))]
    public partial string SecondaryActionText { get; private set; }

    /// <summary>
    /// 获取是否允许执行主动作。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.CanPrimaryAction))]
    public partial bool CanPrimaryAction { get; private set; }

    /// <summary>
    /// 获取是否允许执行辅助动作。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.CanSecondaryAction))]
    public partial bool CanSecondaryAction { get; private set; }

    /// <summary>
    /// 获取或设置患者标识。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.PatientIdentity), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(LabOrderComposerIntent.ChangePatientIdentity))]
    public partial string PatientIdentity { get; set; }

    /// <summary>
    /// 获取或设置检验项目。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.TestItem), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(LabOrderComposerIntent.ChangeTestItem))]
    public partial string TestItem { get; set; }

    /// <summary>
    /// 获取或设置优先级。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.PriorityLevel), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(LabOrderComposerIntent.ChangePriorityLevel))]
    public partial string PriorityLevel { get; set; }

    /// <summary>
    /// 获取或设置标本类型。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.SpecimenType), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(LabOrderComposerIntent.ChangeSpecimenType))]
    public partial string SpecimenType { get; set; }

    /// <summary>
    /// 获取或设置临床指征。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.ClinicalIndication), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(LabOrderComposerIntent.ChangeClinicalIndication))]
    public partial string ClinicalIndication { get; set; }

    /// <summary>
    /// 获取提交医嘱按钮文本。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.SubmitOrderText))]
    public partial string SubmitOrderText { get; private set; }

    /// <summary>
    /// 获取是否允许提交医嘱。
    /// </summary>
    [MviBind(nameof(LabOrderComposerState.CanSubmitOrder))]
    public partial bool CanSubmitOrder { get; private set; }

    /// <summary>
    /// 获取提交医嘱命令。
    /// </summary>
    [MviCommand(typeof(LabOrderComposerIntent.SubmitLabOrderForm), CanExecuteProperty = nameof(CanSubmitOrder), IsAsync = true)]
    public partial IMviAsyncCommand SubmitLabOrderCommand { get; private set; }

    /// <summary>
    /// 获取主动作命令。
    /// </summary>
    [MviCommand(typeof(LabOrderComposerIntent.ExecutePrimaryAction), CanExecuteProperty = nameof(CanPrimaryAction), IsAsync = true)]
    public partial IMviAsyncCommand PrimaryActionCommand { get; private set; }

    /// <summary>
    /// 获取辅助动作命令。
    /// </summary>
    [MviCommand(typeof(LabOrderComposerIntent.ExecuteSecondaryAction), CanExecuteProperty = nameof(CanSecondaryAction), IsAsync = true)]
    public partial IMviAsyncCommand SecondaryActionCommand { get; private set; }
}
