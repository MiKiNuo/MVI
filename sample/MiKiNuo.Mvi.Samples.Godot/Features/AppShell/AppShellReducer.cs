using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳 Reducer。
/// </summary>
public sealed partial class AppShellReducer : MviReducerBase<AppShellState, AppShellIntent, AppShellEffect>
{
    /// <summary>
    /// 处理显示登录界面意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">显示登录界面意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AppShellState, AppShellEffect> Reduce(
        AppShellState state,
        AppShellIntent.ShowLogin intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        AppShellState nextState = state with
        {
            CurrentScreen = GameScreenKeys.Login,
            CurrentTitle = "登录游戏",
            ShellMessage = "已返回登录界面，等待重新进入大厅。",
        };
        return MviReduceResult.StateAndEffect<AppShellState, AppShellEffect>(
            nextState,
            new AppShellEffect.Trace("Shell ShowLogin"));
    }

    /// <summary>
    /// 处理显示游戏大厅意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">显示游戏大厅意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AppShellState, AppShellEffect> Reduce(
        AppShellState state,
        AppShellIntent.ShowLobby intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        AppShellState nextState = state with
        {
            CurrentScreen = GameScreenKeys.Lobby,
            CurrentTitle = "游戏大厅",
            ShellMessage = "Login MVI 已通过 EffectDispatcher 把玩家资料交给 Lobby MVI。",
        };
        return MviReduceResult.StateAndEffect<AppShellState, AppShellEffect>(
            nextState,
            new AppShellEffect.Trace("Shell ShowLobby"));
    }
}
