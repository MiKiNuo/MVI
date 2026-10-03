using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

namespace PackageConsumer;

internal static class Program
{
    internal static string ResultPath { get; private set; } = string.Empty;

    [STAThread]
    private static int Main(string[] args)
    {
        ResultPath = args.Single(value => value.StartsWith("--result-path=", StringComparison.Ordinal))[14..];
        return AppBuilder.Configure<ConsumerApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
    }
}

internal sealed class ConsumerApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        IClassicDesktopStyleApplicationLifetime desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        VerificationAuthService service = new();
        AuthFormsWindow window = new(service) { ShowInTaskbar = false };
        desktop.MainWindow = window;
        window.Opened += async (_, _) =>
        {
            int exitCode = 0;
            string report;
            try
            {
                report = await AuthFormsVerification.RunAsync(window, service, Path.ChangeExtension(Program.ResultPath, ".png"))
                    .WaitAsync(TimeSpan.FromSeconds(40));
            }
            catch (Exception exception)
            {
                exitCode = 1;
                report = "FAIL package Avalonia: " + exception;
            }

            File.WriteAllText(Program.ResultPath, report);
            Console.WriteLine(report);
            desktop.Shutdown(exitCode);
        };
        base.OnFrameworkInitializationCompleted();
    }
}
