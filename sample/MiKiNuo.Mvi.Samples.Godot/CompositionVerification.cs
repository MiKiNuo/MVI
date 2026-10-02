using System.Text.Json;
using global::Godot;
using MiKiNuo.Mvi.Platforms.Godot;

namespace MiKiNuo.Mvi.Samples.Godot;

public partial class CompositionView
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    private static void Click(Button button)
    {
        Vector2 point = button.GetGlobalRect().GetCenter();
        using InputEventMouseButton pressed = new() { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true };
        using InputEventMouseButton released = new() { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false };
        global::Godot.Input.ParseInputEvent(pressed);
        global::Godot.Input.ParseInputEvent(released);
    }

    private async Task VerifyOperationsAsync(Dictionary<string, object?> results)
    {
        first.SetText("slow success");
        await Frames();
        Click(firstView.LoadButton);
        await Frames();
        Require(firstView.Status.Contains("Load: Loading...", StringComparison.Ordinal), "Native local operation must show Loading while IO is pending.");
        first.SetText("edited during IO");
        OperationResult<EditorReply> completed = await firstView.Execution!.WaitAsync(Watchdog);
        Require(completed.Kind == OperationResultKind.Completed && completed.Value!.Success && first.Snapshot.State.Text == "edited during IO", "Slow IO must preserve new edits and commit its typed result.");
        await Frames();
        Require(firstView.Result == "Loaded: slow success", "Committed business result must reach the native View.");
        first.SetText("business-fail");
        OperationResult<EditorReply> business = await first.Load().WaitAsync(Watchdog);
        await Frames();
        Require(business.Kind == OperationResultKind.Completed && !business.Value!.Success && firstView.Result == "Business request declined", "Business failure must remain Completed with a visible business reason.");
        first.SetText("fault");
        OperationResult<EditorReply> fault = await first.Load().WaitAsync(Watchdog);
        await Frames();
        Require(fault.Kind == OperationResultKind.Faulted && firstView.Status.Contains("Load: Fault:", StringComparison.Ordinal), "Unexpected service fault must have a visible structured runtime result.");

        string unchanged = first.Snapshot.State.Result;
        first.SetText("selected target");
        await Frames();
        Click(firstView.SendButton);
        await Frames();
        Require(secondView.Status.Contains("Target request: Loading...", StringComparison.Ordinal), "Directed slow IO must show Loading on the selected target.");
        RequestResult<EditorReply> directed = await firstView.Request!.WaitAsync(Watchdog);
        await Frames();
        Require(directed.OperationResult?.Value?.Success == true && secondView.Result == "Loaded: selected target" && first.Snapshot.State.Result == unchanged, "Directed communication must update only the selected independent instance.");
        first.SetText("fault");
        await Frames();
        Click(firstView.SendButton);
        await Frames();
        RequestResult<EditorReply> directedFault = await firstView.Request!.WaitAsync(Watchdog);
        await Frames();
        Require(directedFault.OperationResult?.Kind == OperationResultKind.Faulted && secondView.Status.Contains("Target request: Fault:", StringComparison.Ordinal), "Directed service fault must be visible on the target View.");
        Require((await mediator.SendAsync<EditorLoad, EditorReply>(new("ambiguous"))).Kind == RequestResultKind.AmbiguousTarget, "Multiple contract providers must not silently select a target.");

        Func<string, CancellationToken, Task<EditorReply>> original = first.Service.Run;
        first.Service.Run = static (_, _) => Task.FromResult(new EditorReply(true, "completed without drawing"));
        long displayed = firstView.DisplayedVersion;
        OperationResult<EditorReply> noDraw = first.Load().WaitAsync(Watchdog).GetAwaiter().GetResult();
        Require(noDraw.Kind == OperationResultKind.Completed && first.Snapshot.State.Result == "completed without drawing"
            && firstView.DisplayedVersion == displayed, "Operation completion must commit state while the UI thread has not drawn another frame.");
        first.Service.Run = original;
        await Frames();
        Require(firstView.Result == "completed without drawing", "A later frame must present the already-completed operation.");
        results["operationResults"] = new[] { completed.Kind.ToString(), business.Kind + "/BusinessFailure", fault.Kind.ToString(), directed.Kind.ToString(), directedFault.OperationResult!.Kind.ToString() };
        results["directedLoadingAndFault"] = true;
        results["completionBeforeRender"] = true;
    }

    private async Task VerifyMountLifecycleAsync(Dictionary<string, object?> results)
    {
        Node editors = GetNode<Node>("Margin/Content/Editors");
        Node slot = GetNode<Node>("Margin/Content/Editors/First");
        Node decoration = slot.GetNode<Node>("Unrelated");
        EditorView old = firstView;
        int oldDisplays = old.DisplayCount;
        first.SetText("queued before unmount");
        firstHost!.Unmount();
        old.Input.EmitSignal(LineEdit.SignalName.TextChanged, "stale input");
        Require(first.Snapshot.State.Text == "queued before unmount" && !first.IsClosed, "Unmount must invalidate native input while retaining the independent Feature.");
        first.SetText("unmounted latest");
        RequestResult<EditorReply> hidden = await mediator.SendAsync(new EditorLoad("hidden request"), first.LoadPort).WaitAsync(Watchdog);
        Require(hidden.OperationResult?.Kind == OperationResultKind.Completed && old.DisplayCount == oldDisplays, "Detached instances must handle messages without running old View callbacks.");
        firstHost.Mount(first, CreateFirst);
        await Frames(3);
        Require(firstView.Input.Text == "unmounted latest" && firstView.Result == "Loaded: hidden request" && decoration.GetParent() == slot, "Remount must start from current state and retain unrelated nodes.");
        editors.RemoveChild(slot);
        Require(decoration.GetParent() == slot && slot.GetChildCount() == 1, "Container exit must remove only its owned View.");
        first.SetText("latest while outside tree");
        editors.AddChild(slot);
        await Frames(3);
        Require(firstView.Input.Text == "latest while outside tree" && firstView.GetParent() == slot && slot.GetChildCount() == 2, "Container reentry must recreate exactly one latest-state projection.");

        EditorFeature previous = first;
        RequestPort<EditorLoad, EditorReply> previousPort = previous.LoadPort;
        await ReplaceFirstAsync();
        await Frames(3);
        Require(previous.IsClosed && previous.Service.DisposeCount == 1 && first.InstanceId != previous.InstanceId, "Replacement must retire the old business instance and create an isolated scoped instance.");
        Require((await mediator.SendAsync(new EditorLoad("old address"), previousPort)).Kind == RequestResultKind.TargetUnavailable, "Retired addresses must reject new requests.");
        Require(decoration.GetParent() == slot && root.Children.Active.Count == 2 && second.Service.DisposeCount == 0, "Replacement must preserve unrelated nodes and the other active instance.");
        results["unmountRemount"] = true;
        results["containerExitReentry"] = true;
        results["replacement"] = true;
        results["ownedNodesOnly"] = true;
    }

    private async Task VerifyInFlightCloseAsync(Dictionary<string, object?> results, string resultPath)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<EditorReply> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        first.Service.Run = async (_, _) => { entered.SetResult(); return await release.Task.ConfigureAwait(false); };
        first.SetText("uncooperative IO");
        Task<OperationResult<EditorReply>> running = first.Load();
        try
        {
            await entered.Task.WaitAsync(Watchdog);
            await Frames();
            Require(firstView.Status.Contains("Load: Loading...", StringComparison.Ordinal), "Uncooperative IO must remain visibly running.");
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string screenshot = Path.ChangeExtension(resultPath, ".png");
            using Image image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(screenshot) == Error.Ok, "The real composition viewport must be saved after drawing.");
            results["screenshot"] = screenshot;
            string stateBeforeClose = first.Snapshot.State.Result;
            CloseResult childClose = firstMember.Remove();
            firstHost!.Unmount();
            Require(first.IsClosed && root.Children.Active.Count == 1 && root.Children.Retired.Count != 0
                && !childClose.Ticket.Released.IsCompleted && first.Service.DisposeCount == 0, "Logical child close must retain resources until its real IO exits.");
            Require((await first.Load()).Kind == OperationResultKind.Rejected, "Closed instances must reject new operations.");
            bool refusedInput = false;
            try { first.SetText("too late"); } catch (FeatureClosedException) { refusedInput = true; }
            Require(refusedInput && (await mediator.SendAsync(new EditorLoad("closed"), first.LoadPort)).Kind == RequestResultKind.TargetUnavailable, "Closed instances must reject both inputs and directed messages.");
            CloseResult parentClose = root.Close();
            secondHost!.Unmount();
            Require(!parentClose.Ticket.Released.IsCompleted && first.Service.DisposeCount == 0, "Parent release must continue tracking the retired uncooperative child.");
            release.SetResult(new(true, "late result"));
            OperationResult<EditorReply> canceled = await running.WaitAsync(Watchdog);
            ReleaseResult released = await parentClose.Ticket.Released.WaitAsync(Watchdog);
            Require(canceled.Kind == OperationResultKind.Canceled && first.Snapshot.State.Result == stateBeforeClose, "Late feedback must be rejected after logical close.");
            Require(released.Succeeded && first.Service.TailUsed && first.Service.DisposeCount == 1 && second.Service.DisposeCount == 1, "Scopes must release exactly once after the last real resource user exits.");
            results["inFlightClose"] = true;
            results["lateFeedbackRejected"] = true;
            results["releasedAfterRealExit"] = true;
        }
        finally
        {
            release.TrySetResult(new(true, "cleanup"));
            await running.WaitAsync(Watchdog);
            await root.Close().Ticket.Released.WaitAsync(Watchdog);
        }
    }

    private async Task Frames(int count = 2)
    {
        for (int index = 0; index < count; index++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task VerifyReadyMountAsync()
    {
        using VBoxContainer parent = new();
        using Label decoration = new() { Text = "unrelated decoration" };
        parent.AddChild(decoration);
        AddChild(parent);
        RemoveChild(parent);
        await using EditorService service = new();
        EditorFeature feature = new(service);
        using GodotFeatureHost host = new(parent);
        EditorView? mounted = null;
        int creations = 0;
        // 新 child 的 Ready 在父容器遍历孩子期间触发，不能直接向仍被 block 的父容器 AddChild。
        using Label readyChild = new();
        parent.AddChild(readyChild);
        readyChild.Ready += () => host.Mount(feature, instance =>
        {
            creations++;
            return mounted = new(instance, mediator, second.LoadPort);
        });
        AddChild(parent);
        try
        {
            await Frames(3);
            Require(creations == 1 && mounted?.GetParent() == parent, "Mount from child Ready must create exactly one correctly parented View after native setup.");
            Require(decoration.GetParent() == parent && parent.GetChildCount() == 3, "Ready mount must retain unrelated children.");
            feature.SetText("ready child input");
            await Frames();
            Require(mounted!.Input.Text == "ready child input", "Deferred Ready mount must create a usable latest-state projection.");
        }
        finally
        {
            host.Unmount();
            parent.Free();
            await feature.Close().Ticket.Released;
        }
    }

    private async Task VerifyAsync()
    {
        string[] args = OS.GetCmdlineUserArgs();
        string path = args.FirstOrDefault(value => value.StartsWith("--result-path=", StringComparison.Ordinal))?[14..]
            ?? ProjectSettings.GlobalizePath("user://composition-result.json");
        Dictionary<string, object?> results = new();
        int exitCode = 1;
        try
        {
            await Frames();
            Require(first.InstanceId != second.InstanceId && first.Snapshot.State.ObjectId == second.Snapshot.State.ObjectId, "Same-type same-object instances must be independent.");
            Require(firstView.IsInsideTree() && secondView.IsInsideTree(), "Both native editor Views must be mounted.");
            int created = createdViews;
            firstHost!.Mount(first, CreateFirst);
            Require(createdViews == created, "Stable Mount must reuse the native View.");
            await VerifyReadyMountAsync();
            results["readyMount"] = true;
            if (args.Contains("--composition-self-test", StringComparer.Ordinal))
            {
                await VerifyOperationsAsync(results);
                await VerifyMountLifecycleAsync(results);
                await VerifyInFlightCloseAsync(results, path);
            }
            results["passed"] = true;
            results["createdViews"] = createdViews;
            GD.Print("Godot composition PASS: isolated scoped editors, native operations, directed IO, stable/Ready mounting, unmount/reentry/replacement and real in-flight release.");
            exitCode = 0;
        }
        catch (Exception failure)
        {
            results["passed"] = false;
            results["exception"] = failure.ToString();
            GD.PushError(failure.ToString());
        }
        finally
        {
            await ReleaseAsync();
            File.WriteAllText(path, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            GetTree().Quit(exitCode);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
