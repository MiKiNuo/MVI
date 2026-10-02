using System.ComponentModel;
using System.Text.Json;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot;

namespace MiKiNuo.Mvi.Samples.Godot;

/// <summary>使用原生控件和本地生成投影展示 HUD，并提供真实引擎自检入口。</summary>
public partial class HudView : Control
{
    private readonly HudFeature feature = new();
    private readonly List<long> versions = [];
    private readonly Dictionary<string, int> notifications = new(StringComparer.Ordinal);
    private GodotProjection? connection;
    private HudFeature.Projection? projection;
    private LineEdit nameEdit = null!;
    private Label playerLabel = null!;
    private Label scoreLabel = null!;
    private Label actionsLabel = null!;
    private ProgressBar progress = null!;
    private Button claim = null!;
    private int mainThread;
    private int nameRefreshes;
    private int scoreRefreshes;
    private int pressedEvents;

    /// <summary>进入真实场景后连接生成投影与原生 HUD 控件。</summary>
    public override void _Ready()
    {
        mainThread = System.Environment.CurrentManagedThreadId;
        nameEdit = GetNode<LineEdit>("Panel/Content/Name");
        playerLabel = GetNode<Label>("Panel/Content/Player");
        scoreLabel = GetNode<Label>("Panel/Content/Score");
        actionsLabel = GetNode<Label>("Panel/Content/Actions");
        progress = GetNode<ProgressBar>("Panel/Content/Progress");
        claim = GetNode<Button>("Panel/Content/Claim");
        AttachView();
        if (OS.GetCmdlineUserArgs().Contains("--self-test", StringComparer.Ordinal)) _ = SelfTestAsync();
    }

    /// <summary>退出场景时释放本地输入、字段事件和排队展示连接。</summary>
    public override void _ExitTree() => DetachView();

    private void AttachView()
    {
        connection = new(this);
        projection = connection.Create(feature.CreateProjection);
        connection.BindInput(projection, nameEdit, value => value.PlayerName);
        projection.PropertyChanged += OnProjectionChanged;
        projection.ClaimCommand.CanExecuteChanged += OnCommandChanged;
        claim.Pressed += OnPressed;
        UpdateName();
        UpdateScore();
        UpdateActions();
        OnCommandChanged(this, EventArgs.Empty);
        ObserveVersion();
    }

    private void DetachView()
    {
        if (projection is not null)
        {
            projection.PropertyChanged -= OnProjectionChanged;
            projection.ClaimCommand.CanExecuteChanged -= OnCommandChanged;
        }
        claim.Pressed -= OnPressed;
        connection?.Dispose();
        connection = null;
        projection = null;
    }

    private void OnProjectionChanged(object? sender, PropertyChangedEventArgs args)
    {
        Require(System.Environment.CurrentManagedThreadId == mainThread, "PropertyChanged must run on the Godot main thread.");
        string name = args.PropertyName ?? string.Empty;
        notifications[name] = notifications.GetValueOrDefault(name) + 1;
        switch (name)
        {
            case nameof(HudState.PlayerName): UpdateName(); break;
            case nameof(HudState.Score): UpdateScore(); break;
            case nameof(HudState.Actions): UpdateActions(); break;
            case nameof(HudFeature.Projection.Snapshot): ObserveVersion(); break;
        }
    }

    private void UpdateName()
    {
        VerifyUiThread();
        playerLabel.Text = "Player: " + projection!.PlayerName;
        nameRefreshes++;
    }

    private void UpdateScore()
    {
        VerifyUiThread();
        scoreLabel.Text = "Score: " + projection!.Score;
        progress.Value = projection.Score;
        scoreRefreshes++;
    }

    private void UpdateActions()
    {
        VerifyUiThread();
        actionsLabel.Text = "Rewards claimed: " + projection!.Actions;
    }

    private void OnCommandChanged(object? sender, EventArgs args)
    {
        VerifyUiThread();
        claim.Disabled = !projection!.ClaimCommand.CanExecute(null);
    }

    private void OnPressed()
    {
        pressedEvents++;
        projection!.ClaimCommand.Execute(null);
    }

