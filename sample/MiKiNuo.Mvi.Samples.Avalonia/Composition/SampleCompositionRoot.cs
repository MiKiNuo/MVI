using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia.Composition;

/// <summary>
/// 表示示例应用组合根。
/// </summary>
public sealed class SampleCompositionRoot
{
    private readonly SampleGeneratedContainer _container;

    /// <summary>
    /// 初始化示例应用组合根。
    /// </summary>
    public SampleCompositionRoot()
    {
        _container = new SampleGeneratedContainer();
    }

    /// <summary>
    /// 创建主窗口。
    /// </summary>
    /// <returns>主窗口。</returns>
    public MainWindow CreateMainWindow()
    {
        _ = new CardStoreFactory(_container.Resolve<IMviMediator>());

        AppShellViewModel shellViewModel = _container.Resolve<AppShellViewModel>();
        LoginViewModel loginViewModel = _container.Resolve<LoginViewModel>();
        ValueTask showPageTask = shellViewModel.ShowPageAsync("登录", loginViewModel);
        if (!showPageTask.IsCompletedSuccessfully)
        {
            showPageTask.AsTask().GetAwaiter().GetResult();
        }
        IMviViewRegistry viewRegistry = _container.Resolve<IMviViewRegistry>();
        EventBindingWorkbenchComposition eventBindingWorkbenchComposition = EventBindingWorkbenchComposition.Create();
        return new MainWindow(shellViewModel, viewRegistry, eventBindingWorkbenchComposition);
    }
}
