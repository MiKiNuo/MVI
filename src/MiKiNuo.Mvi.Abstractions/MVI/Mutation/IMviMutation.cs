using MiKiNuo.Mvi.Abstractions.MVI.State;
namespace MiKiNuo.Mvi.Abstractions.MVI.Mutation;
/// <summary>描述某种状态的不可变变化；它不是用户意图，也不执行外部操作。</summary>
/// <typeparam name="TState">唯一允许被该变化更新的状态。</typeparam>
public interface IMviMutation<TState> where TState : IMviState { }
