using global::Godot;
using Microsoft.Extensions.DependencyInjection;
using MiKiNuo.Mvi.Platforms.Godot;

namespace MiKiNuo.Mvi.Samples.Godot;

/// <summary>以 Core 所有权、Mediator 和标准服务范围组织真实 Godot 子 View。</summary>
public partial class CompositionView : Control
{
    private readonly CompositionFeature root = new();
    private readonly Mediator mediator = new();
    private ServiceProvider? services;
    private GodotFeatureHost? firstHost;
    private GodotFeatureHost? secondHost;
    private EditorFeature first = null!;
    private EditorFeature second = null!;
    private EditorView firstView = null!;
    private EditorView secondView = null!;
    private int createdViews;
    private FeatureMember firstMember = null!;

    /// <summary>在主线程创建独立子实例、建立业务契约接线并挂载 View。</summary>
    public override void _Ready() => _ = StartAsync();

    /// <summary>退出树时只释放本地宿主，业务资源继续通过 Core 关闭票据回收。</summary>
    public override void _ExitTree()
    {
        firstHost?.Dispose();
        secondHost?.Dispose();
        _ = ReleaseAsync();
    }

    private async Task StartAsync()
    {
        ServiceCollection registrations = new();
        registrations.AddScoped<EditorService>();
        services = registrations.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        first = await EditorFeature.CreateAsync(services);
        second = await EditorFeature.CreateAsync(services);
        firstMember = root.Children.Add(first);
        firstMember.Wire(mediator, first.LoadPort);
        root.Children.Add(second).Wire(mediator, second.LoadPort);
        firstHost = new(GetNode<Node>("Margin/Content/Editors/First"));
        secondHost = new(GetNode<Node>("Margin/Content/Editors/Second"));
        firstHost.Mount(first, CreateFirst);
        secondHost.Mount(second, CreateSecond);
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += () => _ = QuitAsync();
        GetNode<Button>("Margin/Content/Controls/Replace").Pressed += () => _ = ReplaceFirstAsync();
        GetNode<Button>("Margin/Content/Controls/Hide").Pressed += () => firstHost.Unmount();
        GetNode<Button>("Margin/Content/Controls/Show").Pressed += () => firstHost.Mount(first, CreateFirst);
        string[] args = OS.GetCmdlineUserArgs();
        if (args.Contains("--composition-host-test", StringComparer.Ordinal) || args.Contains("--composition-self-test", StringComparer.Ordinal)) await VerifyAsync();
    }

    private EditorView CreateFirst(EditorFeature feature) { createdViews++; return firstView = new(feature, mediator, second.LoadPort); }
    private EditorView CreateSecond(EditorFeature feature) { createdViews++; return secondView = new(feature, mediator, first.LoadPort); }

    private async Task ReplaceFirstAsync()
    {
        if (services is null || root.IsClosed) return;
        EditorFeature replacement = await EditorFeature.CreateAsync(services);
        FeatureMember member = root.Children.Add(replacement);
        member.Wire(mediator, replacement.LoadPort);
        firstMember.Remove();
        first = replacement;
        firstMember = member;
        firstHost!.Mount(first, CreateFirst);
        secondHost!.Unmount();
        secondHost.Mount(second, CreateSecond);
    }

    private async Task ReleaseAsync()
    {
        await root.Close().Ticket.Released.ConfigureAwait(false);
        if (services is not null) await services.DisposeAsync().ConfigureAwait(false);
    }

    private async Task QuitAsync()
    {
        root.Close();
        firstHost?.Unmount();
        secondHost?.Unmount();
        await ReleaseAsync();
        GetTree().Quit();
    }
}
