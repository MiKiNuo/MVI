using Avalonia;

namespace MiKiNuo.Mvi.Samples.Avalonia;

/// <summary>
/// 表示 Avalonia 应用程序入口。
/// </summary>
internal static class Program
{
    internal static bool UseV2Input { get; private set; }
    internal static bool VerifyV2Input { get; private set; }
    internal static bool UseV2Auth { get; private set; }
    internal static bool VerifyV2Auth { get; private set; }
    internal static string AuthVerificationPath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "v2-auth-verification.txt");
    internal static string VerificationPath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "v2-input-verification.txt");
    /// <summary>
    /// 启动应用程序。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    /// <returns>平台宿主或自动验收的退出码。</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        VerifyV2Input = args.Contains("--verify-v2-input", StringComparer.Ordinal);
        UseV2Input = VerifyV2Input || args.Contains("--v2-input", StringComparer.Ordinal);
        VerifyV2Auth = args.Contains("--verify-v2-auth", StringComparer.Ordinal);
        UseV2Auth = VerifyV2Auth || args.Contains("--v2-auth", StringComparer.Ordinal);
        string? authResult = args.FirstOrDefault(static argument => argument.StartsWith("--v2-auth-result=", StringComparison.Ordinal));
        if (authResult is not null) AuthVerificationPath = Path.GetFullPath(authResult["--v2-auth-result=".Length..]);
        string? result = args.FirstOrDefault(static argument => argument.StartsWith("--v2-input-result=", StringComparison.Ordinal));
        if (result is not null)
        {
            VerificationPath = Path.GetFullPath(result["--v2-input-result=".Length..]);
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 构建 Avalonia 应用程序。
    /// </summary>
    /// <returns>Avalonia 应用构建器。</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
