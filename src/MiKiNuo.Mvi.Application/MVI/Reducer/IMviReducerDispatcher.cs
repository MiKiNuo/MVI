using MiKiNuo.Mvi.Domain.MVI.Reducer;

using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
namespace MiKiNuo.Mvi.Application.MVI.Reducer;

/// <summary>
/// 表示 MVI 规约分发器。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IMviReducerDispatcher<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>
    /// 规约意图并生成新状态。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public MviReduceResult<TState, TEffect> Reduce(TState state, TIntent intent);
}
