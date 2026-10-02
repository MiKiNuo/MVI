using Avalonia.Controls;
using MiKiNuo.Mvi.Platforms.Avalonia;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

/// <summary>由窗口持有业务实例，演示只卸载与重新连接登录 View。</summary>
public sealed class RemountWindow : Window
{
    private readonly LoginFeature feature;

    /// <summary>创建保留业务实例的重挂载演示窗口。</summary>
    public RemountWindow()
    {
        feature = new(new VerificationAuthService());
        Title = "MVI v2 — 卸载 View，保留 Feature";
        Width = 640;
        Height = 640;
        Surface = new StackPanel();
        Host = new AvaloniaFeatureHost();
        StackPanel buttons = new() { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 12 };
        Button mount = new() { Content = "重挂载" };
        Button unmount = new() { Content = "卸载 View" };
        mount.Click += (_, _) => Host.Mount(feature, static instance => new LoginForm(instance));
        unmount.Click += (_, _) => Host.Unmount();
        buttons.Children.Add(mount);
        buttons.Children.Add(unmount);
        Surface.Children.Add(buttons);
        Surface.Children.Add(Host);
        Content = Surface;
        Host.Mount(feature, static instance => new LoginForm(instance));
        Closed += (_, _) => { Host.Dispose(); feature.Close(); };
    }

    internal AvaloniaFeatureHost Host { get; }
    internal StackPanel Surface { get; }
}
