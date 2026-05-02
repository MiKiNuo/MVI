using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Pharmacy.PrescriptionReviewBoard;

/// <summary>
/// 表示处方审核 MVI ViewModel。
/// </summary>
public sealed partial class PrescriptionReviewBoardViewModel
    : MviViewModelBase<PrescriptionReviewBoardState, PrescriptionReviewBoardIntent, PrescriptionReviewBoardEffect>
{
    /// <summary>
    /// 初始化处方审核 MVI ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    public PrescriptionReviewBoardViewModel(IMviStore<PrescriptionReviewBoardState, PrescriptionReviewBoardIntent, PrescriptionReviewBoardEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取核心指标。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.MainValue))]
    public partial string MainValue { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>
    /// 获取详情文本。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.DetailText))]
    public partial string DetailText { get; private set; }

    /// <summary>
    /// 获取动作日志。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.ActionLog))]
    public partial string ActionLog { get; private set; }

    /// <summary>
    /// 获取主动作文本。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.PrimaryActionText))]
    public partial string PrimaryActionText { get; private set; }

    /// <summary>
    /// 获取辅助动作文本。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.SecondaryActionText))]
    public partial string SecondaryActionText { get; private set; }

    /// <summary>
    /// 获取是否允许执行主动作。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.CanPrimaryAction))]
    public partial bool CanPrimaryAction { get; private set; }

    /// <summary>
    /// 获取是否允许执行辅助动作。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.CanSecondaryAction))]
    public partial bool CanSecondaryAction { get; private set; }

    /// <summary>
    /// 获取或设置处方号。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.PrescriptionNo), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(PrescriptionReviewBoardIntent.ChangePrescriptionNo))]
    public partial string PrescriptionNo { get; set; }

    /// <summary>
    /// 获取或设置患者姓名。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.PatientName), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(PrescriptionReviewBoardIntent.ChangePatientName))]
    public partial string PatientName { get; set; }

    /// <summary>
    /// 获取或设置药品名称。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.DrugName), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(PrescriptionReviewBoardIntent.ChangeDrugName))]
    public partial string DrugName { get; set; }

    /// <summary>
    /// 获取或设置剂量用法。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.DoseText), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(PrescriptionReviewBoardIntent.ChangeDoseText))]
    public partial string DoseText { get; set; }

    /// <summary>
    /// 获取或设置过敏史。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.AllergyHistory), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(PrescriptionReviewBoardIntent.ChangeAllergyHistory))]
    public partial string AllergyHistory { get; set; }

    /// <summary>
    /// 获取审核按钮文本。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.ApprovePrescriptionText))]
    public partial string ApprovePrescriptionText { get; private set; }

    /// <summary>
    /// 获取是否允许审核处方。
    /// </summary>
    [MviBind(nameof(PrescriptionReviewBoardState.CanApprovePrescription))]
    public partial bool CanApprovePrescription { get; private set; }

    /// <summary>
    /// 获取提交处方审核命令。
    /// </summary>
    [MviCommand(typeof(PrescriptionReviewBoardIntent.SubmitPrescriptionReviewForm), CanExecuteProperty = nameof(CanApprovePrescription), IsAsync = true)]
    public partial IMviAsyncCommand SubmitPrescriptionReviewCommand { get; private set; }

    /// <summary>
    /// 获取主动作命令。
    /// </summary>
    [MviCommand(typeof(PrescriptionReviewBoardIntent.ExecutePrimaryAction), CanExecuteProperty = nameof(CanPrimaryAction), IsAsync = true)]
    public partial IMviAsyncCommand PrimaryActionCommand { get; private set; }

    /// <summary>
    /// 获取辅助动作命令。
    /// </summary>
    [MviCommand(typeof(PrescriptionReviewBoardIntent.ExecuteSecondaryAction), CanExecuteProperty = nameof(CanSecondaryAction), IsAsync = true)]
    public partial IMviAsyncCommand SecondaryActionCommand { get; private set; }
}
