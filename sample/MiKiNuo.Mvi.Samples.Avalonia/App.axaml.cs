using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Threading;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.V2Input;

namespace MiKiNuo.Mvi.Samples.Avalonia;

/// <summary>
/// 表示 Avalonia 应用。
/// </summary>
public sealed partial class App : global::Avalonia.Application
{
    /// <summary>
    /// 初始化应用程序。
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 框架初始化完成时创建主窗口。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Program.UseV2Input)
            {
                InputFormWindow window = new();
                desktop.MainWindow = window;
                if (Program.VerifyV2Input)
                {
                    window.ShowInTaskbar = false;
                    window.WindowState = global::Avalonia.Controls.WindowState.Minimized;
                    window.Opened += async (_, _) =>
                    {
                        int exitCode = 0;
                        string report;
                        try
                        {
                            report = await InputFormVerification.RunAsync(window).WaitAsync(TimeSpan.FromSeconds(20));
                        }
                        catch (Exception exception)
                        {
                            exitCode = 1;
                            report = "FAIL v2-input: " + exception;
                        }

                        try
                        {
                            File.WriteAllText(Program.VerificationPath, report);
                            Console.WriteLine(report);
                        }
                        finally
                        {
                            desktop.Shutdown(exitCode);
                        }
                    };
                }
            }
            else
            {
                SampleCompositionRoot compositionRoot = new(new AvaloniaMviUiDispatcher());
                desktop.MainWindow = compositionRoot.CreateMainWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
