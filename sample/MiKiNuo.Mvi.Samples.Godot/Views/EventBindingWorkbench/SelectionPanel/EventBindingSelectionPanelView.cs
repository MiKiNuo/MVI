using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Binding;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.SelectionPanel;

/// <summary>
/// 表示 Godot 事件绑定选择面板视图。
/// 通过 <c>ToEventSource().ItemSelected</c> 把 <see cref="ItemList.ItemSelected"/> 封装为
/// <see cref="Application.MVI.EventBinding.IEventSource{TEvent}"/>，再用 <see cref="EventBindingExtensions.BindTo{TEvent}"/>
/// 映射为 <see cref="EventBindingSelectionIntent.ChangeSelection"/> 意图，注册到 View 生命周期。
/// </summary>
public partial class EventBindingSelectionPanelView : GodotMviControlView<EventBindingSelectionViewModel>
{
    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected override void OnBind(EventBindingSelectionViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        ItemList missionList = GetNode<ItemList>("Panel/Margin/Layout/MissionList");
        Label selectedLabel = GetNode<Label>("Panel/Margin/Layout/SelectedLabel");

        missionList.ToEventSource().ItemSelected.BindTo(
            viewModel.GetIntentDispatcher(),
            index => new EventBindingSelectionIntent.ChangeSelection(
                new MviSelectionChangedEventPayload(
                    missionList.GetItemText((int)index),
                    (int)index,
                    viewModel.SelectedMissionId,
                    index)),
            bindings);

        BindPropertyChanged(
            viewModel,
            nameof(EventBindingSelectionViewModel.SelectedMissionId),
            () => selectedLabel.Text = $"Selected: {viewModel.SelectedMissionId}",
            bindings);
    }
}
