using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.State;
namespace MiKiNuo.Mvi.Runtime.MVI.Reducer;
/// <summary>为标记 MviReduce 的纯方法提供生成式 Mutation 分派。</summary>
/// <typeparam name="TState">不可变状态。</typeparam>
public abstract class MviReducerBase<TState> : IMviReducer<TState> where TState : IMviState
{
    /// <summary>由生成器路由到对应的纯状态转换方法。</summary>
    /// <param name="state">旧状态。</param>
    /// <param name="mutation">状态变化。</param>
    /// <returns>新状态。</returns>
    public abstract TState Reduce(TState state, IMviMutation<TState> mutation);
}
