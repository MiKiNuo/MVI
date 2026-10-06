using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Binding.Views;
using MiKiNuo.Mvi.Platforms.Avalonia.Threading;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
namespace MiKiNuo.Mvi.Samples.Avalonia;
/// <summary>单一应用窗口，缓存页面 View 并在关闭前等待组合释放。</summary>
public sealed partial class MainWindow : Window
{
    private readonly SampleCompositionRoot _root = new(new AvaloniaMviUiDispatcher());
    private readonly Dictionary<ShellPage, Control> _views = [];
    private AppComposition? _composition;
    private ContentControl _content = null!;
    private bool _closing;
    private bool _closed;
    /// <summary>加载窗口并连接平台生命周期。</summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _content = this.FindControl<ContentControl>("RootContent") ?? throw new InvalidOperationException("缺少 RootContent。");
        Opened += InitializeAsync;
        Closing += CloseAsync;
    }
    private async void InitializeAsync(object? sender, EventArgs args)
    {
        try
        {
            _composition = await _root.CreateAsync();
            if (_closing) { await _composition.DisposeAsync(); return; }
            _views.Add(ShellPage.Login, Create<LoginView, LoginViewModel>(_composition.Login.ViewModel));
            _views.Add(ShellPage.Register, Create<RegisterView, RegisterViewModel>(_composition.Register.ViewModel));
            _views.Add(ShellPage.ResetPassword, Create<ResetPasswordView, ResetPasswordViewModel>(_composition.ResetPassword.ViewModel));
            _views.Add(ShellPage.Home, Create<HomeView, HomeViewModel>(_composition.Home.ViewModel));
            _composition.AppShell.ViewModel.PropertyChanged += PageChanged;
            Render();
        }
        catch (Exception exception) { _content.Content = new TextBlock { Text = "初始化失败：" + exception.Message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap }; }
    }
    private TView Create<TView, TModel>(TModel model) where TView : Control, IMviView<TModel>, new() where TModel : class
    { var view = new TView(); view.Bind(model, _root.Container); return view; }
    private void PageChanged(object? sender, PropertyChangedEventArgs args) { if (args.PropertyName == "State") Render(); }
    private void Render() { if (_composition is not null && !_closing) _content.Content = _views[_composition.AppShell.ViewModel.State.CurrentPage]; }
    private async void CloseAsync(object? sender, WindowClosingEventArgs args)
    {
        if (_closed) return;
        args.Cancel = true;
        if (_closing) return;
        _closing = true; _content.Content = null;
        try
        {
            if (_composition is not null)
            {
                _composition.AppShell.ViewModel.PropertyChanged -= PageChanged;
                await _composition.DisposeAsync();
            }
        }
        catch (Exception exception) { System.Diagnostics.Trace.TraceError(exception.ToString()); }
        finally { _views.Clear(); _closed = true; Close(); }
    }
}
