﻿using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示英雄队伍规约器。
/// </summary>
public sealed partial class HeroRosterReducer
    : MviReducerBase<HeroRosterState, HeroRosterIntent, HeroRosterEffect>
{
    /// <summary>处理训练英雄请求。</summary>
    [MviReduce(typeof(HeroRosterIntent.Train))]
    private MviReduceResult<HeroRosterState, HeroRosterEffect> HandleTrain(
        HeroRosterState state,
        HeroRosterIntent.Train intent,
        IMviBusinessResult? result)
    {
        if (result is FollowUpIntentResult<HeroRosterIntent> fur)
        {
            switch (fur.Intent)
            {
                case HeroRosterIntent.Trained trained:
                {
                    HeroRosterState leveledRoster = ApplyHeroLevel(state, trained.Kind, trained.NewLevel);
                    int nextPower = CalculateHeroPower(leveledRoster.WarriorLevel, leveledRoster.MageLevel, leveledRoster.ArcherLevel);
                    HeroRosterState newRoster = leveledRoster with { HeroTeamPower = nextPower };
                    return WithEffects(
                        newRoster,
                        new HeroRosterEffect[]
                        {
                            new HeroRosterEffect.ConsumeGold(trained.Cost),
                            new HeroRosterEffect.UpdateBattleReadyText(trained.BattleReadyText),
                            new HeroRosterEffect.LogActivity($"训练{trained.HeroName}，消耗 {trained.Cost} 金币，战力 {nextPower}。"),
                            new HeroRosterEffect.Trace($"Hero Train {trained.HeroName}"),
                        });
                }
                case HeroRosterIntent.TrainFailed failed:
                {
                    return WithEffects(
                        state,
                        new HeroRosterEffect[]
                        {
                            new HeroRosterEffect.LogActivity(failed.ErrorMessage ?? "训练失败。"),
                            new HeroRosterEffect.Trace("Hero Train Failed"),
                        });
                }
            }
        }

        return Unchanged(state);
    }

    /// <summary>处理英雄训练成功意图。</summary>
    [MviReduce(typeof(HeroRosterIntent.Trained))]
    private MviReduceResult<HeroRosterState, HeroRosterEffect> HandleTrained(
        HeroRosterState state,
        HeroRosterIntent.Trained intent,
        IMviBusinessResult? result)
    {
        HeroRosterState leveledRoster = ApplyHeroLevel(state, intent.Kind, intent.NewLevel);
        int nextPower = CalculateHeroPower(leveledRoster.WarriorLevel, leveledRoster.MageLevel, leveledRoster.ArcherLevel);
        HeroRosterState newRoster = leveledRoster with { HeroTeamPower = nextPower };
        return WithEffects(
            newRoster,
            new HeroRosterEffect[]
            {
                new HeroRosterEffect.ConsumeGold(intent.Cost),
                new HeroRosterEffect.UpdateBattleReadyText(intent.BattleReadyText),
                new HeroRosterEffect.LogActivity($"训练{intent.HeroName}，消耗 {intent.Cost} 金币，战力 {nextPower}。"),
                new HeroRosterEffect.Trace($"Hero Train {intent.HeroName}"),
            });
    }

    /// <summary>处理英雄训练失败意图。</summary>
    [MviReduce(typeof(HeroRosterIntent.TrainFailed))]
    private MviReduceResult<HeroRosterState, HeroRosterEffect> HandleTrainFailed(
        HeroRosterState state,
        HeroRosterIntent.TrainFailed intent,
        IMviBusinessResult? result)
    {
        return WithEffects(
            state,
            new HeroRosterEffect[]
            {
                new HeroRosterEffect.LogActivity(intent.ErrorMessage ?? "训练失败。"),
                new HeroRosterEffect.Trace("Hero Train Failed"),
            });
    }

    /// <summary>处理增加战力意图。</summary>
    [MviReduce(typeof(HeroRosterIntent.AddPower))]
    private MviReduceResult<HeroRosterState, HeroRosterEffect> HandleAddPower(
        HeroRosterState state,
        HeroRosterIntent.AddPower intent,
        IMviBusinessResult? result)
    {
        HeroRosterState newState = state with { HeroTeamPower = state.HeroTeamPower + intent.Bonus };
        return WithEffect(
            newState,
            new HeroRosterEffect.Trace($"HeroRoster AddPower {intent.Bonus}"));
    }

    private HeroRosterState ApplyHeroLevel(HeroRosterState roster, HeroKind kind, int newLevel)
    {
        return kind switch
        {
            HeroKind.Warrior => roster with { WarriorLevel = newLevel },
            HeroKind.Mage => roster with { MageLevel = newLevel },
            HeroKind.Archer => roster with { ArcherLevel = newLevel },
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "无效的英雄种类。"),
        };
    }

    private int CalculateHeroPower(int warriorLevel, int mageLevel, int archerLevel)
    {
        return warriorLevel * 12 + mageLevel * 15 + archerLevel * 10;
    }
}
