using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Events;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Presentation.Binding;
using MiKiNuo.Mvi.Presentation.Disposables;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定详情面板视图。
/// 通过 <c>ToEventSource().PointerPressed</c> 和 <c>ToEventSource().Click</c>
/// 把 <see cref="InputElement.PointerPressed"/> 与 <see cref="Button.Click"/> 封装为
/// <see cref="Application.MVI.EventBinding.IEventSource{TEvent}"/>，再用 <see cref="EventBindingExtensions.BindTo{TEvent}"/>
/// 映射为 <see cref="EventBindingDetailIntent.PressDetail"/> / <see cref="EventBindingDetailIntent.Refresh"/> 意图。
/// </summary>
public sealed partial class EventBindingDetailPanelView : MviAvaloniaView<EventBindingDetailViewModel>
{
    /// <summary>
    /// 初始化事件绑定详情面板视图。
    /// </summary>
    public EventBindingDetailPanelView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 绑定 ViewModel 时注册事件绑定。
    /// </summary>
    /// <param name="viewModel">当前绑定的视图模型。</param>
    /// <param name="bindings">绑定生命周期集合。</param>
    protected override void OnBind(EventBindingDetailViewModel viewModel, MviDisposableBag bindings)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        Control pointerSurface = this.FindControl<Control>("PointerSurface")
            ?? throw new InvalidOperationException("未找到 PointerSurface 控件。");
        Button refreshButton = this.FindControl<Button>("DetailRefreshButton")
            ?? throw new InvalidOperationException("未找到 DetailRefreshButton 控件。");

        // 指针按下 → PressDetail 意图
        pointerSurface.ToEventSource().PointerPressed.BindTo(
            viewModel.GetIntentDispatcher(),
            args => new EventBindingDetailIntent.PressDetail(
                AvaloniaEventPayloads.ToPointerPayload(pointerSurface, args)),
            bindings);

        // 刷新按钮点击 → Refresh 意图
        refreshButton.ToEventSource().Click.BindTo(
            viewModel.GetIntentDispatcher(),
            args => new EventBindingDetailIntent.Refresh(
                AvaloniaEventPayloads.ToActionPayload(refreshButton, args, "Refresh")),
            bindings);
    }
}
