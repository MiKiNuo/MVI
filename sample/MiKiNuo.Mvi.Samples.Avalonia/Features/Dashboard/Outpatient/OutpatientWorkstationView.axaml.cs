using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Slot;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面视图。
/// 3 个子组件 ViewModel 由 <see cref="OutpatientWorkstationViewModel"/> 强类型属性暴露，
/// View 直接读取后交由 ViewRegistry 创建具体 View，不再依赖 State 中的 object 字段。
/// </summary>
public sealed partial class OutpatientWorkstationView : MviAvaloniaView<OutpatientWorkstationViewModel>
{
    private readonly IMviViewRegistry _viewRegistry;
    private readonly MviSlotHost _queueSlot;
    private readonly MviSlotHost _editorSlot;
    private readonly MviSlotHost _reminderSlot;

    /// <summary>
    /// 初始化门诊工作站页面视图。
    /// </summary>
    /// <param name="viewRegistry">视图注册表。</param>
    public OutpatientWorkstationView(IMviViewRegistry viewRegistry)
    {
        ArgumentNullException.ThrowIfNull(viewRegistry);

        _viewRegistry = viewRegistry;
        AvaloniaXamlLoader.Load(this);
        _queueSlot = this.FindControl<MviSlotHost>("QueueSlot") ?? throw new InvalidOperationException("无法找到 QueueSlot。");
        _editorSlot = this.FindControl<MviSlotHost>("EditorSlot") ?? throw new InvalidOperationException("无法找到 EditorSlot。");
        _reminderSlot = this.FindControl<MviSlotHost>("ReminderSlot") ?? throw new InvalidOperationException("无法找到 ReminderSlot。");
    }

    /// <inheritdoc />
    public new void Bind(OutpatientWorkstationViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        base.Bind(viewModel);
        _queueSlot.Content = _viewRegistry.CreateView(viewModel.QueueViewModel);
        _editorSlot.Content = _viewRegistry.CreateView(viewModel.ClinicalEditorViewModel);
        _reminderSlot.Content = _viewRegistry.CreateView(viewModel.ClinicalReminderViewModel);
    }
}
