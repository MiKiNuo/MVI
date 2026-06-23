using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Presentation.Events;

namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定搜索面板意图。
/// </summary>
public abstract partial record EventBindingSearchIntent : IMviIntent
{
    /// <summary>
    /// 表示查询文本变化意图。
    /// </summary>
    /// <param name="Payload">文本变化载荷。</param>
    public sealed partial record ChangeQuery(MviTextChangedEventPayload Payload) : EventBindingSearchIntent;
}

/// <summary>
/// 表示事件绑定搜索面板副作用。
/// </summary>
public abstract partial record EventBindingSearchEffect : IMviEffect
{
    /// <summary>
    /// 表示通知查询变化副作用。
    /// </summary>
    /// <param name="QueryText">查询文本。</param>
    public sealed partial record NotifyQueryChanged(string QueryText) : EventBindingSearchEffect;
}
