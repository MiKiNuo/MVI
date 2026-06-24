using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录意图处理器。
/// </summary>
public sealed class LoginIntentHandler
    : IMviIntentHandler<LoginState, LoginIntent, LoginMutation, LoginEffect>
{
    private readonly IAuthService _authService;

    /// <summary>
    /// 初始化游戏登录意图处理器。
    /// </summary>
    /// <param name="authService">后端认证服务。</param>
    public LoginIntentHandler(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public async ValueTask<MviHandleResult<LoginMutation, LoginEffect>> HandleAsync(
        LoginState state,
        LoginIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            LoginIntent.ChangeUserName changeUserName => HandleChangeUserName(state, changeUserName),
            LoginIntent.ChangePassword changePassword => HandleChangePassword(state, changePassword),
            LoginIntent.Submit => await HandleSubmitAsync(state, cancellationToken).ConfigureAwait(false),
            _ => MviHandleResult.Empty<LoginMutation, LoginEffect>(),
        };
    }

    private static MviHandleResult<LoginMutation, LoginEffect> HandleChangeUserName(
        LoginState state,
        LoginIntent.ChangeUserName intent)
    {
        bool canSubmit = CanSubmit(intent.UserName, state.Password);
        return MviHandleResult.Mutations<LoginMutation, LoginEffect>(
            new LoginMutation.SetUserName(intent.UserName),
            new LoginMutation.SetErrorMessage(null),
            new LoginMutation.SetCanSubmit(canSubmit),
            new LoginMutation.SetLoginStatus("账号已更新，登录按钮状态由 MviCommand CanExecute 自动刷新。"));
    }

    private static MviHandleResult<LoginMutation, LoginEffect> HandleChangePassword(
        LoginState state,
        LoginIntent.ChangePassword intent)
    {
        bool canSubmit = CanSubmit(state.UserName, intent.Password);
        return MviHandleResult.Mutations<LoginMutation, LoginEffect>(
            new LoginMutation.SetPassword(intent.Password),
            new LoginMutation.SetErrorMessage(null),
            new LoginMutation.SetCanSubmit(canSubmit),
            new LoginMutation.SetLoginStatus("密码已更新，ViewModel 双向绑定会生成 ChangePassword Intent。"));
    }

    private async ValueTask<MviHandleResult<LoginMutation, LoginEffect>> HandleSubmitAsync(
        LoginState state,
        CancellationToken cancellationToken)
    {
        if (!CanSubmit(state.UserName, state.Password))
        {
            return MviHandleResult.MutationsAndEffects<LoginMutation, LoginEffect>(
                new LoginMutation[]
                {
                    new LoginMutation.SetErrorMessage("账号不能为空，密码长度至少 3 位。"),
                    new LoginMutation.SetCanSubmit(false),
                    new LoginMutation.SetLoginStatus("登录校验失败，状态由 IntentHandler 返回。"),
                },
                new LoginEffect[] { new LoginEffect.Trace("Login validation failed") });
        }

        LoginResult result = await _authService
            .LoginAsync(state.UserName, state.Password, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Profile is not null)
        {
            return MviHandleResult.MutationsAndEffects<LoginMutation, LoginEffect>(
                new LoginMutation[]
                {
                    new LoginMutation.SetIsBusy(false),
                    new LoginMutation.SetErrorMessage(null),
                    new LoginMutation.SetLoginStatus($"登录成功：{result.Profile.PlayerName}，准备进入游戏大厅。"),
                },
                new LoginEffect[]
                {
                    new LoginEffect.Trace($"Login succeeded for {result.Profile.PlayerName}"),
                    new LoginEffect.LoginSucceeded(result.Profile),
                });
        }

        return MviHandleResult.Mutations<LoginMutation, LoginEffect>(
            new LoginMutation.SetErrorMessage(result.ErrorMessage ?? "登录失败。"),
            new LoginMutation.SetLoginStatus("登录失败，请检查账号和密码。"));
    }

    private static bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password)
            && password.Length >= 3;
    }
}