    private void ObserveVersion()
    {
        long version = projection!.Snapshot.Version;
        Require(versions.Count == 0 || version >= versions[^1], "Displayed versions must never go backwards.");
        versions.Add(version);
    }

    private void VerifyUiThread() => Require(System.Environment.CurrentManagedThreadId == mainThread, "Controls must update on the Godot main thread.");

    private async Task Frames(int count = 2)
    {
        for (int frame = 0; frame < count; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void TypeLetter(LineEdit target, Key key, char letter)
    {
        target.GrabFocus();
        target.SelectAll();
        using InputEventKey pressed = new() { Keycode = key, Unicode = letter, Pressed = true };
        using InputEventKey released = new() { Keycode = key, Unicode = letter, Pressed = false };
        Input.ParseInputEvent(pressed);
        Input.ParseInputEvent(released);
    }

    private async Task CheckReentrantBindingAsync(string mode)
    {
        using Control probeView = new();
        using LineEdit target = new() { Position = new(20, 635), Size = new(300, 40) };
        AddChild(probeView);
        probeView.AddChild(target);
        using GodotProjection local = new(probeView);
        HudFeature probeFeature = new();
        HudFeature.Projection source = local.Create(probeFeature.CreateProjection);
        IDisposable? binding = null;
        bool tailCalled = false;
        PropertyChangedEventHandler earlySource = (_, args) =>
        {
            if (args.PropertyName != nameof(HudState.PlayerName)) return;
            if (mode == "pc-dispose") binding!.Dispose();
            if (mode == "pc-free") probeView.Free();
        };
        LineEdit.TextChangedEventHandler earlyInput = _ => { if (mode == "input-dispose") binding!.Dispose(); };
        source.PropertyChanged += earlySource;
        target.TextChanged += earlyInput;
        binding = local.BindInput(source, target, value => value.PlayerName);
        source.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(HudState.PlayerName)) tailCalled = true; };
        try
        {
            if (mode == "input-dispose") TypeLetter(target, Key.X, 'x');
            else probeFeature.SetPlayerName("changed");
            await Frames();
            if (mode == "input-dispose")
                Require(probeFeature.Snapshot.State.PlayerName == "PILOT", "An input callback captured before disposal must not write state later in the same native signal.");
            else if (mode == "pc-free")
                Require(!GodotObject.IsInstanceValid(target) && tailCalled, "A captured source callback must skip the freed target and allow the remaining multicast handlers to finish.");
            else
                Require(target.Text == "PILOT" && tailCalled, "A captured source callback must not update a connection disposed by an earlier PropertyChanged handler.");
        }
        finally
        {
            source.PropertyChanged -= earlySource;
            if (GodotObject.IsInstanceValid(target)) target.TextChanged -= earlyInput;
            if (GodotObject.IsInstanceValid(probeView)) probeView.Free();
        }
    }

