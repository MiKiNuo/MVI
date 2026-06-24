using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳意图处理器。
/// </summary>
public sealed class AppShellIntentHandler
    : IMviIntentHandler<AppShellState, AppShellIntent, AppShellMutation, AppShellEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<AppShellMutation, AppShellEffect>> HandleAsync(
        AppShellState state,
        AppShellIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<AppShellMutation, AppShellEffect> result = intent switch
        {
            AppShellIntent.ShowLogin => HandleShowLogin(),
            AppShellIntent.ShowLobby => HandleShowLobby(),
            _ => MviHandleResult.Empty<AppShellMutation, AppShellEffect>(),
        };
        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// 处理显示登录界面意图。
    /// </summary>
    /// <returns>处理结果。</returns>
    private static MviHandleResult<AppShellMutation, AppShellEffect> HandleShowLogin()
    {
        AppShellMutation[] mutations = new AppShellMutation[]
        {
            new AppShellMutation.SetCurrentScreen(GameScreenKeys.Login),
            new AppShellMutation.SetCurrentTitle("登录游戏"),
            new AppShellMutation.SetShellMessage("已返回登录界面，等待重新进入大厅。"),
        };
        AppShellEffect[] effects = new AppShellEffect[] { new AppShellEffect.Trace("Shell ShowLogin") };
        return MviHandleResult.MutationsAndEffects<AppShellMutation, AppShellEffect>(mutations, effects);
    }

    /// <summary>
    /// 处理显示游戏大厅意图。
    /// </summary>
    /// <returns>处理结果。</returns>
    private static MviHandleResult<AppShellMutation, AppShellEffect> HandleShowLobby()
    {
        AppShellMutation[] mutations = new AppShellMutation[]
        {
            new AppShellMutation.SetCurrentScreen(GameScreenKeys.Lobby),
            new AppShellMutation.SetCurrentTitle("游戏大厅"),
            new AppShellMutation.SetShellMessage("Login MVI 已通过 EffectDispatcher 把玩家资料交给 Lobby MVI。"),
        };
        AppShellEffect[] effects = new AppShellEffect[] { new AppShellEffect.Trace("Shell ShowLobby") };
        return MviHandleResult.MutationsAndEffects<AppShellMutation, AppShellEffect>(mutations, effects);
    }
}
