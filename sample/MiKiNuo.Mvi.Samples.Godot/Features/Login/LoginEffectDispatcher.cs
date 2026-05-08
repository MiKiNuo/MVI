using System;
using System.Threading;
using System.Threading.Tasks;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录副作用分发器。
/// </summary>
public sealed class LoginEffectDispatcher : IMviEffectDispatcher<LoginEffect>
{
    private readonly IGameShellNavigator _navigator;

    /// <summary>
    /// 初始化游戏登录副作用分发器。
    /// </summary>
    /// <param name="navigator">游戏壳导航协调器。</param>
    public LoginEffectDispatcher(IGameShellNavigator navigator)
    {
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    /// <inheritdoc />
    public ValueTask DispatchAsync(LoginEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        cancellationToken.ThrowIfCancellationRequested();

        if (effect is LoginEffect.Trace trace)
        {
            GD.Print($"[Godot Game MVI Login Effect] {trace.Text}");
            return ValueTask.CompletedTask;
        }

        if (effect is LoginEffect.LoginSucceeded succeeded)
        {
            GD.Print($"[Godot Game MVI Login Effect] OpenLobby {succeeded.Profile.PlayerName}");
            return _navigator.OpenLobbyAsync(succeeded.Profile, cancellationToken);
        }

        return ValueTask.CompletedTask;
    }
}
