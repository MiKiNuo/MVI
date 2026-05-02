using MiKiNuo.Mvi.Domain.MVI.Reducer;

using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
namespace MiKiNuo.Mvi.Application.MVI.Middleware;

/// <summary>
/// 表示 MVI 中间件步骤。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <param name="context">中间件上下文。</param>
/// <param name="cancellationToken">取消标记。</param>
/// <returns>规约结果。</returns>
public delegate ValueTask<MviReduceResult<TState, TEffect>> MviMiddlewareStep<TState, TIntent, TEffect>(
    MviMiddlewareContext<TState, TIntent, TEffect> context,
    CancellationToken cancellationToken)
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect;
