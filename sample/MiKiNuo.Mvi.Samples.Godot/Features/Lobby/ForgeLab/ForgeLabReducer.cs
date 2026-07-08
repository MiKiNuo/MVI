﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示锻造工坊规约器。
/// </summary>
public sealed partial class ForgeLabReducer
    : MviReducerBase<UnitState, ForgeLabIntent, ForgeLabEffect>
{
    /// <summary>处理请求锻造装备意图。</summary>
    [MviReduce(typeof(ForgeLabIntent.Forge))]
    private MviReduceResult<UnitState, ForgeLabEffect> HandleForge(
        UnitState state,
        ForgeLabIntent.Forge intent,
        IMviBusinessResult? result)
    {
        if (result is FollowUpIntentResult<ForgeLabIntent> fur)
        {
            switch (fur.Intent)
            {
                case ForgeLabIntent.Forged forged:
                {
                    ForgeLabEffect[] effects = new ForgeLabEffect[]
                    {
                        new ForgeLabEffect.ConsumeMaterials(forged.OreCost, forged.CrystalCost),
                        new ForgeLabEffect.UpdateForgeScore(forged.ForgeScore),
                        new ForgeLabEffect.AddPower(forged.PowerBonus),
                        new ForgeLabEffect.UpdateBattleReadyText(forged.BattleReadyText),
                        new ForgeLabEffect.LogActivity($"锻造{forged.ItemName}，评分 {forged.ForgeScore}，战力 +{forged.PowerBonus}。"),
                        new ForgeLabEffect.Trace($"Forge {forged.ItemName}"),
                    };
                    return WithEffects(state, effects);
                }
                case ForgeLabIntent.ForgeFailed failed:
                {
                    ForgeLabEffect[] effects = new ForgeLabEffect[]
                    {
                        new ForgeLabEffect.LogActivity(failed.ErrorMessage ?? "锻造失败。"),
                        new ForgeLabEffect.Trace("Forge Failed"),
                    };
                    return WithEffects(state, effects);
                }
            }
        }

        return Unchanged(state);
    }

    /// <summary>处理锻造成功意图。</summary>
    [MviReduce(typeof(ForgeLabIntent.Forged))]
    private MviReduceResult<UnitState, ForgeLabEffect> HandleForged(
        UnitState state,
        ForgeLabIntent.Forged intent,
        IMviBusinessResult? result)
    {
        ForgeLabEffect[] effects = new ForgeLabEffect[]
        {
            new ForgeLabEffect.ConsumeMaterials(intent.OreCost, intent.CrystalCost),
            new ForgeLabEffect.UpdateForgeScore(intent.ForgeScore),
            new ForgeLabEffect.AddPower(intent.PowerBonus),
            new ForgeLabEffect.UpdateBattleReadyText(intent.BattleReadyText),
            new ForgeLabEffect.LogActivity($"锻造{intent.ItemName}，评分 {intent.ForgeScore}，战力 +{intent.PowerBonus}。"),
            new ForgeLabEffect.Trace($"Forge {intent.ItemName}"),
        };
        return WithEffects(state, effects);
    }

    /// <summary>处理锻造失败意图。</summary>
    [MviReduce(typeof(ForgeLabIntent.ForgeFailed))]
    private MviReduceResult<UnitState, ForgeLabEffect> HandleForgeFailed(
        UnitState state,
        ForgeLabIntent.ForgeFailed intent,
        IMviBusinessResult? result)
    {
        ForgeLabEffect[] effects = new ForgeLabEffect[]
        {
            new ForgeLabEffect.LogActivity(intent.ErrorMessage ?? "锻造失败。"),
            new ForgeLabEffect.Trace("Forge Failed"),
        };
        return WithEffects(state, effects);
    }
}
