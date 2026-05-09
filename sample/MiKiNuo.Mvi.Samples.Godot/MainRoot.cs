using System;
using global::Godot;
using MiKiNuo.Mvi.Samples.Godot.Runtime;

/// <summary>
/// 表示 Godot 游戏 MVI 示例主场景根节点。
/// </summary>
[GlobalClass]
public partial class MainRoot : Control
{
    private IDisposable? _runtime;

    /// <inheritdoc />
#pragma warning disable CODE0002 // Godot 原生生命周期方法名称固定为 _Ready。
    public override void _Ready()
#pragma warning restore CODE0002
    {
        base._Ready();
        StartRuntime();
    }

    /// <inheritdoc />
#pragma warning disable CODE0002 // Godot 原生生命周期方法名称固定为 _ExitTree。
    public override void _ExitTree()
#pragma warning restore CODE0002
    {
        _runtime?.Dispose();
        _runtime = null;
        base._ExitTree();
    }

    private void StartRuntime()
    {
        try
        {
            GameLobbySampleRuntime.Start(this, SetRuntime);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void SetRuntime(IDisposable runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
    }

    private void ShowError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        GD.PushError(exception.ToString());

        Label? errorLabel = GetNodeOrNull<Label>("ErrorLabel");
        if (errorLabel is null)
        {
            return;
        }

        errorLabel.Visible = true;
        errorLabel.Text = $"Godot 游戏 MVI 示例启动失败：{exception.GetType().Name}\n{exception.Message}";
    }
}
