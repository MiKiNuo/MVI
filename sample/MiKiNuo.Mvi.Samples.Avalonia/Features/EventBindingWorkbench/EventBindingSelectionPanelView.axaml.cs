using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Application.MVI.EventBinding;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板视图。
/// 通过 <c>ToEventSource().SelectionChanged</c> 把 <see cref="SelectingItemsControl.SelectionChanged"/>
/// 封装为 <see cref="IEventSource{TEvent}"/>，再用 <see cref="EventBinding{TEvent}"/> 映射为
/// <see cref="EventBindingSelectionIntent.ChangeSelection"/> 意图，注册到 ViewModel 生命周期。
/// </summary>
public sealed partial class EventBindingSelectionPanelView : MviAvaloniaView<EventBindingSelectionViewModel>
{
    /// <summary>
    /// 初始化事件绑定选择面板视图。
    /// </summary>
    public EventBindingSelectionPanelView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 绑定 ViewModel 时注册事件绑定。
    /// </summary>
    /// <param name="viewModel">当前绑定的视图模型。</param>
    protected override void OnBind(EventBindingSelectionViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        SelectingItemsControl patientList = this.FindControl<SelectingItemsControl>("PatientList")
            ?? throw new InvalidOperationException("未找到 PatientList 控件。");

        object? previousSelectedValue = patientList.SelectedItem;
        IEventSource<SelectionChangedEventArgs> source = patientList.ToEventSource().SelectionChanged;
        EventBinding<SelectionChangedEventArgs> binding = new(
            source,
            args =>
            {
                object? previousValue = args.RemovedItems.Count > 0
                    ? args.RemovedItems[0]
                    : previousSelectedValue;
                object? selectedValue = patientList.SelectedItem;
                previousSelectedValue = selectedValue;
                return new EventBindingSelectionIntent.ChangeSelection(
                    new MviSelectionChangedEventPayload(
                        selectedValue,
                        patientList.SelectedIndex,
                        previousValue,
                        args));
            });
        viewModel.AddEventBinding(binding);
    }
}
