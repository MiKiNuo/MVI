using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.State;
namespace MiKiNuo.Mvi.Runtime.MVI.Reducer;
/// <summary>只根据旧状态与 Mutation 产生新状态，不接收 Intent，不执行外部操作。</summary>
/// <typeparam name="TState">不可变状态。</typeparam>
public interface IMviReducer<TState> where TState : IMviState
{
    /// <summary>执行纯状态转换。</summary>
    /// <param name="state">旧状态。</param>
    /// <param name="mutation">类型属于该状态的变化。</param>
    /// <returns>新状态；无变化时可返回原实例。</returns>
    public TState Reduce(TState state, IMviMutation<TState> mutation);
}
