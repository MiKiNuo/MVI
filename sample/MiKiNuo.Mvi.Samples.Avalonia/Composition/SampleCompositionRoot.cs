using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia.Composition;

/// <summary>
/// 表示示例应用组合根：创建生成的 DI 容器并注册跨 Feature 导航路由。
/// </summary>
/// <remarks>
/// 各 Feature 的 Store / Reducer / EffectDispatcher / ViewModel 由
/// [MviFeature] 源生成器装配进 GeneratedMviContainer，此处只做路由接线。
/// </remarks>
public sealed class SampleCompositionRoot
{
    private readonly GeneratedMviContainer _container;

    /// <summary>
    /// 初始化组合根。
    /// </summary>
    /// <param name="uiDispatcher">平台 UI 调度器。</param>
    public SampleCompositionRoot(IMviUiDispatcher uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        _container = new GeneratedMviContainer(uiDispatcher);
        if (_container.Mediator is MviMediator mediator)
        {
            mediator.Register<NavigateToPageRequest, bool>(HandleNavigateToPageAsync);
        }
        else
        {
            throw new InvalidOperationException("容器中介者不支持路由注册。");
        }
    }

    /// <summary>
    /// 创建主窗口。
    /// </summary>
    /// <returns>主窗口。</returns>
    public MainWindow CreateMainWindow()
    {
        return new MainWindow(_container.Resolve<AppShellViewModel>(), _container);
    }

    private async ValueTask<bool> HandleNavigateToPageAsync(
        NavigateToPageRequest request,
        CancellationToken cancellationToken)
    {
        IMviStore<AppShellState, AppShellIntent, UnitEffect> shellStore =
            _container.Resolve<IMviStore<AppShellState, AppShellIntent, UnitEffect>>();

        AppShellIntent intent = request.Page switch
        {
            ShellPage.Register => new AppShellIntent.ShowRegister(),
            ShellPage.Home => new AppShellIntent.ShowHome(request.DisplayName ?? string.Empty),
            _ => new AppShellIntent.ShowLogin(),
        };

        await shellStore.DispatchAsync(intent, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
