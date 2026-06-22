using System;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.EventBinding;
using MiKiNuo.Mvi.Presentation.Disposables;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Views.EventBindingWorkbench.SearchPanel;

/// <summary>
/// 表示 Godot 事件绑定搜索面板视图。
/// 通过 <c>ToEventSource().TextChanged</c> 把 <see cref="LineEdit.TextChanged"/> 封装为
/// <see cref="IEventSource{TEvent}"/>，再用 <see cref="EventBinding{TEvent}"/> 映射为
/// <see cref="EventBindingSearchIntent.ChangeQuery"/> 意图，注册到 ViewModel 生命周期。
/// </summary>
public partial class EventBindingSearchPanelView : GodotMviControlView<EventBindingSearchViewModel>
{
    /// <summary>
    /// 执行具体 View 的绑定逻辑。
    /// </summary>
    /// <param name="viewModel">当前 ViewModel。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected override void OnBind(EventBindingSearchViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        LineEdit queryEdit = GetNode<LineEdit>("Panel/Margin/Layout/QueryEdit");
        Label statusLabel = GetNode<Label>("Panel/Margin/Layout/StatusLabel");

        IEventSource<string> source = queryEdit.ToEventSource().TextChanged;
        EventBinding<string> binding = new(
            source,
            text => new EventBindingSearchIntent.ChangeQuery(
                new MviTextChangedEventPayload(text, viewModel.QueryText, true, text)));
        viewModel.AddEventBinding(binding);

        BindPropertyChanged(
            viewModel,
            nameof(EventBindingSearchViewModel.QueryText),
            () => statusLabel.Text = $"Query: {viewModel.QueryText}",
            bindings);
    }
}
