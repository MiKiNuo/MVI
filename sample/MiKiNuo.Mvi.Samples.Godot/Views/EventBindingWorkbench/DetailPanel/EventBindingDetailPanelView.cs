using System;
using global::Godot;
using MiKiNuo.Mvi.Presentation.Binding;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.DetailPanel;

/// <summary>
/// 表示 Godot 事件绑定详情面板视图。
/// 通过 <c>ToEventSource().Pressed</c> 把 <c>Button.Pressed</c> 封装为
/// <see cref="Application.MVI.EventBinding.IEventSource{TEvent}"/>，再用 <see cref="EventBindingExtensions.BindTo{TEvent}"/>
/// 映射为 <see cref="EventBindingDetailIntent.Prepare"/> 意图，注册到 View 生命周期。
/// </summary>
public partial class EventBindingDetailPanelView : GodotMviControlView<EventBindingDetailViewModel>
{
    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected override void OnBind(EventBindingDetailViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        Button prepareButton = GetNode<Button>("Panel/Margin/Layout/PrepareButton");
        Label countLabel = GetNode<Label>("Panel/Margin/Layout/CountLabel");

        prepareButton.ToEventSource().Pressed.BindTo(
            viewModel.GetIntentDispatcher(),
            _ => new EventBindingDetailIntent.Prepare(
                new MviActionEventPayload(prepareButton.Name, "Pressed", null)),
            bindings);

        BindPropertyChanged(
            viewModel,
            nameof(EventBindingDetailViewModel.PrepareCount),
            () => countLabel.Text = $"Prepare count: {viewModel.PrepareCount}",
            bindings);
    }
}
