using Avalonia;
using System;

namespace MiKiNuo.Mvi.Samples.Avalonia;

/// <summary>
/// Avalonia 桌面程序入口。
/// </summary>
public static class Program
{
    /// <summary>
    /// 启动 Avalonia 示例应用。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 创建 Avalonia 应用构建器。
    /// </summary>
    /// <returns>Avalonia 应用构建器。</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
