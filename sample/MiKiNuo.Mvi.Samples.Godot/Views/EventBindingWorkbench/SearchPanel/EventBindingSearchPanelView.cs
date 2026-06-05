using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Presentation.Events;
using MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.SearchPanel;

/// <summary>
/// 表示 Godot 事件绑定搜索面板视图。
/// </summary>
public partial class EventBindingSearchPanelView : GodotMviControlView<EventBindingSearchViewModel>
{
    /// <inheritdoc />
    protected override void OnBind(EventBindingSearchViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        LineEdit queryEdit = GetNode<LineEdit>("Panel/Margin/Layout/QueryEdit");
        Label statusLabel = GetNode<Label>("Panel/Margin/Layout/StatusLabel");

        BindEvent<string, LineEdit.TextChangedEventHandler>(
            handler => queryEdit.TextChanged += handler,
            handler => queryEdit.TextChanged -= handler,
            handler => new LineEdit.TextChangedEventHandler(handler),
            viewModel.QueryTextChangedCommand,
            bindings,
            text => new MviTextChangedEventPayload(text, viewModel.QueryText, true, text),
            queryEdit.Name);

        BindPropertyChanged(
            viewModel,
            nameof(EventBindingSearchViewModel.QueryText),
            () => statusLabel.Text = $"Query: {viewModel.QueryText}",
            bindings);
    }
}
