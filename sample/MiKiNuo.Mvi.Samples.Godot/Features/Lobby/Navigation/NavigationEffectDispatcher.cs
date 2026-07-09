using System;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示大厅导航副作用分发器。
/// </summary>
public sealed class NavigationEffectDispatcher : MviEffectDispatcherBase<NavigationEffect>
{
    private readonly IMviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> _activityLogStore;
    private readonly ITraceEffectLogger _traceLogger;
    private IGameShellNavigator? _navigator;

    /// <summary>初始化导航副作用分发器。</summary>
    /// <param name="activityLogStore">活动日志状态存储。</param>
    /// <param name="traceLogger">追踪日志记录器。</param>
    public NavigationEffectDispatcher(
        IMviStore<ActivityLogState, ActivityLogIntent, ActivityLogEffect> activityLogStore,
        ITraceEffectLogger traceLogger)
    {
        ArgumentNullException.ThrowIfNull(activityLogStore);
        _activityLogStore = activityLogStore;
        ArgumentNullException.ThrowIfNull(traceLogger);
        _traceLogger = traceLogger;
    }

    /// <summary>设置游戏壳导航协调器。</summary>
    /// <param name="navigator">游戏壳导航协调器。</param>
    public void SetNavigator(IGameShellNavigator navigator)
    {
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    /// <summary>分发副作用。</summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    protected override async ValueTask DispatchCoreAsync(NavigationEffect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case NavigationEffect.Trace trace:
                _traceLogger.Log(trace);
                break;
            case NavigationEffect.LogActivity data:
                await _activityLogStore
                    .DispatchAsync(new ActivityLogIntent.AppendEntry(data.Message), cancellationToken)
                    .ConfigureAwait(false);
                break;
            case NavigationEffect.LogoutRequested:
                await HandleLogoutRequestedAsync(cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async ValueTask HandleLogoutRequestedAsync(CancellationToken cancellationToken)
    {
        GD.Print("[Navigation Effect] ReturnToLogin");
        if (_navigator is not null)
        {
            await _navigator.ReturnToLoginAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
