using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录界面意图处理器。
/// </summary>
public sealed class LoginIntentHandler
    : IMviIntentHandler<LoginState, LoginIntent, LoginEffect>
{
    /// <summary>
    /// 处理意图并产生动作副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>动作副作用集合。</returns>
    public ValueTask<IReadOnlyList<LoginEffect>> HandleAsync(
        LoginState state,
        LoginIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        IReadOnlyList<LoginEffect> effects = intent switch
        {
            LoginIntent.Submit when state.CanSubmit => new LoginEffect[]
            {
                new LoginEffect.RequestLogin(state.UserName, state.Password),
            },
            LoginIntent.LoginSucceeded succeeded => new LoginEffect[]
            {
                new LoginEffect.NavigateToDashboard(succeeded.DisplayName),
            },
            _ => Array.Empty<LoginEffect>(),
        };
        return new ValueTask<IReadOnlyList<LoginEffect>>(effects);
    }
}
