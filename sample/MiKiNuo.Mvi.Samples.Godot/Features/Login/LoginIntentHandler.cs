using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录意图处理器。
/// </summary>
public sealed class LoginIntentHandler
    : IMviIntentHandler<LoginState, LoginIntent, LoginEffect>
{
    private readonly IAuthService _authService;

    /// <summary>
    /// 初始化游戏登录意图处理器。
    /// </summary>
    /// <param name="authService">认证服务。</param>
    public LoginIntentHandler(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图集合。</returns>
    public async ValueTask<IReadOnlyList<LoginIntent>> HandleAsync(
        LoginState state,
        LoginIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (intent is LoginIntent.Submit && CanSubmit(state.UserName, state.Password))
        {
            LoginResult result = await _authService
                .LoginAsync(state.UserName, state.Password, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && result.Profile is not null)
            {
                return new LoginIntent[] { new LoginIntent.LoginSucceeded(result.Profile) };
            }

            return new LoginIntent[] { new LoginIntent.LoginFailed(result.ErrorMessage ?? "登录失败。") };
        }

        return Array.Empty<LoginIntent>();
    }

    private static bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password)
            && password.Length >= 3;
    }
}
