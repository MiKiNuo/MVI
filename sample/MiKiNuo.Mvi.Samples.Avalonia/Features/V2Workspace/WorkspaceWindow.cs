using Avalonia.Controls;
using MiKiNuo.Mvi.Platforms.Avalonia;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Workspace;

/// <summary>显式拥有实例树、路由和独立视觉宿主的动态工作区。</summary>
public sealed class WorkspaceWindow : Window, IAsyncDisposable
{
    private readonly List<(WorkspaceEditorFeature Feature, AvaloniaFeatureHost Host)> editors = [];
    private readonly WorkspaceService sharedService = new();
    private readonly AvaloniaFeatureHost detailHost;

    /// <summary>创建多开同业务对象和嵌套完整 Feature 的原生窗口。</summary>
    public WorkspaceWindow()
    {
        Title = "MVI v2 — 独立实例动态工作区";
        Width = 900;
        Height = 650;
        Root = new(Mediator);
        Panels = new StackPanel { Spacing = 12 };
        StackPanel buttons = new() { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 12 };
        Button add = new() { Content = "多开编辑器" };
        Button remove = new() { Content = "移除最后实例" };
        Button coordinate = new() { Content = "明确协调全部编辑器" };
        add.Click += (_, _) => AddEditor();
        remove.Click += (_, _) => { if (editors.Count != 0) RemoveEditor(editors[^1].Feature); };
        coordinate.Click += async (_, _) => await Root.CoordinateAsync(editors.Select(editor => editor.Feature.Load).ToArray());
        buttons.Children.Add(add);
        buttons.Children.Add(remove);
        buttons.Children.Add(coordinate);
        Panels.Children.Add(buttons);
        Content = new ScrollViewer { Content = Panels };
        First = AddEditor();
        Second = AddEditor();
        Detail = new(sharedService);
        First.Children.Add(Detail).Wire(DetailMediator, Detail.Load);
        detailHost = new();
        detailHost.Mount(Detail, static feature => new WorkspaceEditorView(feature));
        Panels.Children.Add(detailHost);
        Closed += async (_, _) =>
        {
            foreach ((WorkspaceEditorFeature _, AvaloniaFeatureHost host) in editors) host.Dispose();
            detailHost.Dispose();
            await DisposeAsync();
        };
    }

    internal Mediator Mediator { get; } = new();
    internal Mediator DetailMediator { get; } = new();
    internal WorkspaceFeature Root { get; }
    internal WorkspaceEditorFeature First { get; }
    internal WorkspaceEditorFeature Second { get; }
    internal WorkspaceEditorFeature Detail { get; }
    internal StackPanel Panels { get; }

    /// <summary>在全部所属实例真实退出后释放窗口手工拥有的共享服务。</summary>
    /// <returns>窗口业务资源完成释放的任务。</returns>
    public async ValueTask DisposeAsync()
    {
        await Root.Close().Ticket.Released;
        await sharedService.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    internal WorkspaceEditorFeature AddEditor()
    {
        WorkspaceEditorFeature feature = new(sharedService);
        Root.Children.Add(feature).Wire(Mediator, feature.Load);
        AvaloniaFeatureHost host = new();
        host.Mount(feature, static instance => new WorkspaceEditorView(instance));
        Panels.Children.Add(host);
        editors.Add((feature, host));
        return feature;
    }

    internal CloseResult RemoveEditor(WorkspaceEditorFeature feature)
    {
        if (ReferenceEquals(feature, First))
        {
            detailHost.Unmount();
            Panels.Children.Remove(detailHost);
            detailHost.Dispose();
        }

        (WorkspaceEditorFeature _, AvaloniaFeatureHost host) = editors.Single(editor => ReferenceEquals(editor.Feature, feature));
        host.Unmount();
        Panels.Children.Remove(host);
        host.Dispose();
        editors.RemoveAll(editor => ReferenceEquals(editor.Feature, feature));
        return Root.Children.Remove(feature);
    }
}

internal sealed class WorkspaceEditorView : UserControl, IDisposable
{
    private readonly WorkspaceEditorFeature.Projection projection;
    private readonly IDisposable input;

    internal WorkspaceEditorView(WorkspaceEditorFeature feature)
    {
        projection = AvaloniaProjection.Create<WorkspaceEditorFeature.Projection>(feature.CreateProjection);
        TextBox text = new();
        input = AvaloniaProjection.BindInput(projection, text, TextBox.TextProperty, static view => view.Text);
        StackPanel panel = new() { Margin = new global::Avalonia.Thickness(16), Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = $"业务对象 {feature.Snapshot.State.ObjectId} / 实例 {feature.InstanceId}" });
        panel.Children.Add(text);
        Content = panel;
    }

    /// <summary>释放本 View 的原生输入和本地投影。</summary>
    public void Dispose() { input.Dispose(); projection.Dispose(); }
}
