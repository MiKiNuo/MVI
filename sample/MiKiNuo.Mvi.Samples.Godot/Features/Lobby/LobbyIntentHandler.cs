using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示游戏大厅意图处理器。
/// </summary>
public sealed class LobbyIntentHandler
    : IMviIntentHandler<LobbyState, LobbyIntent, LobbyEffect>
{
    /// <summary>
    /// 处理意图并产生动作副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>动作副作用集合。</returns>
    public ValueTask<IReadOnlyList<LobbyEffect>> HandleAsync(
        LobbyState state,
        LobbyIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        IReadOnlyList<LobbyEffect> effects = intent switch
        {
            LobbyIntent.SetPlayer setPlayer => new LobbyEffect[]
            {
                new LobbyEffect.RequestSetPlayer(setPlayer.Profile),
                new LobbyEffect.Trace("Lobby SetPlayer"),
            },
            LobbyIntent.SelectMissionBoard => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Lobby Select MissionBoard"),
            },
            LobbyIntent.SelectHeroRoster => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Lobby Select HeroRoster"),
            },
            LobbyIntent.SelectInventory => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Lobby Select Inventory"),
            },
            LobbyIntent.SelectForgeLab => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Lobby Select ForgeLab"),
            },
            LobbyIntent.SelectBattlePrep => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Lobby Select BattlePrep"),
            },
            LobbyIntent.AcceptForestMission => new LobbyEffect[]
            {
                new LobbyEffect.RequestAcceptMission("森林巡逻", 12, 80),
            },
            LobbyIntent.AcceptMineMission => new LobbyEffect[]
            {
                new LobbyEffect.RequestAcceptMission("矿洞救援", 18, 125),
            },
            LobbyIntent.CompleteMission => new LobbyEffect[]
            {
                new LobbyEffect.RequestCompleteMission(),
            },
            LobbyIntent.TrainWarrior => new LobbyEffect[]
            {
                new LobbyEffect.RequestTrainHero(HeroKind.Warrior, "战士", state.HeroRoster.WarriorLevel),
            },
            LobbyIntent.TrainMage => new LobbyEffect[]
            {
                new LobbyEffect.RequestTrainHero(HeroKind.Mage, "法师", state.HeroRoster.MageLevel),
            },
            LobbyIntent.TrainArcher => new LobbyEffect[]
            {
                new LobbyEffect.RequestTrainHero(HeroKind.Archer, "弓手", state.HeroRoster.ArcherLevel),
            },
            LobbyIntent.UsePotion => new LobbyEffect[]
            {
                new LobbyEffect.RequestUsePotion(),
            },
            LobbyIntent.OpenGoldBox => new LobbyEffect[]
            {
                new LobbyEffect.RequestOpenGoldBox(),
            },
            LobbyIntent.ForgeWeapon => new LobbyEffect[]
            {
                new LobbyEffect.RequestForge("武器", 2, 1, 8),
            },
            LobbyIntent.ForgeArmor => new LobbyEffect[]
            {
                new LobbyEffect.RequestForge("护甲", 1, 1, 5),
            },
            LobbyIntent.PrepareBattle => new LobbyEffect[]
            {
                new LobbyEffect.RequestPrepareBattle(),
            },
            LobbyIntent.Logout => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Lobby Logout"),
                new LobbyEffect.LogoutRequested(),
            },
            LobbyIntent.PlayerSet => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Lobby PlayerSet"),
            },
            LobbyIntent.MissionAccepted accepted => new LobbyEffect[]
            {
                new LobbyEffect.Trace($"Mission Accept {accepted.MissionName}"),
            },
            LobbyIntent.MissionAcceptFailed => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Mission Accept Failed"),
            },
            LobbyIntent.MissionCompleted completed => new LobbyEffect[]
            {
                new LobbyEffect.Trace($"Mission Complete reward={completed.Reward}"),
            },
            LobbyIntent.HeroTrained trained => new LobbyEffect[]
            {
                new LobbyEffect.Trace($"Hero Train {trained.HeroName}"),
            },
            LobbyIntent.HeroTrainFailed => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Hero Train Failed"),
            },
            LobbyIntent.PotionUsed => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Inventory UsePotion"),
            },
            LobbyIntent.PotionUseFailed => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Inventory UsePotion Failed"),
            },
            LobbyIntent.GoldBoxOpened => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Inventory OpenGoldBox"),
            },
            LobbyIntent.Forged forged => new LobbyEffect[]
            {
                new LobbyEffect.Trace($"Forge {forged.ItemName}"),
            },
            LobbyIntent.ForgeFailed => new LobbyEffect[]
            {
                new LobbyEffect.Trace("Forge Failed"),
            },
            LobbyIntent.BattlePrepared => new LobbyEffect[]
            {
                new LobbyEffect.Trace("BattlePrep Prepare"),
            },
            _ => Array.Empty<LobbyEffect>(),
        };
        return new ValueTask<IReadOnlyList<LobbyEffect>>(effects);
    }
}
