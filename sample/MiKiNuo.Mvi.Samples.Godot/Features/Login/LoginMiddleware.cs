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
    /// <inheritdoc />
    public async ValueTask<MviReduceResult<LoginState, LoginEffect>> InvokeAsync(
        MviMiddlewareContext<LoginState, LoginIntent, LoginEffect> context,
        MviMiddlewareStep<LoginState, LoginIntent, LoginEffect> nextMiddleware,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextMiddleware);
        string before = $"Login Middleware Before Intent={context.Intent.GetType().Name}";
        MviReduceResult<LoginState, LoginEffect> result = await nextMiddleware(context, cancellationToken).ConfigureAwait(false);
        string after = $"Login Middleware After CanSubmit={result.State.CanSubmit}";
        return MviReduceResult.StateAndEffects<LoginState, LoginEffect>(
            result.State,
            [.. result.Effects, new LoginEffect.Trace(before), new LoginEffect.Trace(after)]);
    }
}
