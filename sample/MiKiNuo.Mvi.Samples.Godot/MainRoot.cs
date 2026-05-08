using System;
using System.Reflection;
using global::Godot;

/// <summary>
/// 表示 Godot 游戏 MVI 示例主场景根节点。
/// </summary>
[GlobalClass]
public partial class MainRoot : Control
{
    private const string RuntimeTypeName = "MiKiNuo.Mvi.Samples.Godot.Runtime.GameLobbySampleRuntime";
    private const string StartMethodName = "Start";

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
#pragma warning disable CA1031 // 根节点兜底捕获启动异常，并显示到 Godot 界面上，避免只看到 C# 脚本类加载失败。
        try
        {
            Type runtimeType = typeof(MainRoot).Assembly.GetType(RuntimeTypeName, throwOnError: true)!;
            MethodInfo startMethod = runtimeType.GetMethod(
                StartMethodName,
                BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("没有找到游戏大厅示例运行时启动方法。");

            startMethod.Invoke(null, new object[] { this, (Action<IDisposable>)SetRuntime });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ShowError(exception.InnerException);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
#pragma warning restore CA1031
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
