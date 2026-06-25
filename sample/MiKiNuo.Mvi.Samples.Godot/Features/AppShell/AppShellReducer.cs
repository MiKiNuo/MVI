using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳规约器。
/// </summary>
public sealed class AppShellReducer
    : MviReducerBase<AppShellState, AppShellIntent, AppShellEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<AppShellState, AppShellEffect> Reduce(
        AppShellState state,
        AppShellIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            AppShellIntent.ShowLogin => MviReduceResult.State<AppShellState, AppShellEffect>(
                state with
                {
                    CurrentScreen = GameScreenKeys.Login,
                    CurrentTitle = "登录游戏",
                    ShellMessage = "已返回登录界面，等待重新进入大厅。",
                }),
            AppShellIntent.ShowLobby => MviReduceResult.State<AppShellState, AppShellEffect>(
                state with
                {
                    CurrentScreen = GameScreenKeys.Lobby,
                    CurrentTitle = "游戏大厅",
                    ShellMessage = "Login MVI 已通过 EffectDispatcher 把玩家资料交给 Lobby MVI。",
                }),
            _ => MviReduceResult.State<AppShellState, AppShellEffect>(state),
        };
    }
}
