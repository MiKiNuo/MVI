using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.SpecimenTracker;

/// <summary>
/// 表示标本流转 MVI ViewModel。
/// </summary>
public sealed partial class SpecimenTrackerViewModel
    : MviViewModelBase<SpecimenTrackerState, SpecimenTrackerIntent, SpecimenTrackerEffect>
{
    /// <summary>
    /// 初始化标本流转 MVI ViewModel。
    /// </summary>
    /// <param name="store">状态存储。</param>
    public SpecimenTrackerViewModel(IMviStore<SpecimenTrackerState, SpecimenTrackerIntent, SpecimenTrackerEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取标题。
    /// </summary>
    [MviBind(nameof(SpecimenTrackerState.Title))]
    public partial string Title { get; private set; }

    /// <summary>
    /// 获取核心指标。
    /// </summary>
    [MviBind(nameof(SpecimenTrackerState.MainValue))]
    public partial string MainValue { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(SpecimenTrackerState.StatusText))]
    public partial string StatusText { get; private set; }

    /// <summary>
    /// 获取详情文本。
    /// </summary>
    [MviBind(nameof(SpecimenTrackerState.DetailText))]
    public partial string DetailText { get; private set; }

    /// <summary>
    /// 获取动作日志。
    /// </summary>
    [MviBind(nameof(SpecimenTrackerState.ActionLog))]
    public partial string ActionLog { get; private set; }

    /// <summary>
    /// 获取主动作文本。
    /// </summary>
    [MviBind(nameof(SpecimenTrackerState.PrimaryActionText))]
    public partial string PrimaryActionText { get; private set; }

    /// <summary>
    /// 获取辅助动作文本。
    /// </summary>
    [MviBind(nameof(SpecimenTrackerState.SecondaryActionText))]
    public partial string SecondaryActionText { get; private set; }

    /// <summary>
    /// 获取是否允许执行主动作。
    /// </summary>
    [MviBind(nameof(SpecimenTrackerState.CanPrimaryAction))]
    public partial bool CanPrimaryAction { get; private set; }

    /// <summary>
    /// 获取是否允许执行辅助动作。
    /// </summary>
    [MviBind(nameof(SpecimenTrackerState.CanSecondaryAction))]
    public partial bool CanSecondaryAction { get; private set; }

    /// <summary>
    /// 获取主动作命令。
    /// </summary>
    [MviCommand(typeof(SpecimenTrackerIntent.ExecutePrimaryAction), CanExecuteProperty = nameof(CanPrimaryAction), IsAsync = true)]
    public partial IMviAsyncCommand PrimaryActionCommand { get; private set; }

    /// <summary>
    /// 获取辅助动作命令。
    /// </summary>
    [MviCommand(typeof(SpecimenTrackerIntent.ExecuteSecondaryAction), CanExecuteProperty = nameof(CanSecondaryAction), IsAsync = true)]
    public partial IMviAsyncCommand SecondaryActionCommand { get; private set; }
}
