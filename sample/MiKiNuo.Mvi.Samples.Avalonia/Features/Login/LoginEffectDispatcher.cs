using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录副作用分发器。
/// </summary>
public sealed class LoginEffectDispatcher : IMviEffectDispatcher<LoginEffect>
{
    private readonly ILoginNavigationService _navigationService;

    /// <summary>
    /// 初始化登录副作用分发器。
    /// </summary>
    /// <param name="navigationService">登录导航服务。</param>
    public LoginEffectDispatcher(ILoginNavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public async ValueTask DispatchAsync(LoginEffect effect, CancellationToken cancellationToken = default)
    {
        switch (effect)
        {
            case LoginEffect.NavigateToDashboard navigateToDashboard:
                await _navigationService.NavigateToDashboardAsync(
                    navigateToDashboard.DisplayName,
                    cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
