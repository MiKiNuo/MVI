using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;

namespace MiKiNuo.Mvi.Samples.Avalonia;

/// <summary>
/// 表示 Avalonia 应用。
/// </summary>
public sealed partial class App : global::Avalonia.Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        MviUiNotificationDispatcher.Configure(action => Dispatcher.UIThread.Post(action));

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            SampleCompositionRoot compositionRoot = new();
            desktop.MainWindow = compositionRoot.CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
