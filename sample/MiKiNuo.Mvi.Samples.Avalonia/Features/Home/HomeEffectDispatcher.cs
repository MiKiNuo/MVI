using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

/// <summary>
/// 表示主页副作用分发器。
/// </summary>
public sealed partial class HomeEffectDispatcher
    : MviEffectDispatcherBase<HomeIntent, HomeEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化主页副作用分发器。
    /// </summary>
    /// <param name="mediator">跨 Feature 协调中介者。</param>
    public HomeEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        _mediator = mediator;
    }

    /// <summary>
    /// 经 Mediator 请求应用壳跳转到登录页。
    /// </summary>
    [MviEffect(typeof(HomeEffect.ShowLoginPage))]
    private async ValueTask HandleShowLoginPage(
        HomeEffect.ShowLoginPage effect,
        CancellationToken cancellationToken)
    {
        _ = await _mediator
            .SendAsync(new NavigateToPageRequest(ShellPage.Login, null), cancellationToken)
            .ConfigureAwait(false);
    }
}
