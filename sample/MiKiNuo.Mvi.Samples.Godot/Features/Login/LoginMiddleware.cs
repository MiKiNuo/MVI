using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录中间件。
/// </summary>
public sealed class LoginMiddleware : IMviMiddleware<LoginState, LoginIntent, LoginEffect>
{
    /// <summary>
    /// 调用下一中间件并附加追踪副作用。
    /// </summary>
    /// <param name="context">中间件上下文。</param>
    /// <param name="nextMiddleware">下一中间件步骤。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>归约结果。</returns>
    public async ValueTask<MviReduceResult<LoginState, LoginEffect>> InvokeAsync(
        MviMiddlewareContext<LoginState, LoginIntent, LoginEffect> context,
        MviMiddlewareStep<LoginState, LoginIntent, LoginEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);
        string before = $"Login Middleware Before Intent={typeof(LoginIntent).Name}";
        MviReduceResult<LoginState, LoginEffect> result = await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
        string after = $"Login Middleware After CanSubmit={result.State.CanSubmit}";
        return MviReduceResult.StateAndEffects<LoginState, LoginEffect>(
            result.State,
            [.. result.Effects, new LoginEffect.Trace(before), new LoginEffect.Trace(after)]);
    }
}
