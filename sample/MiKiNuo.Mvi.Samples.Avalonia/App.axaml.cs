﻿﻿﻿﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;

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
            SampleCompositionRoot compositionRoot = new();
            desktop.MainWindow = compositionRoot.CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
