using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录界面意图处理器。
/// </summary>
public sealed class LoginIntentHandler
    : IMviIntentHandler<LoginState, LoginIntent, LoginMutation, LoginEffect>
{
    private readonly IAuthService _authService;

    /// <summary>
    /// 初始化登录界面意图处理器。
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
            new LoginMutation.SetCanSubmit(canSubmit));
    }

    private static MviHandleResult<LoginMutation, LoginEffect> HandleChangePassword(
        LoginState state,
        LoginIntent.ChangePassword intent)
    {
        bool canSubmit = CanSubmit(state.UserName, intent.Password);
        return MviHandleResult.Mutations<LoginMutation, LoginEffect>(
            new LoginMutation.SetPassword(intent.Password),
            new LoginMutation.SetErrorMessage(null),
            new LoginMutation.SetCanSubmit(canSubmit));
    }

    private async ValueTask<MviHandleResult<LoginMutation, LoginEffect>> HandleSubmitAsync(
        LoginState state,
        CancellationToken cancellationToken)
    {
        if (!state.CanSubmit)
        {
            return MviHandleResult.Empty<LoginMutation, LoginEffect>();
        }

        MviHandleResult<LoginMutation, LoginEffect> busyResult = MviHandleResult.Mutations<LoginMutation, LoginEffect>(
            new LoginMutation.SetIsBusy(true),
            new LoginMutation.SetErrorMessage(null),
            new LoginMutation.SetCanSubmit(false));
        LoginResult result = await _authService
            .LoginAsync(state.UserName, state.Password, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.DisplayName is not null)
        {
            return MviHandleResult.MutationsAndEffects<LoginMutation, LoginEffect>(
                new LoginMutation[]
                {
                    new LoginMutation.SetIsBusy(false),
                    new LoginMutation.SetErrorMessage(null),
                    new LoginMutation.SetCanSubmit(true),
                },
                new LoginEffect[] { new LoginEffect.NavigateToDashboard(result.DisplayName) });
        }

        return MviHandleResult.Mutations<LoginMutation, LoginEffect>(
            new LoginMutation.SetIsBusy(false),
            new LoginMutation.SetErrorMessage(result.ErrorMessage ?? "登录失败。"),
            new LoginMutation.SetCanSubmit(CanSubmit(state.UserName, state.Password)));
    }

    private static bool CanSubmit(string userName, string password)
    {
        return !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(password);
    }
}
