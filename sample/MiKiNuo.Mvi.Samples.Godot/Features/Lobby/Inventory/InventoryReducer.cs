﻿using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示背包仓库规约器。
/// </summary>
public sealed partial class InventoryReducer
    : MviReducerBase<InventoryState, InventoryIntent, InventoryEffect>
{
    /// <summary>处理使用药水请求。</summary>
    [MviReduce(typeof(InventoryIntent.UsePotion))]
    private MviReduceResult<InventoryState, InventoryEffect> HandleUsePotion(
        InventoryState state,
        InventoryIntent.UsePotion intent,
        IMviBusinessResult? result)
    {
        if (result is FollowUpIntentResult<InventoryIntent> fur)
        {
            switch (fur.Intent)
            {
                case InventoryIntent.PotionUsed used:
                {
                    InventoryState newState = state with { PotionCount = used.NewPotionCount };
                    return WithEffects(
                        newState,
                        new InventoryEffect[]
                        {
                            new InventoryEffect.RestoreStamina(used.NewStamina),
                            new InventoryEffect.UpdateBattleReadyText(used.BattleReadyText),
                            new InventoryEffect.LogActivity("使用药水，体力恢复 20。"),
                            new InventoryEffect.Trace("Inventory UsePotion"),
                        });
                }
                case InventoryIntent.PotionUseFailed failed:
                {
                    return WithEffects(
                        state,
                        new InventoryEffect[]
                        {
                            new InventoryEffect.LogActivity(failed.ErrorMessage ?? "使用药水失败。"),
                            new InventoryEffect.Trace("Inventory UsePotion Failed"),
                        });
                }
            }
        }

        return Unchanged(state);
    }

    /// <summary>处理药水使用成功意图。</summary>
    [MviReduce(typeof(InventoryIntent.PotionUsed))]
    private MviReduceResult<InventoryState, InventoryEffect> HandlePotionUsed(
        InventoryState state,
        InventoryIntent.PotionUsed intent,
        IMviBusinessResult? result)
    {
        InventoryState newState = state with { PotionCount = intent.NewPotionCount };
        return WithEffects(
            newState,
            new InventoryEffect[]
            {
                new InventoryEffect.RestoreStamina(intent.NewStamina),
                new InventoryEffect.UpdateBattleReadyText(intent.BattleReadyText),
                new InventoryEffect.LogActivity("使用药水，体力恢复 20。"),
                new InventoryEffect.Trace("Inventory UsePotion"),
            });
    }

    /// <summary>处理药水使用失败意图。</summary>
    [MviReduce(typeof(InventoryIntent.PotionUseFailed))]
    private MviReduceResult<InventoryState, InventoryEffect> HandlePotionUseFailed(
        InventoryState state,
        InventoryIntent.PotionUseFailed intent,
        IMviBusinessResult? result)
    {
        return WithEffects(
            state,
            new InventoryEffect[]
            {
                new InventoryEffect.LogActivity(intent.ErrorMessage ?? "使用药水失败。"),
                new InventoryEffect.Trace("Inventory UsePotion Failed"),
            });
    }

    /// <summary>处理打开金币箱请求。</summary>
    [MviReduce(typeof(InventoryIntent.OpenGoldBox))]
    private MviReduceResult<InventoryState, InventoryEffect> HandleOpenGoldBox(
        InventoryState state,
        InventoryIntent.OpenGoldBox intent,
        IMviBusinessResult? result)
    {
        if (result is FollowUpIntentResult<InventoryIntent> fur
            && fur.Intent is InventoryIntent.GoldBoxOpened opened)
        {
            return WithEffects(
                state,
                new InventoryEffect[]
                {
                    new InventoryEffect.AddGold(opened.Gold),
                    new InventoryEffect.LogActivity($"打开金币箱，金币增加 {opened.Gold}。"),
                    new InventoryEffect.Trace("Inventory OpenGoldBox"),
                });
        }

        return Unchanged(state);
    }

    /// <summary>处理金币箱已打开意图。</summary>
    [MviReduce(typeof(InventoryIntent.GoldBoxOpened))]
    private MviReduceResult<InventoryState, InventoryEffect> HandleGoldBoxOpened(
        InventoryState state,
        InventoryIntent.GoldBoxOpened intent,
        IMviBusinessResult? result)
    {
        return WithEffects(
            state,
            new InventoryEffect[]
            {
                new InventoryEffect.AddGold(intent.Gold),
                new InventoryEffect.LogActivity($"打开金币箱，金币增加 {intent.Gold}。"),
                new InventoryEffect.Trace("Inventory OpenGoldBox"),
            });
    }

    /// <summary>处理消耗材料意图。</summary>
    [MviReduce(typeof(InventoryIntent.ConsumeMaterials))]
    private MviReduceResult<InventoryState, InventoryEffect> HandleConsumeMaterials(
        InventoryState state,
        InventoryIntent.ConsumeMaterials intent,
        IMviBusinessResult? result)
    {
        InventoryState newState = state with
        {
            OreCount = Math.Max(0, state.OreCount - intent.OreCost),
            CrystalCount = Math.Max(0, state.CrystalCount - intent.CrystalCost),
        };
        return WithEffect(
            newState,
            new InventoryEffect.Trace("Inventory ConsumeMaterials"));
    }

    /// <summary>处理更新锻造评分意图。</summary>
    [MviReduce(typeof(InventoryIntent.UpdateForgeScore))]
    private MviReduceResult<InventoryState, InventoryEffect> HandleUpdateForgeScore(
        InventoryState state,
        InventoryIntent.UpdateForgeScore intent,
        IMviBusinessResult? result)
    {
        InventoryState newState = state with { ForgeScore = intent.ForgeScore };
        return WithEffect(
            newState,
            new InventoryEffect.Trace("Inventory UpdateForgeScore"));
    }
}
