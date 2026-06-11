using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.DetailPanel;
using MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.SearchPanel;
using MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.SelectionPanel;

namespace MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench;

/// <summary>
/// 表示 Godot 事件绑定复杂组合根视图。
/// </summary>
public partial class EventBindingWorkbenchView : GodotMviControlView<EventBindingWorkbenchViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(EventBindingWorkbenchViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        EventBindingSearchPanelView searchView = GetNode<EventBindingSearchPanelView>("Root/Body/SearchPanel");
        EventBindingSelectionPanelView selectionView = GetNode<EventBindingSelectionPanelView>("Root/Body/SelectionPanel");
        EventBindingDetailPanelView detailView = GetNode<EventBindingDetailPanelView>("Root/Body/DetailPanel");

        searchView.Bind((EventBindingSearchViewModel)viewModel.CreateSearchViewModel());
        selectionView.Bind((EventBindingSelectionViewModel)viewModel.CreateSelectionViewModel());
        detailView.Bind((EventBindingDetailViewModel)viewModel.CreateDetailViewModel());

        Label interactionLabel = GetNode<Label>("Root/Header/InteractionLabel");
        BindPropertyChanged(
            viewModel,
            nameof(EventBindingWorkbenchViewModel.LastInteractionText),
            () => interactionLabel.Text = viewModel.LastInteractionText,
            bindings);
    }
}
