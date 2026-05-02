using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
namespace MiKiNuo.Mvi.Application.MVI.Middleware;

/// <summary>
/// 表示 MVI 中间件上下文。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <param name="State">当前状态。</param>
/// <param name="Intent">当前意图。</param>
public sealed record MviMiddlewareContext<TState, TIntent, TEffect>(
    TState State,
    TIntent Intent)
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect;
