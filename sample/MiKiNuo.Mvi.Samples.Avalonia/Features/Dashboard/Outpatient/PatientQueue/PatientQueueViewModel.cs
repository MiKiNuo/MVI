using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列 ViewModel。
/// </summary>
public sealed partial class PatientQueueViewModel
    : MviViewModelBase<PatientQueueState, PatientQueueIntent, PatientQueueEffect>
{
    /// <summary>
    /// 初始化门诊队列 ViewModel。
    /// </summary>
    /// <param name="store">门诊队列状态存储。</param>
    public PatientQueueViewModel(IMviStore<PatientQueueState, PatientQueueIntent, PatientQueueEffect> store)
        : base(store)
    {
        InitializeGeneratedCommands();
    }

    /// <summary>
    /// 获取候诊患者集合。
    /// </summary>
    [MviBind(nameof(PatientQueueState.Patients))]
    public partial IReadOnlyList<string> Patients { get; private set; }

    /// <summary>
    /// 获取当前接诊患者姓名。
    /// </summary>
    [MviBind(nameof(PatientQueueState.SelectedPatientName))]
    public partial string SelectedPatientName { get; private set; }

    /// <summary>
    /// 获取队列摘要。
    /// </summary>
    [MviBind(nameof(PatientQueueState.QueueSummary))]
    public partial string QueueSummary { get; private set; }

    /// <summary>
    /// 获取是否可以接诊下一位。
    /// </summary>
    [MviBind(nameof(PatientQueueState.CanCallNext))]
    public partial bool CanCallNext { get; private set; }

    /// <summary>
    /// 获取接诊下一位命令。
    /// </summary>
    [MviCommand(typeof(PatientQueueIntent.CallNext), CanExecuteProperty = nameof(CanCallNext), IsAsync = true)]
    public partial IMviAsyncCommand CallNextCommand { get; private set; }
}
