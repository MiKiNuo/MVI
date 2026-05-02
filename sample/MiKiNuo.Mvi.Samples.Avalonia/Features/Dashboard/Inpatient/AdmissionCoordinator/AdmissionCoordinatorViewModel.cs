using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.AdmissionCoordinator;

/// <summary>
/// 表示入院流程 MVI ViewModel。
/// </summary>
public sealed partial class AdmissionCoordinatorViewModel
    : MviViewModelBase<AdmissionCoordinatorState, AdmissionCoordinatorIntent, AdmissionCoordinatorEffect>
{
    /// <summary>
    /// 初始化入院流程 MVI ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    public AdmissionCoordinatorViewModel(IMviStore<AdmissionCoordinatorState, AdmissionCoordinatorIntent, AdmissionCoordinatorEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取核心指标。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.MainValue))]
    public partial string MainValue { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>
    /// 获取详情文本。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.DetailText))]
    public partial string DetailText { get; private set; }

    /// <summary>
    /// 获取动作日志。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.ActionLog))]
    public partial string ActionLog { get; private set; }

    /// <summary>
    /// 获取主动作文本。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.PrimaryActionText))]
    public partial string PrimaryActionText { get; private set; }

    /// <summary>
    /// 获取辅助动作文本。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.SecondaryActionText))]
    public partial string SecondaryActionText { get; private set; }

    /// <summary>
    /// 获取是否允许执行主动作。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.CanPrimaryAction))]
    public partial bool CanPrimaryAction { get; private set; }

    /// <summary>
    /// 获取是否允许执行辅助动作。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.CanSecondaryAction))]
    public partial bool CanSecondaryAction { get; private set; }

    /// <summary>
    /// 获取或设置患者姓名。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.PatientName), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(AdmissionCoordinatorIntent.ChangePatientName))]
    public partial string PatientName { get; set; }

    /// <summary>
    /// 获取或设置患者年龄。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.PatientAge), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(AdmissionCoordinatorIntent.ChangePatientAge))]
    public partial string PatientAge { get; set; }

    /// <summary>
    /// 获取或设置入院诊断。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.AdmissionDiagnosis), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(AdmissionCoordinatorIntent.ChangeAdmissionDiagnosis))]
    public partial string AdmissionDiagnosis { get; set; }

    /// <summary>
    /// 获取或设置目标床号。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.TargetBedNo), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(AdmissionCoordinatorIntent.ChangeTargetBedNo))]
    public partial string TargetBedNo { get; set; }

    /// <summary>
    /// 获取或设置护士交接备注。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.NurseNote), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(AdmissionCoordinatorIntent.ChangeNurseNote))]
    public partial string NurseNote { get; set; }

    /// <summary>
    /// 获取确认入院按钮文本。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.ConfirmAdmissionText))]
    public partial string ConfirmAdmissionText { get; private set; }

    /// <summary>
    /// 获取是否允许提交入院登记。
    /// </summary>
    [MviBind(nameof(AdmissionCoordinatorState.CanConfirmAdmission))]
    public partial bool CanConfirmAdmission { get; private set; }

    /// <summary>
    /// 获取提交入院登记命令。
    /// </summary>
    [MviCommand(typeof(AdmissionCoordinatorIntent.SubmitAdmissionForm), CanExecuteProperty = nameof(CanConfirmAdmission), IsAsync = true)]
    public partial IMviAsyncCommand SubmitAdmissionCommand { get; private set; }

    /// <summary>
    /// 获取主动作命令。
    /// </summary>
    [MviCommand(typeof(AdmissionCoordinatorIntent.ExecutePrimaryAction), CanExecuteProperty = nameof(CanPrimaryAction), IsAsync = true)]
    public partial IMviAsyncCommand PrimaryActionCommand { get; private set; }

    /// <summary>
    /// 获取辅助动作命令。
    /// </summary>
    [MviCommand(typeof(AdmissionCoordinatorIntent.ExecuteSecondaryAction), CanExecuteProperty = nameof(CanSecondaryAction), IsAsync = true)]
    public partial IMviAsyncCommand SecondaryActionCommand { get; private set; }
}
