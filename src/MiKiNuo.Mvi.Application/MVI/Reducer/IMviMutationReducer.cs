using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.Reducer;

/// <summary>
/// 表示变更规约器，将变更应用到状态。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TMutation">变更类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IMviMutationReducer<TState, TMutation, TEffect>
    where TState : IMviState
    where TMutation : IMviMutation
    where TEffect : IMviEffect
{
    /// <summary>
    /// 将单个变更应用到当前状态。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    public MviReduceResult<TState, TEffect> Reduce(TState state, TMutation mutation);
}
