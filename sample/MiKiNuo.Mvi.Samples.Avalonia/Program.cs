using Avalonia;

namespace MiKiNuo.Mvi.Samples.Avalonia;

/// <summary>
/// 表示 Avalonia 应用程序入口。
/// </summary>
internal static class Program
{
    internal static bool UseV2Input { get; private set; }
    internal static bool VerifyV2Input { get; private set; }
    internal static bool UseV2Search { get; private set; }
    internal static bool VerifyV2Search { get; private set; }
    internal static string SearchVerificationPath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "v2-search-verification.txt");
    internal static bool UseV2Auth { get; private set; }
    internal static bool VerifyV2Auth { get; private set; }
    internal static string AuthVerificationPath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "v2-auth-verification.txt");
    internal static string VerificationPath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "v2-input-verification.txt");
    internal static bool UseV2Remount { get; private set; }
    internal static bool VerifyV2Remount { get; private set; }
    internal static string RemountVerificationPath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "v2-remount-verification.txt");
    internal static bool UseV2Workspace { get; private set; }
    internal static bool VerifyV2Workspace { get; private set; }
    internal static string WorkspaceVerificationPath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "v2-workspace-verification.txt");
    internal static bool VerifyV2Close { get; private set; }
    internal static string CloseVerificationPath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "v2-close-verification.txt");
    internal static bool VerifyV2Navigation { get; private set; }
    internal static string NavigationVerificationPath { get; private set; } = Path.Combine(AppContext.BaseDirectory, "v2-navigation-verification.txt");
    /// <summary>
    /// 启动应用程序。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    /// <returns>平台宿主或自动验收的退出码。</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        VerifyV2Workspace = args.Contains("--verify-v2-workspace", StringComparer.Ordinal);
        VerifyV2Close = args.Contains("--verify-v2-close", StringComparer.Ordinal);
        VerifyV2Navigation = args.Contains("--verify-v2-navigation", StringComparer.Ordinal);
        UseV2Workspace = VerifyV2Navigation || VerifyV2Close || VerifyV2Workspace || args.Contains("--v2-workspace", StringComparer.Ordinal);
        string? navigationResult = args.FirstOrDefault(static argument => argument.StartsWith("--v2-navigation-result=", StringComparison.Ordinal));
        if (navigationResult is not null) NavigationVerificationPath = Path.GetFullPath(navigationResult["--v2-navigation-result=".Length..]);
        string? closeResult = args.FirstOrDefault(static argument => argument.StartsWith("--v2-close-result=", StringComparison.Ordinal));
        if (closeResult is not null) CloseVerificationPath = Path.GetFullPath(closeResult["--v2-close-result=".Length..]);
        string? workspaceResult = args.FirstOrDefault(static argument => argument.StartsWith("--v2-workspace-result=", StringComparison.Ordinal));
        if (workspaceResult is not null) WorkspaceVerificationPath = Path.GetFullPath(workspaceResult["--v2-workspace-result=".Length..]);
        VerifyV2Remount = args.Contains("--verify-v2-remount", StringComparer.Ordinal);
        UseV2Remount = VerifyV2Remount || args.Contains("--v2-remount", StringComparer.Ordinal);
        string? remountResult = args.FirstOrDefault(static argument => argument.StartsWith("--v2-remount-result=", StringComparison.Ordinal));
        if (remountResult is not null) RemountVerificationPath = Path.GetFullPath(remountResult["--v2-remount-result=".Length..]);
        VerifyV2Input = args.Contains("--verify-v2-input", StringComparer.Ordinal);
        UseV2Input = VerifyV2Input || args.Contains("--v2-input", StringComparer.Ordinal);
        VerifyV2Search = args.Contains("--verify-v2-search", StringComparer.Ordinal);
        UseV2Search = VerifyV2Search || args.Contains("--v2-search", StringComparer.Ordinal);
        string? searchResult = args.FirstOrDefault(static argument => argument.StartsWith("--v2-search-result=", StringComparison.Ordinal));
        if (searchResult is not null)
        {
            SearchVerificationPath = Path.GetFullPath(searchResult["--v2-search-result=".Length..]);
        }

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
