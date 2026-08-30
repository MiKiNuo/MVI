using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录页副作用分发器：执行联网认证并回流结果意图，导航经 Mediator 协调应用壳。
/// </summary>
public sealed partial class LoginEffectDispatcher
    : MviEffectDispatcherBase<LoginIntent, LoginEffect>
{
    private readonly IAuthService _authService;
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化登录页副作用分发器。
    /// </summary>
    /// <param name="authService">认证服务。</param>
    /// <param name="mediator">跨 Feature 协调中介者。</param>
    public LoginEffectDispatcher(IAuthService authService, IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(mediator);
        _authService = authService;
        _mediator = mediator;
    }

    /// <summary>
    /// 执行联网登录并回流成功/失败意图。
    /// </summary>
    [MviEffect(typeof(LoginEffect.PerformLogin))]
    private async ValueTask HandlePerformLogin(
        LoginEffect.PerformLogin effect,
        CancellationToken cancellationToken)
    {
        AuthResult result = await _authService
            .LoginAsync(effect.UserName, effect.Password, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.DisplayName is not null)
        {
            await DispatchIntentAsync(new LoginIntent.Succeeded(result.DisplayName), cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await DispatchIntentAsync(new LoginIntent.Failed(result.ErrorMessage ?? "登录失败。"), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 经 Mediator 请求应用壳跳转到注册页。
    /// </summary>
    [MviEffect(typeof(LoginEffect.ShowRegisterPage))]
    private async ValueTask HandleShowRegisterPage(
        LoginEffect.ShowRegisterPage effect,
        CancellationToken cancellationToken)
    {
        _ = await _mediator
            .SendAsync(new NavigateToPageRequest(ShellPage.Register, null), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 经 Mediator 请求应用壳跳转到主页。
    /// </summary>
    [MviEffect(typeof(LoginEffect.ShowHomePage))]
    private async ValueTask HandleShowHomePage(
        LoginEffect.ShowHomePage effect,
        CancellationToken cancellationToken)
    {
        _ = await _mediator
            .SendAsync(new NavigateToPageRequest(ShellPage.Home, effect.DisplayName), cancellationToken)
            .ConfigureAwait(false);
    }
}
