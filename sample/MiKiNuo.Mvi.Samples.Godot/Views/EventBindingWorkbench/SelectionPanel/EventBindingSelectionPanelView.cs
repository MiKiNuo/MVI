using System;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Presentation.Events;
using MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.SelectionPanel;

/// <summary>
/// 表示 Godot 事件绑定选择面板视图。
/// </summary>
public partial class EventBindingSelectionPanelView : GodotMviControlView<EventBindingSelectionViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(EventBindingSelectionViewModel viewModel, GodotMviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        ItemList missionList = GetNode<ItemList>("Panel/Margin/Layout/MissionList");
        Label selectedLabel = GetNode<Label>("Panel/Margin/Layout/SelectedLabel");

        BindEvent<long, ItemList.ItemSelectedEventHandler>(
            handler => missionList.ItemSelected += handler,
            handler => missionList.ItemSelected -= handler,
            handler => new ItemList.ItemSelectedEventHandler(handler),
            viewModel.SelectionChangedCommand,
            bindings,
            index => new MviSelectionChangedEventPayload(
                missionList.GetItemText((int)index),
                (int)index,
                viewModel.SelectedMissionId,
                index),
            missionList.Name);

        BindPropertyChanged(
            viewModel,
            nameof(EventBindingSelectionViewModel.SelectedMissionId),
            () => selectedLabel.Text = $"Selected: {viewModel.SelectedMissionId}",
            bindings);
    }
}
