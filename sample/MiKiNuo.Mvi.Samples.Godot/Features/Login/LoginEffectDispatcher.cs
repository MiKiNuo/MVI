using System;
using System.Threading;
using System.Threading.Tasks;
using global::Godot;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Store;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Login;

/// <summary>
/// 表示游戏登录副作用分发器。
/// <para>
/// 处理 <see cref="LoginEffect.RequestLogin"/> 时调用认证服务，
/// 根据结果向 Store 派发 <see cref="LoginIntent.LoginSucceeded"/> 或 <see cref="LoginIntent.LoginFailed"/>。
/// </para>
/// </summary>
public sealed class LoginEffectDispatcher : IMviEffectDispatcher<LoginEffect>
{
    private readonly IAuthService _authService;
    private readonly IGameShellNavigator _navigator;
    private readonly Func<IMviStore<LoginState, LoginIntent, LoginEffect>> _storeFactory;

    /// <summary>
    /// 初始化游戏登录副作用分发器。
    /// </summary>
    /// <param name="authService">认证服务。</param>
    /// <param name="navigator">游戏壳导航协调器。</param>
    /// <param name="storeFactory">Store 工厂，用于延迟获取 Store 引用以派发意图。</param>
    public LoginEffectDispatcher(
        IAuthService authService,
        IGameShellNavigator navigator,
        Func<IMviStore<LoginState, LoginIntent, LoginEffect>> storeFactory)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        _storeFactory = storeFactory ?? throw new ArgumentNullException(nameof(storeFactory));
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
            case LoginEffect.RequestLogin requestLogin:
                await HandleRequestLoginAsync(requestLogin, cancellationToken).ConfigureAwait(false);
                break;
            case LoginEffect.Trace trace:
                GD.Print($"[Godot Game MVI Login Effect] {trace.Text}");
                break;
            case LoginEffect.LoginSucceeded succeeded:
                GD.Print($"[Godot Game MVI Login Effect] OpenLobby {succeeded.Profile.PlayerName}");
                await _navigator.OpenLobbyAsync(succeeded.Profile, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// 处理登录请求副作用：调用认证服务并派发结果意图。
    /// </summary>
    /// <param name="requestLogin">登录请求副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步处理过程的任务。</returns>
    private async ValueTask HandleRequestLoginAsync(
        LoginEffect.RequestLogin requestLogin,
        CancellationToken cancellationToken)
    {
        LoginResult result = await _authService
            .LoginAsync(requestLogin.UserName, requestLogin.Password, cancellationToken)
            .ConfigureAwait(false);

        IMviStore<LoginState, LoginIntent, LoginEffect> store = _storeFactory();
        if (result.IsSuccess && result.Profile is not null)
        {
            await store.DispatchAsync(
                new LoginIntent.LoginSucceeded(result.Profile),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await store.DispatchAsync(
                new LoginIntent.LoginFailed(result.ErrorMessage ?? "登录失败。"),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
