using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳副作用分发器。
/// </summary>
public sealed class AppShellEffectDispatcher : IMviEffectDispatcher<AppShellEffect>
{
    private readonly ITraceEffectLogger _traceLogger;

    /// <summary>
    /// 初始化游戏应用壳副作用分发器。
    /// </summary>
    /// <param name="logger">追踪日志记录器。</param>
    public AppShellEffectDispatcher(ITraceEffectLogger logger)
    {
        _traceLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(AppShellEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        cancellationToken.ThrowIfCancellationRequested();
        if (effect is AppShellEffect.Trace trace)
        {
            _traceLogger.Log(trace);
        }

        return ValueTask.CompletedTask;
    }
}
