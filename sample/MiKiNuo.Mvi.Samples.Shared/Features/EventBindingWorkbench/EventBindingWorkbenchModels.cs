using MiKiNuo.Mvi.Application.MVI.Reducer;
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
