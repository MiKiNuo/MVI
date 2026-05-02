using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.Platforms.Avalonia.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia;

/// <summary>
/// 表示主窗口。
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly AppShellViewModel _viewModel;
    private readonly IMviViewRegistry _viewRegistry;
    private ContentControl _rootContent = null!;

    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    /// <param name="viewModel">应用壳 ViewModel。</param>
    /// <param name="viewRegistry">视图注册表。</param>
    public MainWindow(AppShellViewModel viewModel, IMviViewRegistry viewRegistry)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(viewRegistry);

        _viewModel = viewModel;
        _viewRegistry = viewRegistry;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AppShellViewModel.CurrentViewModel))
            {
                RenderCurrentView();
            }
        };
        RenderCurrentView();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _rootContent = this.FindControl<ContentControl>("RootContent")
            ?? throw new InvalidOperationException("无法找到 RootContent 控件。");
    }

    private void RenderCurrentView()
    {
        if (_viewModel.CurrentViewModel is null)
        {
            _rootContent.Content = null;
            return;
        }

        _rootContent.Content = _viewRegistry.CreateView(_viewModel.CurrentViewModel);
    }
}
