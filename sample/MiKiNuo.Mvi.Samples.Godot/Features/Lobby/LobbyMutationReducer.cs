using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅变更规约器。
/// </summary>
public sealed partial class LobbyMutationReducer
    : MviMutationReducerBase<LobbyState, LobbyMutation, LobbyEffect>
{
    /// <summary>
    /// 应用替换玩家资料变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LobbyState, LobbyEffect> HandleSetPlayer(
        LobbyState state,
        LobbyMutation.SetPlayer mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LobbyState, LobbyEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用累加玩家金币变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LobbyState, LobbyEffect> HandleAddPlayerGold(
        LobbyState state,
        LobbyMutation.AddPlayerGold mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LobbyState, LobbyEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置玩家体力变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LobbyState, LobbyEffect> HandleSetPlayerStamina(
        LobbyState state,
        LobbyMutation.SetPlayerStamina mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LobbyState, LobbyEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用替换导航变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LobbyState, LobbyEffect> HandleSetNavigation(
        LobbyState state,
        LobbyMutation.SetNavigation mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LobbyState, LobbyEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用替换任务变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LobbyState, LobbyEffect> HandleSetMission(
        LobbyState state,
        LobbyMutation.SetMission mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LobbyState, LobbyEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用替换英雄队伍变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LobbyState, LobbyEffect> HandleSetHeroRoster(
        LobbyState state,
        LobbyMutation.SetHeroRoster mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LobbyState, LobbyEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用替换背包仓库变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LobbyState, LobbyEffect> HandleSetInventory(
        LobbyState state,
        LobbyMutation.SetInventory mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LobbyState, LobbyEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用设置战斗准备摘要变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LobbyState, LobbyEffect> HandleSetBattleReadyText(
        LobbyState state,
        LobbyMutation.SetBattleReadyText mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LobbyState, LobbyEffect>(state.Apply(mutation));
    }

    /// <summary>
    /// 应用追加活动日志变更。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="mutation">变更数据。</param>
    /// <returns>规约结果。</returns>
    [MviReduceMutation]
    public MviReduceResult<LobbyState, LobbyEffect> HandleAppendActivityLog(
        LobbyState state,
        LobbyMutation.AppendActivityLog mutation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(mutation);
        return MviReduceResult.State<LobbyState, LobbyEffect>(state.Apply(mutation));
    }
}
