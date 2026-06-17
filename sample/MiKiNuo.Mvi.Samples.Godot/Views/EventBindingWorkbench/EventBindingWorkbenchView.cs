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

        // 3 个子 View 必须用 2-arg Bind 向下传递组合根解析器（虽然它们未使用 [MviSlot]，
        // 但 IMviGodotBindable 接口已统一为 2-arg 入口；空 OnBindSlots 钩子零成本）。
        searchView.Bind((EventBindingSearchViewModel)viewModel.CreateSearchViewModel(), ResolverOrThrow());
        selectionView.Bind((EventBindingSelectionViewModel)viewModel.CreateSelectionViewModel(), ResolverOrThrow());
        detailView.Bind((EventBindingDetailViewModel)viewModel.CreateDetailViewModel(), ResolverOrThrow());

        Label interactionLabel = GetNode<Label>("Root/Header/InteractionLabel");
        BindPropertyChanged(
            viewModel,
            nameof(EventBindingWorkbenchViewModel.LastInteractionText),
            () => interactionLabel.Text = viewModel.LastInteractionText,
            bindings);
    }

    private MiKiNuo.Mvi.Application.DI.IMviResolver ResolverOrThrow()
    {
        return Resolver ?? throw new InvalidOperationException(
            "EventBindingWorkbenchView 必须由父组合根通过 Bind(viewModel, resolver) 2-arg 重载激活。");
    }
}
