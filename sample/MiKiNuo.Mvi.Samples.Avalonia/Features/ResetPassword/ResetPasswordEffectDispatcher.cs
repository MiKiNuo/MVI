using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;

/// <summary>
/// 表示重置密码页副作用分发器：执行联网重置并回流结果意图，导航经 Mediator 协调应用壳。
/// </summary>
public sealed partial class ResetPasswordEffectDispatcher
    : MviEffectDispatcherBase<ResetPasswordIntent, ResetPasswordEffect>
{
    private readonly IAuthService _authService;
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化重置密码页副作用分发器。
    /// </summary>
    /// <param name="authService">认证服务。</param>
    /// <param name="mediator">跨 Feature 协调中介者。</param>
    public ResetPasswordEffectDispatcher(IAuthService authService, IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(mediator);
        _authService = authService;
        _mediator = mediator;
    }

    /// <summary>
    /// 执行联网重置密码并回流成功/失败意图。
    /// </summary>
    [MviEffect(typeof(ResetPasswordEffect.PerformResetPassword))]
    private async ValueTask HandlePerformResetPassword(
        ResetPasswordEffect.PerformResetPassword effect,
        CancellationToken cancellationToken)
    {
        AuthResult result = await _authService
            .ResetPasswordAsync(effect.UserName, effect.NewPassword, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await DispatchIntentAsync(new ResetPasswordIntent.Succeeded(), cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await DispatchIntentAsync(new ResetPasswordIntent.Failed(result.ErrorMessage ?? "重置密码失败。"), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 经 Mediator 请求应用壳返回登录页。
    /// </summary>
    [MviEffect(typeof(ResetPasswordEffect.ShowLoginPage))]
    private async ValueTask HandleShowLoginPage(
        ResetPasswordEffect.ShowLoginPage effect,
        CancellationToken cancellationToken)
    {
        _ = await _mediator
            .SendAsync(new NavigateToPageRequest(ShellPage.Login, null), cancellationToken)
            .ConfigureAwait(false);
    }
}
