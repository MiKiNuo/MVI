using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Presentation.Binding;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板视图。
/// 通过 <c>ToEventSource().SelectionChanged</c> 把 <see cref="SelectingItemsControl.SelectionChanged"/>
/// 封装为 <see cref="Application.MVI.EventBinding.IEventSource{TEvent}"/>，再用 <see cref="EventBindingExtensions.BindTo{TEvent}"/>
/// 映射为 <see cref="EventBindingSelectionIntent.ChangeSelection"/> 意图，注册到 View 生命周期。
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
    /// <param name="bindings">绑定生命周期集合。</param>
    protected override void OnBind(EventBindingSelectionViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        SelectingItemsControl patientList = this.FindControl<SelectingItemsControl>("PatientList")
            ?? throw new InvalidOperationException("未找到 PatientList 控件。");

        object? previousSelectedValue = patientList.SelectedItem;
        patientList.ToEventSource().SelectionChanged.BindTo(
            viewModel.GetIntentDispatcher(),
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
            },
            bindings);
    }
}
