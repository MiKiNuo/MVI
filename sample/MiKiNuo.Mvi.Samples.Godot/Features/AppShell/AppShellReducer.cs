using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳规约器。
/// </summary>
public sealed partial class AppShellReducer
    : MviReducerBase<AppShellState, AppShellIntent, AppShellEffect>
{
    /// <summary>处理显示登录界面意图。</summary>
    [MviReduce(typeof(AppShellIntent.ShowLogin))]
    private MviReduceResult<AppShellState, AppShellEffect> HandleShowLogin(
        AppShellState state,
        AppShellIntent.ShowLogin intent)
    {
        return MviReduceResult.State<AppShellState, AppShellEffect>(
            state with
            {
                CurrentScreen = GameScreenKeys.Login,
                CurrentTitle = "登录游戏",
                ShellMessage = "已返回登录界面，等待重新进入大厅。",
            });
    }

    /// <summary>处理显示游戏大厅意图。</summary>
    [MviReduce(typeof(AppShellIntent.ShowLobby))]
    private MviReduceResult<AppShellState, AppShellEffect> HandleShowLobby(
        AppShellState state,
        AppShellIntent.ShowLobby intent)
    {
        return MviReduceResult.State<AppShellState, AppShellEffect>(
            state with
            {
                CurrentScreen = GameScreenKeys.Lobby,
                CurrentTitle = "游戏大厅",
                ShellMessage = "Login MVI 已通过 EffectDispatcher 把玩家资料交给 Lobby MVI。",
            });
    }
}
