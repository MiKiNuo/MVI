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

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public async ValueTask DispatchAsync(LoginEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        cancellationToken.ThrowIfCancellationRequested();

        switch (effect)
        {
            case LoginEffect.Trace trace:
                GD.Print($"[Godot Game MVI Login Effect] {trace.Text}");
                break;
            case LoginEffect.LoginSucceeded succeeded:
                GD.Print($"[Godot Game MVI Login Effect] OpenLobby {succeeded.Profile.PlayerName}");
                await _navigator.OpenLobbyAsync(succeeded.Profile, cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
