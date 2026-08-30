using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia;

/// <summary>
/// 表示主窗口：订阅应用壳页面状态，按当前页渲染对应视图。
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly AppShellViewModel _shellViewModel;
    private readonly IMviResolver _resolver;
    private readonly ContentControl _rootContent;

    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    /// <param name="shellViewModel">应用壳 ViewModel。</param>
    /// <param name="resolver">组件解析容器。</param>
    public MainWindow(AppShellViewModel shellViewModel, IMviResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(shellViewModel);
        ArgumentNullException.ThrowIfNull(resolver);

        _shellViewModel = shellViewModel;
        _resolver = resolver;

        AvaloniaXamlLoader.Load(this);
        _rootContent = this.FindControl<ContentControl>("RootContent")
            ?? throw new InvalidOperationException("无法找到 RootContent 控件。");

        _shellViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AppShellViewModel.CurrentPage))
            {
                RenderCurrentPage();
            }
        };

        RenderCurrentPage();
    }

    private void RenderCurrentPage()
    {
        Control view = _shellViewModel.CurrentPage switch
        {
            ShellPage.Register => CreateView<RegisterView, RegisterViewModel>(),
            ShellPage.Home => CreateView<HomeView, HomeViewModel>(),
            _ => CreateView<LoginView, LoginViewModel>(),
        };

        _rootContent.Content = view;
    }

    private TView CreateView<TView, TViewModel>()
        where TView : MviAvaloniaView<TViewModel>, new()
        where TViewModel : class
    {
        TView view = new();
        view.Bind(_resolver.Resolve<TViewModel>(), _resolver);
        return view;
    }
}
