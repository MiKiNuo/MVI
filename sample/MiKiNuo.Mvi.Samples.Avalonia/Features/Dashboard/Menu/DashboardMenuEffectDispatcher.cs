using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;

/// <summary>
/// 表示 Dashboard 菜单副作用分发器。
/// </summary>
public sealed class DashboardMenuEffectDispatcher : IMviEffectDispatcher<DashboardMenuEffect>
{
    private readonly IMviMediator _mediator;

    /// <summary>
    /// 初始化 Dashboard 菜单副作用分发器。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    public DashboardMenuEffectDispatcher(IMviMediator mediator)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _mediator = mediator;
    }

    /// <inheritdoc />
    public async ValueTask DispatchAsync(DashboardMenuEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is DashboardMenuEffect.RequestNavigation requestNavigation)
        {
            await _mediator.SendAsync<NavigateDashboardPageRequest, DashboardNavigationResponse>(
                new NavigateDashboardPageRequest(requestNavigation.PageKey),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
