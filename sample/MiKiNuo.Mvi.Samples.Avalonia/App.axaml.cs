using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Platforms.Avalonia.Threading;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.V2Input;
using MiKiNuo.Mvi.Samples.Avalonia.Features.V2Search;
using MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;
using MiKiNuo.Mvi.Samples.Avalonia.Features.V2Workspace;

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
            if (Program.UseV2Workspace)
            {
                WorkspaceWindow window = new();
                desktop.MainWindow = window;
                if (Program.VerifyV2Close)
                {
                    desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                    ConfigureVerification(desktop, window, () => CloseVerification.RunAsync(window), Program.CloseVerificationPath, "v2-close");
                }
                else if (Program.VerifyV2Workspace)
                {
                    ConfigureVerification(desktop, window, () => WorkspaceVerification.RunAsync(window),
                        Program.WorkspaceVerificationPath, "v2-workspace");
                }
            }
            else if (Program.UseV2Remount)
            {
                RemountWindow window = new();
                desktop.MainWindow = window;
                if (Program.VerifyV2Remount)
                {
                    ConfigureVerification(desktop, window, () => RemountVerification.RunAsync(window),
                        Program.RemountVerificationPath, "v2-remount");
                }
            }
            else if (Program.UseV2Search)
            {
                ControlledSearchService? service = Program.VerifyV2Search ? new ControlledSearchService() : null;
                SearchWindow window = service is null ? new SearchWindow() : new SearchWindow(service.SearchAsync);
                desktop.MainWindow = window;
                if (service is not null)
                {
                    ConfigureVerification(desktop, window, () => SearchVerification.RunAsync(window, service),
                        Program.SearchVerificationPath, "v2-search");
                }
            }
            else if (Program.UseV2Auth)
            {
                VerificationAuthService? service = Program.VerifyV2Auth ? new() : null;
                AuthFormsWindow window = new(service);
                desktop.MainWindow = window;
                if (service is not null)
                {
                    window.ShowInTaskbar = false;
                    window.Opened += async (_, _) =>
                    {
                        int exitCode = 0;
                        string report;
                        try
                        {
                            report = await AuthFormsVerification.RunAsync(window, service, Program.AuthVerificationPath + ".png").WaitAsync(TimeSpan.FromSeconds(40));
                        }
                        catch (Exception exception)
                        {
                            exitCode = 1;
                            report = "FAIL v2-auth: " + exception;
                        }

                        try
                        {
                            File.WriteAllText(Program.AuthVerificationPath, report);
                            Console.WriteLine(report);
                        }
                        finally
                        {
                            desktop.Shutdown(exitCode);
                        }
                    };
                }
            }
            else if (Program.UseV2Input)
            {
                InputFormWindow window = new();
                desktop.MainWindow = window;
                if (Program.VerifyV2Input)
                {
                    ConfigureVerification(desktop, window, () => InputFormVerification.RunAsync(window),
                        Program.VerificationPath, "v2-input");
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

    private static void ConfigureVerification(IClassicDesktopStyleApplicationLifetime desktop, global::Avalonia.Controls.Window window,
        Func<Task<string>> verify, string path, string name)
    {
        window.ShowInTaskbar = false;
        window.WindowState = global::Avalonia.Controls.WindowState.Minimized;
        window.Opened += async (_, _) =>
        {
            int exitCode = 0;
            string report;
            try
            {
                report = await verify().WaitAsync(TimeSpan.FromSeconds(20));
            }
            catch (Exception exception)
            {
                exitCode = 1;
                report = "FAIL " + name + ": " + exception;
            }

            try
            {
                File.WriteAllText(path, report);
                Console.WriteLine(report);
            }
            finally
            {
                desktop.Shutdown(exitCode);
            }
        };
    }
}
