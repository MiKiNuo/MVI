using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.Slot;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面视图。
/// 3 个子组件 ViewModel 由 <see cref="OutpatientWorkstationViewModel"/> 工厂方法按需解析，
/// 再由 <see cref="MiKiNuo.Mvi.Presentation.ViewRegistry.IMviViewRegistry"/> 创建具体 View。
/// <para>
/// 槽位字段全部以 <c>[MviSlot]</c> 特性声明，
/// 由 <c>MviCompositeSlotBindingGenerator</c> 在编译期 emit <c>OnBindSlots</c> override
/// 把父 VM 工厂方法返回的子 ViewModel 解析成子 View 并写入 <c>MviSlotHost</c>。
/// 视图不再持有 <see cref="MiKiNuo.Mvi.Presentation.ViewRegistry.IMviViewRegistry"/> 引用，
/// 源生成器从 <c>Bind(viewModel, resolver)</c> 注入的 <see cref="MiKiNuo.Mvi.Application.DI.IMviResolver"/>
/// 中解析，避免把"服务定位器"透传到 View。
/// </para>
/// </summary>
public sealed partial class OutpatientWorkstationView : MviAvaloniaView<OutpatientWorkstationViewModel>
{
    /// <summary>
    /// 候诊队列槽位：常驻子 View，绑定时通过 <c>CreateQueueViewModel()</c> 解析。
    /// </summary>
    [MviSlot(typeof(PatientQueueView), factory: nameof(OutpatientWorkstationViewModel.CreateQueueViewModel))]
    private MviSlotHost? _queueSlot;

    /// <summary>
    /// 电子病历编辑槽位：常驻子 View，绑定时通过 <c>CreateEditorViewModel()</c> 解析。
    /// </summary>
    [MviSlot(typeof(ClinicalEditorView), factory: nameof(OutpatientWorkstationViewModel.CreateEditorViewModel))]
    private MviSlotHost? _editorSlot;

    /// <summary>
    /// 临床提醒槽位：常驻子 View，绑定时通过 <c>CreateReminderViewModel()</c> 解析。
    /// </summary>
    [MviSlot(typeof(ClinicalReminderView), factory: nameof(OutpatientWorkstationViewModel.CreateReminderViewModel))]
    private MviSlotHost? _reminderSlot;

    /// <summary>
    /// 初始化门诊工作站页面视图。
    /// </summary>
    public OutpatientWorkstationView()
    {
        AvaloniaXamlLoader.Load(this);
        _queueSlot = this.FindControl<MviSlotHost>("QueueSlot") ?? throw new InvalidOperationException("无法找到 QueueSlot。");
        _editorSlot = this.FindControl<MviSlotHost>("EditorSlot") ?? throw new InvalidOperationException("无法找到 EditorSlot。");
        _reminderSlot = this.FindControl<MviSlotHost>("ReminderSlot") ?? throw new InvalidOperationException("无法找到 ReminderSlot。");
    }
}
