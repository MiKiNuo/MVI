using System;
using System.Threading;
using System.Threading.Tasks;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录副作用分发器。
/// <para>
/// 处理 <see cref="LoginEffect.Trace"/> 写日志，
/// 处理 <see cref="LoginEffect.LoginSucceeded"/> 调用导航协调器进入大厅。
/// </para>
/// </summary>
public sealed class LoginEffectDispatcher : MviEffectDispatcherBase<LoginEffect>
{
    private readonly IGameShellNavigator _navigator;
    private readonly ITraceEffectLogger _traceLogger;

    /// <summary>
    /// 初始化游戏登录副作用分发器。
    /// </summary>
    /// <param name="navigator">游戏壳导航协调器。</param>
    /// <param name="logger">追踪日志记录器。</param>
    public LoginEffectDispatcher(IGameShellNavigator navigator, ITraceEffectLogger logger)
    {
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        _traceLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 分发具体副作用。
    /// </summary>
    /// <param name="effect">副作用（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    protected override async ValueTask DispatchCoreAsync(LoginEffect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case LoginEffect.Trace trace:
                _traceLogger.Log(trace);
                break;
            case LoginEffect.LoginSucceeded succeeded:
                GD.Print($"[Godot Game MVI Login Effect] OpenLobby {succeeded.Profile.PlayerName}");
                await _navigator.OpenLobbyAsync(succeeded.Profile, cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
