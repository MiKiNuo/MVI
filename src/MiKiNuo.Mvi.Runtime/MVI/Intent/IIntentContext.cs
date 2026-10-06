using MiKiNuo.Mvi.Abstractions.MVI.Mutation;
using MiKiNuo.Mvi.Abstractions.MVI.State;
namespace MiKiNuo.Mvi.Runtime.MVI.Intent;
/// <summary>限定单次意图的状态提交权限；禁止在处理方法返回后继续使用。</summary>
/// <typeparam name="TState">本组件的不可变状态。</typeparam>
public interface IIntentContext<TState> where TState : IMviState
{
    /// <summary>获取最近一次已提交的状态快照，不是 ViewModel 的投影。</summary>
    public TState State { get; }
    /// <summary>通过纯 Reducer 提交一次状态变化。</summary>
    /// <param name="mutation">与本状态匹配的变化。</param>
    public void Reduce(IMviMutation<TState> mutation);
    /// <summary>在同一临界区内检查业务前提并提交变化，避免检查后再写入的竞态。</summary>
    /// <param name="guard">短小、同步、无副作用的业务前提。</param>
    /// <param name="mutation">前提满足后应用的变化。</param>
    /// <returns>本次变化是否被提交。</returns>
    public bool TryReduce(Func<TState, bool> guard, IMviMutation<TState> mutation);
}
