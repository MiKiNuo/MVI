using System.Collections.Concurrent;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Domain.DI;

namespace MiKiNuo.Mvi.Platforms.Godot.Threading;

/// <summary>
/// 表示 Godot 主线程上的 MVI UI 调度器。
/// </summary>
[DiService(ServiceLifetime.Singleton, ServiceType = typeof(IMviUiDispatcher))]
public partial class GodotMviUiDispatcher : Node, IMviUiDispatcher
{
    private readonly ConcurrentQueue<Action> _actions = new();
    private int _drainRequested;

    /// <summary>
    /// 节点进入场景树后初始化。
    /// </summary>
#pragma warning disable CODE0002 // Godot 原生生命周期方法名称固定为 _Ready。
    public override void _Ready()
#pragma warning restore CODE0002
    {
        SetProcess(true);
        DrainPostedActions();
        base._Ready();
    }

    /// <summary>
    /// 每帧处理投递动作。
    /// </summary>
    /// <param name="delta">帧间隔时间（秒）。</param>
#pragma warning disable CODE0002 // Godot 原生生命周期方法名称固定为 _Process。
    public override void _Process(double delta)
#pragma warning restore CODE0002
    {
        DrainPostedActions();
        base._Process(delta);
    }

    /// <summary>
    /// 将操作投递到 UI 线程。
    /// </summary>
    /// <param name="action">需要在 UI 线程上执行的操作。</param>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _actions.Enqueue(action);

        if (!IsInsideTree())
        {
            DrainPostedActions();
            return;
        }

        SetProcess(true);
        RequestDeferredDrain();
    }

    /// <summary>
    /// 执行已经投递到 Godot UI 线程的属性通知动作。
    /// </summary>
    public void DrainPostedActions()
    {
        Interlocked.Exchange(ref _drainRequested, 0);

        while (_actions.TryDequeue(out Action? action))
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                GD.PushError($"Godot MVI UI 调度动作执行失败：{exception}");
            }
        }
    }

    private void RequestDeferredDrain()
    {
        if (Interlocked.Exchange(ref _drainRequested, 1) != 0)
        {
            return;
        }

        // 不能使用 CallDeferred(nameof(DrainPostedActions))。
        // Godot 会尝试查找 Node::DrainPostedActions，导致 C# 自定义方法无法被找到。
        // Callable.From(...) 会直接包装 C# 委托，不依赖 Godot 方法表。
        Callable.From(DrainPostedActions).CallDeferred();
    }
}