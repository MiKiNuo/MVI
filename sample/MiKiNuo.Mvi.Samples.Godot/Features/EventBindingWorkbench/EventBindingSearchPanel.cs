using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;

/// <summary>表示 Godot 搜索面板状态。</summary>
/// <param name="QueryText">查询文本。</param>
/// <param name="EventCount">事件次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingSearchState(
    string QueryText,
    int EventCount,
    string StatusText) : IMviState
{
    /// <summary>获取初始状态。</summary>
    public static EventBindingSearchState Initial { get; } = new(string.Empty, 0, "等待 LineEdit.TextChanged。");
}

/// <summary>表示 Godot 搜索面板 ViewModel。</summary>
/// <remarks>
/// 事件绑定通过 <see cref="Application.MVI.EventBinding.IEventSource{TEvent}"/> 适配器 +
/// <see cref="Application.MVI.EventBinding.EventBinding{TEvent}"/> 在 View 层完成，
/// 事件直接映射为 <see cref="EventBindingSearchIntent.ChangeQuery"/> 意图派发到 Store，不经过命令层。
/// </remarks>
public sealed partial class EventBindingSearchViewModel
    : MviViewModelBase<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>初始化 Godot 搜索面板 ViewModel。</summary>
    public EventBindingSearchViewModel(IMviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> store)
        : base(store)
    {
    }

    /// <summary>获取查询文本。</summary>
    [MviBind(nameof(EventBindingSearchState.QueryText))]
    public partial string QueryText { get; private set; }
}
