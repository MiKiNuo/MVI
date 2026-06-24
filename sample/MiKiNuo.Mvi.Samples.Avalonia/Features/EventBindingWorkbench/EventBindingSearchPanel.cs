using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Domain.MVI.Binding;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板状态。
/// </summary>
/// <param name="QueryText">查询文本。</param>
/// <param name="PreviousQueryText">上一次查询文本。</param>
/// <param name="EventCount">事件次数。</param>
/// <param name="StatusText">状态文本。</param>
public sealed record EventBindingSearchState(
    string QueryText,
    string PreviousQueryText,
    int EventCount,
    string StatusText) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static EventBindingSearchState Initial { get; } = new(
        string.Empty,
        string.Empty,
        0,
        "等待 TextChanged 事件。");
}

/// <summary>
/// 表示事件绑定搜索面板副作用分发器。
/// </summary>
public sealed class EventBindingSearchEffectDispatcher : IMviEffectDispatcher<EventBindingSearchEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化事件绑定搜索面板副作用分发器。
    /// </summary>
    /// <param name="mediator">中介者。</param>
    public EventBindingSearchEffectDispatcher(IMviMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public async ValueTask DispatchAsync(EventBindingSearchEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (effect is EventBindingSearchEffect.NotifyQueryChanged queryChanged)
        {
            await _mediator.SendAsync<EventBindingWorkbenchInteractionRequest, EventBindingWorkbenchInteractionResponse>(
                new EventBindingWorkbenchInteractionRequest("SearchPanel", "TextChanged", queryChanged.QueryText),
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// 表示事件绑定搜索面板 ViewModel。
/// </summary>
public sealed partial class EventBindingSearchViewModel
    : MviViewModelBase<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect>
{
    /// <summary>
    /// 初始化事件绑定搜索面板 ViewModel。
    /// </summary>
    /// <param name="store">搜索面板 Store。</param>
    /// <param name="uiDispatcher">UI 调度器（可选）。</param>
    public EventBindingSearchViewModel(
        IMviStore<EventBindingSearchState, EventBindingSearchIntent, EventBindingSearchEffect> store,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
    }

    /// <summary>
    /// 获取查询文本。
    /// </summary>
    [MviBind(nameof(EventBindingSearchState.QueryText))]
    public partial string QueryText { get; private set; }

    /// <summary>
    /// 获取状态文本。
    /// </summary>
    [MviBind(nameof(EventBindingSearchState.StatusText))]
    public partial string StatusText { get; private set; }
}
