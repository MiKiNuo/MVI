using MiKiNuo.Mvi.Domain.MVI.Mutation;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅 MVI 变更。
/// </summary>
public abstract record LobbyMutation : IMviMutation<LobbyState>
{
    /// <summary>
    /// 表示替换玩家资料子状态的变更。
    /// </summary>
    /// <param name="Value">玩家资料。</param>
    [MviMutation(Path = "Player")]
    public sealed record SetPlayer(LobbyPlayer Value) : LobbyMutation;

    /// <summary>
    /// 表示累加玩家金币的变更。
    /// </summary>
    /// <param name="Value">金币增量。</param>
    [MviMutation(Path = "Player.Gold", Op = MutationOp.Add)]
    public sealed record AddPlayerGold(int Value) : LobbyMutation;

    /// <summary>
    /// 表示设置玩家体力的变更。
    /// </summary>
    /// <param name="Value">体力值。</param>
    [MviMutation(Path = "Player.Stamina")]
    public sealed record SetPlayerStamina(int Value) : LobbyMutation;

    /// <summary>
    /// 表示替换导航子状态的变更。
    /// </summary>
    /// <param name="Value">导航状态。</param>
    [MviMutation(Path = "Navigation")]
    public sealed record SetNavigation(LobbyNavigation Value) : LobbyMutation;

    /// <summary>
    /// 表示替换任务子状态的变更。
    /// </summary>
    /// <param name="Value">任务状态。</param>
    [MviMutation(Path = "Mission")]
    public sealed record SetMission(LobbyMission Value) : LobbyMutation;

    /// <summary>
    /// 表示替换英雄队伍子状态的变更。
    /// </summary>
    /// <param name="Value">英雄队伍状态。</param>
    [MviMutation(Path = "HeroRoster")]
    public sealed record SetHeroRoster(LobbyHeroRoster Value) : LobbyMutation;

    /// <summary>
    /// 表示替换背包仓库子状态的变更。
    /// </summary>
    /// <param name="Value">背包仓库状态。</param>
    [MviMutation(Path = "Inventory")]
    public sealed record SetInventory(LobbyInventory Value) : LobbyMutation;

    /// <summary>
    /// 表示设置战斗准备摘要的变更。
    /// </summary>
    /// <param name="Value">战斗准备摘要。</param>
    [MviMutation(Path = "BattleReadyText")]
    public sealed record SetBattleReadyText(string Value) : LobbyMutation;

    /// <summary>
    /// 表示追加活动日志的变更。
    /// </summary>
    /// <param name="Value">日志条目。</param>
    [MviMutation(Path = "ActivityLog", Op = MutationOp.Append)]
    public sealed record AppendActivityLog(string Value) : LobbyMutation;
}
