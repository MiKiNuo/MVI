using System;
using System.Diagnostics.CodeAnalysis;
using global::Godot;
using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Platforms.Godot.Binding;
using MiKiNuo.Mvi.Platforms.Godot.Composition;
using MiKiNuo.Mvi.Platforms.Godot.Threading;
using MiKiNuo.Mvi.Samples.Godot.Composition;
using MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

namespace MiKiNuo.Mvi.Samples.Godot.Runtime;

/// <summary>
/// 表示 Godot 游戏大厅示例的运行时启动器。
/// </summary>
public sealed class GameLobbySampleRuntime : IDisposable
{
    private readonly AppCompositionRoot _compositionRoot;
    private readonly IMviGodotBindable<AppShellViewModel>? _appShellBindable;
    private bool _disposed;

    private GameLobbySampleRuntime(
        AppCompositionRoot compositionRoot,
        IMviGodotBindable<AppShellViewModel>? appShellBindable)
    {
        _compositionRoot = compositionRoot;
        _appShellBindable = appShellBindable;
    }

    /// <summary>
    /// 启动 Godot 游戏大厅 MVI 示例。
    /// </summary>
    /// <param name="root">Godot 主场景根节点。</param>
    /// <param name="assignRuntime">用于把运行时生命周期对象交给主场景持有的回调。</param>
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "GodotMviSceneHost.Mount 调用 AddChild 后，节点生命周期由 Godot 场景树接管；异常路径通过 finally 释放未挂载节点。")]
    public static void Start(Control root, Action<IDisposable> assignRuntime)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(assignRuntime);

        Label? errorLabel = null;
        AppCompositionRoot? compositionRoot = null;
        IMviGodotBindable<AppShellViewModel>? appShellBindable = null;
        Control? appShell = null;

        try
        {
            Control appShellSlot = root.GetNode<Control>("AppShellSlot");
            errorLabel = root.GetNode<Label>("ErrorLabel");
            errorLabel.Visible = false;

            GodotMviUiDispatcher uiDispatcher = GodotMviBootstrapper.Install(root);
            compositionRoot = new AppCompositionRoot(uiDispatcher);
            appShell = CreateAppShellView(compositionRoot);
            Control mounted = GodotMviSceneHost.Mount(appShellSlot, appShell);
            appShell = null;

            if (mounted is IMviGodotBindable<AppShellViewModel> bindable)
            {
                appShellBindable = bindable;
                // AppShellView 内部会通过 [MviSlot] 派生 LobbyView 槽位；必须用 2-arg Bind
                // 传入组合根作为 IMviResolver，让源生成器在 OnBindSlots 中 resolve IMviViewRegistry。
                bindable.Bind(compositionRoot.AppShellViewModel, compositionRoot);
            }
            else
            {
                throw new InvalidOperationException("AppShellView 没有实现 IMviGodotBindable<AppShellViewModel>，无法绑定应用壳 ViewModel。");
            }

            GameLobbySampleRuntime? runtime = null;
            try
            {
                runtime = new GameLobbySampleRuntime(compositionRoot, appShellBindable);
                compositionRoot = null;
                appShellBindable = null;
                assignRuntime(runtime);
                runtime = null;
            }
            finally
            {
                runtime?.Dispose();
            }
        }
        catch (Exception exception)
        {
            ShowError(errorLabel, exception);
            appShellBindable?.Unbind();
            compositionRoot?.Dispose();
        }
        finally
        {
            appShell?.Dispose();
        }
    }

    /// <summary>
    /// 释放运行时资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _appShellBindable?.Unbind();
        _compositionRoot.Dispose();
        _disposed = true;
    }

    private static Control CreateAppShellView(AppCompositionRoot compositionRoot)
    {
        ArgumentNullException.ThrowIfNull(compositionRoot);

        Control? registryView = null;
        try
        {
            if (compositionRoot.GodotViewRegistry.TryCreate(GodotViewKeys.AppShell, out registryView) && registryView is not null)
            {
                Control result = registryView;
                registryView = null;
                return result;
            }
        }
        catch (Exception exception)
        {
            GD.PushWarning($"ViewRegistry 加载失败，回退到 PackedScene。{exception.Message}");
        }
        finally
        {
            registryView?.Dispose();
        }

        PackedScene? scene = GD.Load<PackedScene>("res://Views/AppShell/AppShellView.tscn");
        Node? sceneInstance = null;
        try
        {
            sceneInstance = scene?.Instantiate();
            if (sceneInstance is Control control)
            {
                Control result = control;
                sceneInstance = null;
                return result;
            }
        }
        finally
        {
            sceneInstance?.Dispose();
        }

        throw new InvalidOperationException("无法加载 AppShellView.tscn，请检查场景路径。");
    }

    private static void ShowError(Label? errorLabel, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        string message = exception.ToString();
        GD.PushError(message);
        if (errorLabel is not null)
        {
            errorLabel.Text = message;
            errorLabel.Visible = true;
        }
    }
}
