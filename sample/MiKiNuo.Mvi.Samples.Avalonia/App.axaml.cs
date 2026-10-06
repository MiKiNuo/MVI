using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
namespace MiKiNuo.Mvi.Samples.Avalonia;
/// <summary>示例应用。</summary>
public sealed partial class App : global::Avalonia.Application
{
    /// <summary>加载主题。</summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    /// <summary>创建窗口；组合在窗口打开后异步初始化。</summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
