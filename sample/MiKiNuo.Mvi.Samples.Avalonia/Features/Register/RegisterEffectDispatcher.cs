using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Register;

/// <summary>
/// 表示注册页副作用分发器：执行联网注册并回流结果意图，导航经 Mediator 协调应用壳。
/// </summary>
public sealed partial class RegisterEffectDispatcher
    : MviEffectDispatcherBase<RegisterIntent, RegisterEffect>
{
    private readonly IAuthService _authService;
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化注册页副作用分发器。
    /// </summary>
    /// <param name="authService">认证服务。</param>
    /// <param name="mediator">跨 Feature 协调中介者。</param>
    public RegisterEffectDispatcher(IAuthService authService, IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(mediator);
        _authService = authService;
        _mediator = mediator;
    }

    /// <summary>
    /// 执行联网注册并回流成功/失败意图。
    /// </summary>
    [MviEffect(typeof(RegisterEffect.PerformRegister))]
    private async ValueTask HandlePerformRegister(
        RegisterEffect.PerformRegister effect,
        CancellationToken cancellationToken)
    {
        AuthResult result = await _authService
            .RegisterAsync(effect.UserName, effect.Email, effect.Password, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await DispatchIntentAsync(new RegisterIntent.Succeeded(), cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await DispatchIntentAsync(new RegisterIntent.Failed(result.ErrorMessage ?? "注册失败。"), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 经 Mediator 请求应用壳跳转到登录页。
    /// </summary>
    [MviEffect(typeof(RegisterEffect.ShowLoginPage))]
    private async ValueTask HandleShowLoginPage(
        RegisterEffect.ShowLoginPage effect,
        CancellationToken cancellationToken)
    {
        _ = await _mediator
            .SendAsync(new NavigateToPageRequest(ShellPage.Login, null), cancellationToken)
            .ConfigureAwait(false);
    }
}
