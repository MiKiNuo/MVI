using System;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.EventBinding;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.DetailPanel;

/// <summary>
/// 表示 Godot 事件绑定详情面板视图。
/// 通过 <see cref="GodotEventSources.FromPressed"/> 把 <c>Button.Pressed</c> 封装为
/// <see cref="IEventSource{TEvent}"/>，再用 <see cref="EventBinding{TEvent}"/> 映射为
/// <see cref="EventBindingDetailIntent.Prepare"/> 意图，注册到 ViewModel 生命周期。
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

        IEventSource<EventArgs> source = GodotEventSources.FromPressed(prepareButton);
        EventBinding<EventArgs> binding = new(
            source,
            _ => new EventBindingDetailIntent.Prepare(
                new MviActionEventPayload(prepareButton.Name, "Pressed", null)));
        viewModel.AddEventBinding(binding);

        BindPropertyChanged(
            viewModel,
            nameof(EventBindingDetailViewModel.PrepareCount),
            () => countLabel.Text = $"Prepare count: {viewModel.PrepareCount}",
            bindings);
    }
}
