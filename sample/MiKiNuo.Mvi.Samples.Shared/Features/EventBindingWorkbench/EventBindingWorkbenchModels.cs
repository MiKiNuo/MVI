using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合根状态。
/// </summary>
/// <param name="LastInteractionText">最后一次交互文本。</param>
/// <param name="InteractionCount">交互次数。</param>
public sealed record EventBindingWorkbenchState(
    string LastInteractionText,
    int InteractionCount) : IMviState;

/// <summary>
/// 表示事件绑定组合根意图。
/// </summary>
public abstract partial record EventBindingWorkbenchIntent : IMviIntent
{
    /// <summary>
    /// 表示记录子组件交互意图。
    /// </summary>
    /// <param name="SourceComponent">来源组件。</param>
    /// <param name="ActionKey">动作键。</param>
    /// <param name="ContextText">上下文文本。</param>
    public sealed partial record RecordInteraction(
        string SourceComponent,
        string ActionKey,
        string ContextText) : EventBindingWorkbenchIntent;
}

/// <summary>
/// 表示事件绑定组合根副作用。
/// </summary>
public abstract partial record EventBindingWorkbenchEffect : IMviEffect;

/// <summary>
/// 表示事件绑定组合根规约器。
/// </summary>
public sealed partial class EventBindingWorkbenchReducer
    : MviReducerBase<EventBindingWorkbenchState, EventBindingWorkbenchIntent, EventBindingWorkbenchEffect>
{
    /// <summary>
    /// 处理记录子组件交互意图。
    /// </summary>
    [MviReduce]
    private MviReduceResult<EventBindingWorkbenchState, EventBindingWorkbenchEffect> Reduce(
        EventBindingWorkbenchState state,
        EventBindingWorkbenchIntent.RecordInteraction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<EventBindingWorkbenchState, EventBindingWorkbenchEffect>(state with
        {
            LastInteractionText = $"{intent.SourceComponent}/{intent.ActionKey}: {intent.ContextText}",
            InteractionCount = state.InteractionCount + 1
        });
    }
}

/// <summary>
/// 表示事件绑定组合根空副作用分发器。
/// </summary>
public sealed class EventBindingWorkbenchEffectDispatcher : IMviEffectDispatcher<EventBindingWorkbenchEffect>
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(EventBindingWorkbenchEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        throw new NotImplementedException("事件绑定工作台当前无副作用需要派发。");
    }
}
