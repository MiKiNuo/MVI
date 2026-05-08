using System;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅 MVI 意图。
/// </summary>
public abstract partial record LobbyIntent : IMviIntent
{
    /// <summary>
    /// 表示挂载大厅子 ViewModel 的意图。
    /// </summary>
    public sealed partial record AttachChildren : LobbyIntent
    {
        /// <summary>
        /// 初始化挂载大厅子 ViewModel 的意图。
        /// </summary>
        public AttachChildren(
            PlayerHeaderViewModel playerHeaderViewModel,
            LobbyMenuViewModel lobbyMenuViewModel,
            MissionBoardViewModel missionBoardViewModel,
            HeroRosterViewModel heroRosterViewModel,
            InventoryViewModel inventoryViewModel,
            ForgeLabViewModel forgeLabViewModel,
            BattlePrepViewModel battlePrepViewModel,
            ActivityLogViewModel activityLogViewModel)
        {
            ArgumentNullException.ThrowIfNull(playerHeaderViewModel);
            ArgumentNullException.ThrowIfNull(lobbyMenuViewModel);
            ArgumentNullException.ThrowIfNull(missionBoardViewModel);
            ArgumentNullException.ThrowIfNull(heroRosterViewModel);
            ArgumentNullException.ThrowIfNull(inventoryViewModel);
            ArgumentNullException.ThrowIfNull(forgeLabViewModel);
            ArgumentNullException.ThrowIfNull(battlePrepViewModel);
            ArgumentNullException.ThrowIfNull(activityLogViewModel);
            PlayerHeaderViewModel = playerHeaderViewModel;
            LobbyMenuViewModel = lobbyMenuViewModel;
            MissionBoardViewModel = missionBoardViewModel;
            HeroRosterViewModel = heroRosterViewModel;
            InventoryViewModel = inventoryViewModel;
            ForgeLabViewModel = forgeLabViewModel;
            BattlePrepViewModel = battlePrepViewModel;
            ActivityLogViewModel = activityLogViewModel;
        }

        /// <summary>获取玩家头部 ViewModel。</summary>
        public PlayerHeaderViewModel PlayerHeaderViewModel { get; init; }

        /// <summary>获取大厅菜单 ViewModel。</summary>
        public LobbyMenuViewModel LobbyMenuViewModel { get; init; }

        /// <summary>获取任务大厅 ViewModel。</summary>
        public MissionBoardViewModel MissionBoardViewModel { get; init; }

        /// <summary>获取英雄队伍 ViewModel。</summary>
        public HeroRosterViewModel HeroRosterViewModel { get; init; }

        /// <summary>获取背包仓库 ViewModel。</summary>
        public InventoryViewModel InventoryViewModel { get; init; }

        /// <summary>获取锻造工坊 ViewModel。</summary>
        public ForgeLabViewModel ForgeLabViewModel { get; init; }

        /// <summary>获取战斗准备 ViewModel。</summary>
        public BattlePrepViewModel BattlePrepViewModel { get; init; }

        /// <summary>获取活动日志 ViewModel。</summary>
        public ActivityLogViewModel ActivityLogViewModel { get; init; }
    }

    /// <summary>
    /// 表示设置玩家资料的意图。
    /// </summary>
    public sealed partial record SetPlayer : LobbyIntent
    {
        /// <summary>
        /// 初始化设置玩家资料的意图。
        /// </summary>
        /// <param name="profile">玩家资料。</param>
        public SetPlayer(PlayerProfile profile)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        /// <summary>
        /// 获取玩家资料。
        /// </summary>
        public PlayerProfile Profile { get; init; }
    }

    /// <summary>表示选择任务大厅面板的意图。</summary>
    public sealed partial record SelectMissionBoard : LobbyIntent;

    /// <summary>表示选择英雄队伍面板的意图。</summary>
    public sealed partial record SelectHeroRoster : LobbyIntent;

    /// <summary>表示选择背包仓库面板的意图。</summary>
    public sealed partial record SelectInventory : LobbyIntent;

    /// <summary>表示选择锻造工坊面板的意图。</summary>
    public sealed partial record SelectForgeLab : LobbyIntent;

    /// <summary>表示选择战斗准备面板的意图。</summary>
    public sealed partial record SelectBattlePrep : LobbyIntent;

    /// <summary>表示接受森林任务的意图。</summary>
    public sealed partial record AcceptForestMission : LobbyIntent;

    /// <summary>表示接受矿洞任务的意图。</summary>
    public sealed partial record AcceptMineMission : LobbyIntent;

    /// <summary>表示完成当前任务的意图。</summary>
    public sealed partial record CompleteMission : LobbyIntent;

    /// <summary>表示训练战士的意图。</summary>
    public sealed partial record TrainWarrior : LobbyIntent;

    /// <summary>表示训练法师的意图。</summary>
    public sealed partial record TrainMage : LobbyIntent;

    /// <summary>表示训练弓手的意图。</summary>
    public sealed partial record TrainArcher : LobbyIntent;

    /// <summary>表示使用药水的意图。</summary>
    public sealed partial record UsePotion : LobbyIntent;

    /// <summary>表示打开金币箱的意图。</summary>
    public sealed partial record OpenGoldBox : LobbyIntent;

    /// <summary>表示锻造武器的意图。</summary>
    public sealed partial record ForgeWeapon : LobbyIntent;

    /// <summary>表示锻造护甲的意图。</summary>
    public sealed partial record ForgeArmor : LobbyIntent;

    /// <summary>表示准备战斗的意图。</summary>
    public sealed partial record PrepareBattle : LobbyIntent;

    /// <summary>表示退出到登录页的意图。</summary>
    public sealed partial record Logout : LobbyIntent;
}
