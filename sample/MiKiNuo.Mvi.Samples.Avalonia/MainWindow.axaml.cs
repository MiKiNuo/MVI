using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Platforms.Avalonia.Views;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia;

/// <summary>
/// 表示主窗口：订阅应用壳页面状态，按当前页渲染对应视图。
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly AppComposition _composition;
    private readonly IMviResolver _resolver;
    private readonly ContentControl _rootContent;

    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    /// <param name="composition">应用组合句柄，持有各 Feature 实例。</param>
    /// <param name="resolver">组件解析容器。</param>
    public MainWindow(AppComposition composition, IMviResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(resolver);

        _composition = composition;
        _resolver = resolver;

        AvaloniaXamlLoader.Load(this);
        _rootContent = this.FindControl<ContentControl>("RootContent")
            ?? throw new InvalidOperationException("无法找到 RootContent 控件。");

        _composition.AppShell.ViewModel.PropertyChanged += (_, args) =>
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
        Control view = _composition.AppShell.ViewModel.CurrentPage switch
        {
            ShellPage.Register => CreateView<RegisterView, RegisterViewModel>(_composition.Register.ViewModel),
            ShellPage.ResetPassword => CreateView<ResetPasswordView, ResetPasswordViewModel>(_composition.ResetPassword.ViewModel),
            ShellPage.Home => CreateView<HomeView, HomeViewModel>(_composition.Home.ViewModel),
            _ => CreateView<LoginView, LoginViewModel>(_composition.Login.ViewModel),
        };

        _rootContent.Content = view;
    }

    private TView CreateView<TView, TViewModel>(TViewModel viewModel)
        where TView : MviAvaloniaView<TViewModel>, new()
        where TViewModel : class
    {
        TView view = new();
        view.Bind(viewModel, _resolver);
        return view;
    }
}
