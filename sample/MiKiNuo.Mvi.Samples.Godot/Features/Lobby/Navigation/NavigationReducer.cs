using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示大厅导航规约器。
/// </summary>
public sealed partial class NavigationReducer
    : MviReducerBase<NavigationState, NavigationIntent, NavigationEffect>
{
    /// <summary>处理选择面板意图。</summary>
    [MviReduce(typeof(NavigationIntent.SelectPanel))]
    private MviReduceResult<NavigationState, NavigationEffect> HandleSelectPanel(
        NavigationState state,
        NavigationIntent.SelectPanel intent,
        IMviBusinessResult? result)
    {
        string title = intent.Panel switch
        {
            LobbyPanel.MissionBoard => "任务大厅",
            LobbyPanel.HeroRoster => "英雄队伍",
            LobbyPanel.Inventory => "背包仓库",
            LobbyPanel.ForgeLab => "锻造工坊",
            LobbyPanel.BattlePrep => "战斗准备",
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent.Panel, "无效的面板。"),
        };
        NavigationState newState = state with
        {
            CurrentPanel = intent.Panel,
            CurrentPanelTitle = title,
        };
        return MviReduceResult.StateAndEffects<NavigationState, NavigationEffect>(
            newState,
            new NavigationEffect[]
            {
                new NavigationEffect.LogActivity($"切换到{title}。"),
                new NavigationEffect.Trace($"Navigation Select {intent.Panel}"),
            });
    }

    /// <summary>处理退出登录意图。</summary>
    [MviReduce(typeof(NavigationIntent.Logout))]
    private MviReduceResult<NavigationState, NavigationEffect> HandleLogout(
        NavigationState state,
        NavigationIntent.Logout intent,
        IMviBusinessResult? result)
    {
        return MviReduceResult.StateAndEffects<NavigationState, NavigationEffect>(
            state,
            new NavigationEffect[]
            {
                new NavigationEffect.LogActivity("大厅请求退出到登录页。"),
                new NavigationEffect.Trace("Navigation Logout"),
                new NavigationEffect.LogoutRequested(),
            });
    }
}
