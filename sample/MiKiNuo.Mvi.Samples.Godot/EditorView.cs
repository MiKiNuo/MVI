using System.ComponentModel;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot;

namespace MiKiNuo.Mvi.Samples.Godot;

/// <summary>只展示本地编辑器状态，并经明确端口向目标实例发出请求。</summary>
public partial class EditorView : PanelContainer
{
    private readonly EditorFeature feature;
    private readonly Mediator mediator;
    private readonly RequestPort<EditorLoad, EditorReply> target;
    private GodotProjection? connection;
    private EditorFeature.Projection? projection;
    private VBoxContainer layout = null!;
    private Label heading = null!;
    private LineEdit input = null!;
    private Button load = null!;
    private Button send = null!;
    private Label status = null!;
    private Label result = null!;

    /// <summary>创建尚未进入树的本地 View；连接将在 Ready 中建立。</summary>
    /// <param name="feature">被展示的独立业务实例。</param>
    /// <param name="mediator">宿主明确建立的通信范围。</param>
    /// <param name="target">宿主选定的业务目标端口。</param>
    public EditorView(EditorFeature feature, Mediator mediator, RequestPort<EditorLoad, EditorReply> target)
    {
        this.feature = feature;
        this.mediator = mediator;
        this.target = target;
        CustomMinimumSize = new(350, 380);
    }

    /// <summary>获取原生输入控件，供真实宿主验收输入解绑。</summary>
    public LineEdit Input => input;
    /// <summary>获取原生本地加载按钮。</summary>
    public Button LoadButton => load;
    /// <summary>获取原生定向通信按钮。</summary>
    public Button SendButton => send;
    /// <summary>获取当前原生状态文本。</summary>
    public string Status => status.Text;
    /// <summary>获取当前原生业务响应。</summary>
    public string Result => result.Text;
    /// <summary>获取当前已展示的版本。</summary>
    public long DisplayedVersion { get; private set; }
    /// <summary>获取本地实际展示次数。</summary>
    public int DisplayCount { get; private set; }
    /// <summary>获取最近一次原生按钮触发的操作。</summary>
    public Task<OperationResult<EditorReply>>? Execution => projection?.LoadCommand.Execution;
    /// <summary>获取最近一次定向通信请求。</summary>
    public Task<RequestResult<EditorReply>>? Request { get; private set; }

    /// <summary>通过原生节点和生成投影建立本地编辑、命令与字段展示。</summary>
    public override void _Ready()
    {
        layout = new();
        AddChild(layout);
        layout.AddThemeConstantOverride("separation", 18);
        heading = new() { Text = "EDITOR " + feature.InstanceId.ToString("N")[..8] };
        heading.AddThemeFontSizeOverride("font_size", 22);
        input = new() { CustomMinimumSize = new(0, 44) };
        load = new() { Text = "Load (slow IO)", CustomMinimumSize = new(0, 44) };
        send = new() { Text = "Send to selected editor", CustomMinimumSize = new(0, 44) };
        status = new() { Text = "Ready" };
        result = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        layout.AddChild(heading);
        layout.AddChild(input);
        layout.AddChild(load);
        layout.AddChild(send);
        layout.AddChild(status);
        layout.AddChild(result);
        connection = new(this);
        projection = connection.Create(feature.CreateProjection);
        connection.BindInput(projection, input, value => value.Text);
        projection.PropertyChanged += Changed;
        projection.LoadCommand.CanExecuteChanged += CommandChanged;
        load.Pressed += LoadPressed;
        send.Pressed += SendPressed;
        Render();
    }

    /// <summary>退出树时同步断开本地输入和命令，保留 Feature 及其执行。</summary>
    public override void _ExitTree()
    {
        if (projection is not null)
        {
            projection.PropertyChanged -= Changed;
            projection.LoadCommand.CanExecuteChanged -= CommandChanged;
        }
        load.Pressed -= LoadPressed;
        send.Pressed -= SendPressed;
        connection?.Dispose();
        connection = null;
        projection = null;
    }

    private void Changed(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(EditorState.Result) or nameof(EditorState.BusinessError) or nameof(EditorFeature.Projection.Snapshot)) Render();
    }

    private void Render()
    {
        if (projection is null) return;
        projection.Snapshot.OperationStates.TryGetValue("Load", out OperationState? operation);
        projection.Snapshot.OperationStates.TryGetValue("HandleLoad", out OperationState? request);
        status.Text = "Load: " + Describe(operation) + "\nTarget request: " + Describe(request);
        result.Text = projection.BusinessError ?? projection.Result;
        DisplayedVersion = projection.Snapshot.Version;
        DisplayCount++;
        CommandChanged(this, EventArgs.Empty);
    }

    private static string Describe(OperationState? operation) => operation?.IsRunning == true ? "Loading..."
        : operation?.LastResult == OperationResultKind.Faulted ? "Fault: " + operation.Exception?.Message
        : operation?.LastResult?.ToString() ?? "Idle";

    private void CommandChanged(object? sender, EventArgs args)
    {
        if (projection is not null) load.Disabled = !projection.LoadCommand.CanExecute(null);
    }
    private void LoadPressed() => projection!.LoadCommand.Execute(null);
    private void SendPressed() => Request = mediator.SendAsync(new EditorLoad(projection!.Text), target);
}
