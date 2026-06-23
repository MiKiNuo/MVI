using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Domain.MVI.Mutation;

/// <summary>
/// 表示绑定状态类型的 MVI 变更。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
public interface IMviMutation<TState> : IMviMutation
    where TState : IMviState
{
}
