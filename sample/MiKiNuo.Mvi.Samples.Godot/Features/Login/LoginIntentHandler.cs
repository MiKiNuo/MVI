using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录意图处理器。
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
            LoginIntent.Submit when CanSubmit(state.UserName, state.Password) => new LoginEffect[]
            {
                new LoginEffect.RequestLogin(state.UserName, state.Password),
            },
            LoginIntent.LoginSucceeded succeeded => new LoginEffect[]
            {
                new LoginEffect.LoginSucceeded(succeeded.Profile),
                new LoginEffect.Trace($"Login succeeded for {succeeded.Profile.PlayerName}"),
            },
            LoginIntent.LoginFailed => new LoginEffect[]
            {
                new LoginEffect.Trace("Login validation failed"),
            },
            _ => Array.Empty<LoginEffect>(),
        };
        return new ValueTask<IReadOnlyList<LoginEffect>>(effects);
    }

    private static bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password)
            && password.Length >= 3;
    }
}
