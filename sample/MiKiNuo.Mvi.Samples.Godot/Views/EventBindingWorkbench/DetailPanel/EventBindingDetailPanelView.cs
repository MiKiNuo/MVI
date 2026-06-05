using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Presentation.Events;
using MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.DetailPanel;

/// <summary>
/// 表示 Godot 事件绑定详情面板视图。
/// </summary>
public partial class EventBindingDetailPanelView : GodotMviControlView<EventBindingDetailViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(EventBindingDetailViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        Button prepareButton = GetNode<Button>("Panel/Margin/Layout/PrepareButton");
        Label countLabel = GetNode<Label>("Panel/Margin/Layout/CountLabel");

        BindEvent(
            handler => prepareButton.Pressed += handler,
            handler => prepareButton.Pressed -= handler,
            viewModel.PrepareCommand,
            bindings,
            () => new MviActionEventPayload(prepareButton.Name, "Pressed", null),
            prepareButton.Name);

        BindPropertyChanged(
            viewModel,
            nameof(EventBindingDetailViewModel.PrepareCount),
            () => countLabel.Text = $"Prepare count: {viewModel.PrepareCount}",
            bindings);
    }
}
