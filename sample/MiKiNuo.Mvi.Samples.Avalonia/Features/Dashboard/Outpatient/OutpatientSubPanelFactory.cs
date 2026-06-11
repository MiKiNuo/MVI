using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示 <see cref="IOutpatientSubPanelFactory"/> 的标准实现。
/// <para>
/// 构造时由组合根（如 <c>SampleGeneratedContainer</c>）注入 3 个子 VM 并一次性缓存。
/// 本工厂本身不创建子 VM——子 VM 由组合根在装配期间通过 DI 容器解析，
/// 由本工厂对外屏蔽"3 个子 VM 来自哪里"的事实，让父 VM 与子 VM 之间仅通过接口解耦。
/// </para>
/// </summary>
public sealed class OutpatientSubPanelFactory : IOutpatientSubPanelFactory
{
    private readonly PatientQueueViewModel _queueViewModel;
    private readonly ClinicalEditorViewModel _editorViewModel;
    private readonly ClinicalReminderViewModel _reminderViewModel;

    /// <summary>
    /// 初始化门诊工作站子组件 ViewModel 工厂。
    /// </summary>
    /// <param name="queueViewModel">候诊队列子组件 ViewModel。</param>
    /// <param name="editorViewModel">电子病历编辑子组件 ViewModel。</param>
    /// <param name="reminderViewModel">临床提醒子组件 ViewModel。</param>
    public OutpatientSubPanelFactory(
        PatientQueueViewModel queueViewModel,
        ClinicalEditorViewModel editorViewModel,
        ClinicalReminderViewModel reminderViewModel)
    {
        ArgumentNullException.ThrowIfNull(queueViewModel);
        ArgumentNullException.ThrowIfNull(editorViewModel);
        ArgumentNullException.ThrowIfNull(reminderViewModel);

        _queueViewModel = queueViewModel;
        _editorViewModel = editorViewModel;
        _reminderViewModel = reminderViewModel;
    }

    /// <inheritdoc />
    public object CreateQueueViewModel() => _queueViewModel;

    /// <inheritdoc />
    public object CreateEditorViewModel() => _editorViewModel;

    /// <inheritdoc />
    public object CreateReminderViewModel() => _reminderViewModel;
}
