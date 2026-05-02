using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面 ViewModel。
/// </summary>
public sealed partial class OutpatientWorkstationViewModel
    : MviViewModelBase<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationEffect>
{
    /// <summary>
    /// 初始化门诊工作站页面 ViewModel。
    /// </summary>
    /// <param name="store">门诊工作站页面状态存储。</param>
    public OutpatientWorkstationViewModel(IMviStore<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationEffect> store)
        : base(store)
    {
    }

    /// <summary>
    /// 获取候诊队列 ViewModel。
    /// </summary>
    [MviBind(nameof(OutpatientWorkstationState.QueueViewModel))]
    public partial object QueueViewModel { get; private set; }

    /// <summary>
    /// 获取电子病历编辑 ViewModel。
    /// </summary>
    [MviBind(nameof(OutpatientWorkstationState.ClinicalEditorViewModel))]
    public partial object ClinicalEditorViewModel { get; private set; }

    /// <summary>
    /// 获取临床提醒 ViewModel。
    /// </summary>
    [MviBind(nameof(OutpatientWorkstationState.ClinicalReminderViewModel))]
    public partial object ClinicalReminderViewModel { get; private set; }
}
