using System;
using System.Threading;
using System.Threading.Tasks;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅副作用分发器。
/// </summary>
public sealed class LobbyEffectDispatcher : IMviEffectDispatcher<LobbyEffect>
{
    private IGameShellNavigator? _navigator;

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
    public ValueTask DispatchAsync(LobbyEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        cancellationToken.ThrowIfCancellationRequested();
        if (effect is LobbyEffect.Trace trace)
        {
            GD.Print($"[Godot Game MVI Lobby Effect] {trace.Text}");
            return ValueTask.CompletedTask;
        }

        if (effect is LobbyEffect.LogoutRequested && _navigator is not null)
        {
            GD.Print("[Godot Game MVI Lobby Effect] ReturnToLogin");
            return _navigator.ReturnToLoginAsync(cancellationToken);
        }

        return ValueTask.CompletedTask;
    }
}
