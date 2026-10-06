using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Runtime.MVI.Reducer;
namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.Minimal;
/// <summary>不参与业务决策的纯状态转换。</summary>
public sealed class MinimalReducer : IMviReducer<MinimalState>
{
    /// <summary>返回计数加一的状态。</summary>
    /// <param name="state">当前已提交的计数状态。</param>
    /// <param name="mutation">本次计数变化。</param>
    /// <returns>计数增加后的新状态。</returns>
    public MinimalState Reduce(MinimalState state, IMviMutation<MinimalState> mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return state with { Counter = state.Counter + 1 };
    }
}
