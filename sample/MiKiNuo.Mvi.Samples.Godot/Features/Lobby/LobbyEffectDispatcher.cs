using System;
using System.Threading;
using System.Threading.Tasks;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅副作用分发器。
/// <para>
/// 仅处理日志与导航类副作用：写 <see cref="LobbyEffect.Trace"/> 日志，
/// 处理 <see cref="LobbyEffect.LogoutRequested"/> 调用导航协调器返回登录页。
/// </para>
/// </summary>
public sealed class LobbyEffectDispatcher : IMviEffectDispatcher<LobbyEffect>
{
    private IGameShellNavigator? _navigator;

    /// <summary>
    /// 初始化游戏大厅副作用分发器。
    /// </summary>
    public LobbyEffectDispatcher()
    {
    }

    /// <summary>
    /// 设置游戏壳导航协调器。
    /// </summary>
    /// <param name="navigator">游戏壳导航协调器。</param>
    public void SetNavigator(IGameShellNavigator navigator)
    {
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public async ValueTask DispatchAsync(LobbyEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        cancellationToken.ThrowIfCancellationRequested();

        switch (effect)
        {
            case LobbyEffect.Trace trace:
                GD.Print($"[Godot Game MVI Lobby Effect] {trace.Text}");
                break;
            case LobbyEffect.LogoutRequested:
                await HandleLogoutRequestedAsync(cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async ValueTask HandleLogoutRequestedAsync(CancellationToken cancellationToken)
    {
        GD.Print("[Godot Game MVI Lobby Effect] ReturnToLogin");
        if (_navigator is not null)
        {
            await _navigator.ReturnToLoginAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
