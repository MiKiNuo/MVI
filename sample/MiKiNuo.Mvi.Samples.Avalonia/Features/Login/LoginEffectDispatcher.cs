using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录副作用分发器。
/// </summary>
public sealed class LoginEffectDispatcher : IMviEffectDispatcher<LoginEffect>
{
    private readonly IAuthService _authService;
    private readonly ILoginNavigationService _navigationService;
    private readonly Func<IMviStore<LoginState, LoginIntent, LoginEffect>> _storeFactory;

    /// <summary>
    /// 初始化登录副作用分发器。
    /// </summary>
    public LoginEffectDispatcher(
        IAuthService authService,
        ILoginNavigationService navigationService,
        Func<IMviStore<LoginState, LoginIntent, LoginEffect>> storeFactory)
    {
        _authService = authService;
        _navigationService = navigationService;
        _storeFactory = storeFactory;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(LoginEffect effect, CancellationToken cancellationToken = default)
    {
        switch (effect)
        {
            case LoginEffect.RequestLogin requestLogin:
                await HandleRequestLoginAsync(requestLogin, cancellationToken).ConfigureAwait(false);
                break;
            case LoginEffect.NavigateToDashboard navigateToDashboard:
                await _navigationService.NavigateToDashboardAsync(
                    navigateToDashboard.DisplayName,
                    cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async ValueTask HandleRequestLoginAsync(
        LoginEffect.RequestLogin effect,
        CancellationToken cancellationToken)
    {
        LoginResult result = await _authService
            .LoginAsync(effect.UserName, effect.Password, cancellationToken)
            .ConfigureAwait(false);

        IMviStore<LoginState, LoginIntent, LoginEffect> store = _storeFactory();

        if (result.IsSuccess && result.DisplayName is not null)
        {
            await store.DispatchAsync(new LoginIntent.LoginSucceeded(result.DisplayName), cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await store.DispatchAsync(
            new LoginIntent.LoginFailed(result.ErrorMessage ?? "登录失败。"),
            cancellationToken).ConfigureAwait(false);
    }
}