    private async Task SelfTestAsync()
    {
        string[] args = OS.GetCmdlineUserArgs();
        string resultPath = args.FirstOrDefault(value => value.StartsWith("--result-path=", StringComparison.Ordinal))?[14..]
            ?? ProjectSettings.GlobalizePath("user://hud-result.json");
        Dictionary<string, object?> result = new();
        int exitCode = 1;
        try
        {
            await Frames();
            TypeLetter(nameEdit, Key.R, 'r');
            await Frames();
            Require(nameEdit.HasFocus() && feature.Snapshot.State.PlayerName == "R" && nameEdit.Text == "R", "Native LineEdit input must commit and echo its normalized value.");
            TypeLetter(nameEdit, Key.R, 'r');
            await Frames();
            Require(nameEdit.Text == "R" && feature.Snapshot.State.PlayerName == "R", "Same-value normalization must correct the native edit.");

            int namesBefore = nameRefreshes;
            int scoresBefore = scoreRefreshes;
            feature.SetTelemetry(9);
            await Frames();
            Require(nameRefreshes == namesBefore && scoreRefreshes == scoresBefore, "Unrelated telemetry must not refresh player or score controls.");
            int workerThread = 0;
            long beforeVersion = feature.Snapshot.Version;
            int beforeDisplays = versions.Count;
            // 自检暂不让主线程进入下一帧，确保这 1000 次后台提交属于同一等待展示批次。
            Task.Run(() =>
            {
                workerThread = System.Environment.CurrentManagedThreadId;
                for (int index = 0; index < 1000; index++) feature.SetDelta(1);
            }).GetAwaiter().GetResult();
            Require(workerThread != mainThread && feature.Snapshot.Version == beforeVersion + 1000, "Background input must commit each business transition.");
            Require(feature.Snapshot.State.AcceptedInputs == 1000 && projection!.Snapshot.Version == beforeVersion, "Display coalescing must not discard business input or update controls on the worker.");
            await Frames();
            int burstDisplays = versions.Count - beforeDisplays;
            Require(burstDisplays == 1 && scoreRefreshes == scoresBefore + 1 && nameRefreshes == namesBefore, "The waiting burst must display its latest score once, without refreshing name.");
            Require(projection!.Score == 1000 && progress.Value == 1000, "The displayed score must come from the committed snapshot.");

            Vector2 point = claim.GetGlobalRect().GetCenter();
            using InputEventMouseButton pressed = new() { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true };
            using InputEventMouseButton released = new() { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false };
            Input.ParseInputEvent(pressed);
            Input.ParseInputEvent(released);
            await Frames();
            Require(pressedEvents == 1 && projection.ClaimCommand.Execution is not null, "Native Button input must invoke one generated operation.");
            OperationResult<int> claimed = await projection.ClaimCommand.Execution!;
            Require(claimed.Kind == OperationResultKind.Completed && feature.Snapshot.State.Actions == 1, "One-shot business behavior must survive display coalescing.");
            await Frames();
            Require(projection.Actions == 1 && actionsLabel.Text == "Rewards claimed: 1", "The completed operation must reach the native action field on a later display frame.");

            int oldRefreshes = scoreRefreshes;
            feature.SetDelta(2);
            DetachView();
            Task.Run(() => feature.SetDelta(3)).GetAwaiter().GetResult();
            await Frames();
            Require(scoreRefreshes == oldRefreshes && scoreLabel.Text == "Score: 1000", "Disposed connections and queued old callbacks must not update the controls.");
            TypeLetter(nameEdit, Key.S, 's');
            await Frames();
            Require(feature.Snapshot.State.PlayerName == "R", "Detached native input must not write business state.");
            AttachView();
            Require(projection!.Score == 1005 && nameEdit.Text == "R" && scoreLabel.Text == "Score: 1005", "A new local connection must start with the latest snapshot.");

            await CheckReentrantBindingAsync("pc-dispose");
            await CheckReentrantBindingAsync("pc-free");
            await CheckReentrantBindingAsync("input-dispose");
            nameEdit.GrabFocus();

            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string screenshot = Path.ChangeExtension(resultPath, ".png");
            using Image image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(screenshot) == Error.Ok, "The rendered viewport screenshot must be saved.");
            result["passed"] = true;
            result["inputSource"] = "Input.ParseInputEvent: focused LineEdit keys and native Button mouse events";
            result["mainThread"] = mainThread;
            result["workerThread"] = workerThread;
            result["burstInputs"] = 1000;
            result["burstDisplays"] = burstDisplays;
            result["finalAcceptedInputs"] = feature.Snapshot.State.AcceptedInputs;
            result["finalScore"] = feature.Snapshot.State.Score;
            result["buttonEvents"] = pressedEvents;
            result["actions"] = feature.Snapshot.State.Actions;
            result["reentrantPropertyChangedDispose"] = true;
            result["reentrantViewFree"] = true;
            result["reentrantNativeInputDispose"] = true;
            result["displayedVersions"] = versions;
            result["fieldNotifications"] = notifications;
            result["screenshot"] = screenshot;
            GD.Print("Godot HUD PASS: native input, main-thread field projection, 1000 committed inputs / 1 waiting display, one-shot operation and local detach/attach.");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            result["passed"] = false;
            result["exception"] = exception.ToString();
            GD.PushError(exception.ToString());
        }
        finally
        {
            File.WriteAllText(resultPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            GetTree().Quit(exitCode);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
