using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.RiskEventBoard;

/// <summary>
/// 表示风险事件 MVI ViewModel。
/// </summary>
public sealed partial class RiskEventBoardViewModel
    : MviViewModelBase<RiskEventBoardState, RiskEventBoardIntent, RiskEventBoardEffect>
{
    /// <summary>
    /// 初始化风险事件 MVI ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    public RiskEventBoardViewModel(IMviStore<RiskEventBoardState, RiskEventBoardIntent, RiskEventBoardEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取核心指标。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.MainValue))]
    public partial string MainValue { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>
    /// 获取详情文本。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.DetailText))]
    public partial string DetailText { get; private set; }

    /// <summary>
    /// 获取动作日志。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.ActionLog))]
    public partial string ActionLog { get; private set; }

    /// <summary>
    /// 获取主动作文本。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.PrimaryActionText))]
    public partial string PrimaryActionText { get; private set; }

    /// <summary>
    /// 获取辅助动作文本。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.SecondaryActionText))]
    public partial string SecondaryActionText { get; private set; }

    /// <summary>
    /// 获取是否允许执行主动作。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.CanPrimaryAction))]
    public partial bool CanPrimaryAction { get; private set; }

    /// <summary>
    /// 获取是否允许执行辅助动作。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.CanSecondaryAction))]
    public partial bool CanSecondaryAction { get; private set; }

    /// <summary>
    /// 获取或设置事件标题。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.EventTitle), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(RiskEventBoardIntent.ChangeEventTitle))]
    public partial string EventTitle { get; set; }

    /// <summary>
    /// 获取或设置责任科室。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.DepartmentName), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(RiskEventBoardIntent.ChangeDepartmentName))]
    public partial string DepartmentName { get; set; }

    /// <summary>
    /// 获取或设置风险等级。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.SeverityLevel), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(RiskEventBoardIntent.ChangeSeverityLevel))]
    public partial string SeverityLevel { get; set; }

    /// <summary>
    /// 获取或设置责任人。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.ResponsibleOwner), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(RiskEventBoardIntent.ChangeResponsibleOwner))]
    public partial string ResponsibleOwner { get; set; }

    /// <summary>
    /// 获取或设置整改措施。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.CorrectiveAction), BindingMode = MviBindingMode.TwoWay, IntentType = typeof(RiskEventBoardIntent.ChangeCorrectiveAction))]
    public partial string CorrectiveAction { get; set; }

    /// <summary>
    /// 获取提交事件按钮文本。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.SubmitRiskEventText))]
    public partial string SubmitRiskEventText { get; private set; }

    /// <summary>
    /// 获取是否允许提交风险事件。
    /// </summary>
    [MviBind(nameof(RiskEventBoardState.CanSubmitRiskEvent))]
    public partial bool CanSubmitRiskEvent { get; private set; }

    /// <summary>
    /// 获取提交风险事件命令。
    /// </summary>
    [MviCommand(typeof(RiskEventBoardIntent.SubmitRiskEventForm), CanExecuteProperty = nameof(CanSubmitRiskEvent), IsAsync = true)]
    public partial IMviAsyncCommand SubmitRiskEventCommand { get; private set; }

    /// <summary>
    /// 获取主动作命令。
    /// </summary>
    [MviCommand(typeof(RiskEventBoardIntent.ExecutePrimaryAction), CanExecuteProperty = nameof(CanPrimaryAction), IsAsync = true)]
    public partial IMviAsyncCommand PrimaryActionCommand { get; private set; }

    /// <summary>
    /// 获取辅助动作命令。
    /// </summary>
    [MviCommand(typeof(RiskEventBoardIntent.ExecuteSecondaryAction), CanExecuteProperty = nameof(CanSecondaryAction), IsAsync = true)]
    public partial IMviAsyncCommand SecondaryActionCommand { get; private set; }
}
