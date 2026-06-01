using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia;

/// <summary>
/// 表示主窗口。
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly AppShellViewModel _viewModel;
    private readonly IMviViewRegistry _viewRegistry;
    private readonly EventBindingWorkbenchComposition _eventBindingWorkbenchComposition;
    private readonly string _originalSampleTitle;
    private readonly object? _originalSampleViewModel;
    private ContentControl _rootContent = null!;
    private Button _originalSampleButton = null!;
    private Button _eventBindingWorkbenchButton = null!;

    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    /// <param name="viewModel">应用壳 ViewModel。</param>
    /// <param name="viewRegistry">视图注册表。</param>
    /// <param name="eventBindingWorkbenchComposition">事件绑定复杂组合示例。</param>
    public MainWindow(
        AppShellViewModel viewModel,
        IMviViewRegistry viewRegistry,
        EventBindingWorkbenchComposition eventBindingWorkbenchComposition)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(viewRegistry);
        ArgumentNullException.ThrowIfNull(eventBindingWorkbenchComposition);

        _viewModel = viewModel;
        _viewRegistry = viewRegistry;
        _eventBindingWorkbenchComposition = eventBindingWorkbenchComposition;
        InitializeComponent();
        DataContext = viewModel;
        _originalSampleTitle = viewModel.Title;
        _originalSampleViewModel = viewModel.CurrentViewModel;
        _originalSampleButton.Click += async (_, _) => await ShowOriginalSampleAsync();
        _eventBindingWorkbenchButton.Click += async (_, _) => await ShowEventBindingWorkbenchAsync();
        Closed += (_, _) => _eventBindingWorkbenchComposition.Dispose();
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
        _originalSampleButton = this.FindControl<Button>("OriginalSampleButton")
            ?? throw new InvalidOperationException("无法找到 OriginalSampleButton 控件。");
        _eventBindingWorkbenchButton = this.FindControl<Button>("EventBindingWorkbenchButton")
            ?? throw new InvalidOperationException("无法找到 EventBindingWorkbenchButton 控件。");
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

    private ValueTask ShowOriginalSampleAsync()
    {
        if (_originalSampleViewModel is null)
        {
            return ValueTask.CompletedTask;
        }

        return _viewModel.ShowPageAsync(_originalSampleTitle, _originalSampleViewModel);
    }

    private ValueTask ShowEventBindingWorkbenchAsync()
    {
        return _viewModel.ShowPageAsync(
            "事件绑定 Workbench",
            _eventBindingWorkbenchComposition.WorkbenchViewModel);
    }
}
