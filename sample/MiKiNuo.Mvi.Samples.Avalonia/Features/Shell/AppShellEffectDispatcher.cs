using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳副作用分发器：承载跨 Feature 导航路由，导航到主页后发布事实通知。
/// </summary>
/// <remarks>
/// 应用壳无副作用（Effect 通道为 <see cref="UnitEffect"/>），
/// 本分发器作为应用壳 Feature 的对外协调点存在。
/// </remarks>
public sealed partial class AppShellEffectDispatcher
    : MviEffectDispatcherBase<AppShellIntent, UnitEffect>
{
    private readonly MviMediatorEndpoint _mediator;

    /// <summary>
    /// 初始化应用壳副作用分发器。
    /// </summary>
    /// <param name="mediator">本实例独占的组合范围端点。</param>
    public AppShellEffectDispatcher(MviMediatorEndpoint mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        _mediator = mediator;
    }

    /// <summary>
    /// 处理跨 Feature 页面导航请求：回流对应导航意图，进入主页时发布事实通知。
    /// </summary>
    /// <param name="request">导航请求。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>导航已接纳。</returns>
    [MviRouteHandler(typeof(NavigateToPageRequest))]
    public async ValueTask<bool> HandleNavigateToPageAsync(
        NavigateToPageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AppShellIntent intent = request.Page switch
        {
            ShellPage.Register => new AppShellIntent.ShowRegister(),
            ShellPage.ResetPassword => new AppShellIntent.ShowResetPassword(),
            ShellPage.Home => new AppShellIntent.ShowHome(request.DisplayName ?? string.Empty),
            _ => new AppShellIntent.ShowLogin(),
        };

        await DispatchIntentAsync(intent, cancellationToken).ConfigureAwait(false);
        if (request.Page == ShellPage.Home)
        {
            await _mediator
                .PublishAsync(new HomeEnteredNotification(request.DisplayName ?? string.Empty), cancellationToken)
                .ConfigureAwait(false);
        }

        return true;
    }
}
