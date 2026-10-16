using System.Collections.Concurrent;
using global::Godot;
using MiKiNuo.Mvi.Binding.Threading;
using MiKiNuo.Mvi.Abstractions.DI;

namespace MiKiNuo.Mvi.Platforms.Godot.Threading;

/// <summary>表示 Godot 主线程上的 MVI UI 调度器。</summary>
[DiService(ServiceLifetime.Singleton, ServiceType = typeof(IMviUiDispatcher))]
public partial class GodotMviUiDispatcher : Node, IMviUiDispatcher
{
    private readonly ConcurrentQueue<Action> _actions = new();
    private int _sceneThreadId;
    private bool _draining;

    /// <summary>进入场景树时启用主线程处理，支持节点重新挂载。</summary>
#pragma warning disable CODE0002 // Godot 固定生命周期方法名。
    public override void _EnterTree()
#pragma warning restore CODE0002
    {
        base._EnterTree();
        ProcessThreadGroup = ProcessThreadGroupEnum.MainThread;
        Volatile.Write(ref _sceneThreadId, System.Environment.CurrentManagedThreadId);
        SetProcess(true);
    }

    /// <summary>首次就绪时执行挂载前积累的动作。</summary>
#pragma warning disable CODE0002 // Godot 固定生命周期方法名。
    public override void _Ready()
#pragma warning restore CODE0002
    {
        DrainPostedActions();
        base._Ready();
    }

    /// <summary>每帧处理投递动作。</summary>
    /// <param name="delta">帧间隔时间（秒）。</param>
#pragma warning disable CODE0002 // Godot 固定生命周期方法名。
    public override void _Process(double delta)
#pragma warning restore CODE0002
    {
        DrainPostedActions();
        base._Process(delta);
    }

    /// <summary>离树时暂停排空，保留待执行动作直到重新挂载。</summary>
#pragma warning disable CODE0002 // Godot 固定生命周期方法名。
    public override void _ExitTree()
#pragma warning restore CODE0002
    {
        Volatile.Write(ref _sceneThreadId, 0);
        base._ExitTree();
    }

    /// <summary>将操作排队，由挂载后的 Godot 主线程执行。</summary>
    /// <param name="action">需要在 UI 线程上执行的操作。</param>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Enqueue(action);
    }

    /// <summary>仅在已挂载的场景主线程排空；其他线程调用不执行动作。</summary>
    public void DrainPostedActions()
    {
        int currentThreadId = System.Environment.CurrentManagedThreadId;
        if (Volatile.Read(ref _sceneThreadId) != currentThreadId || _draining)
            return;

        _draining = true;
        try
        {
            while (Volatile.Read(ref _sceneThreadId) == currentThreadId && _actions.TryDequeue(out Action? action))
            {
                try { action(); }
                catch (Exception exception) { GD.PushError($"Godot MVI UI 调度动作执行失败：{exception}"); }
            }
        }
        finally { _draining = false; }
    }
}
