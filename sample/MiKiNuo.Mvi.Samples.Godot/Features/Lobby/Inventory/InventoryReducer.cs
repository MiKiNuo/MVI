using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
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
        InventoryIntent.UsePotion intent)
        => MviReduceResult.State<InventoryState, InventoryEffect>(state);

    /// <summary>处理药水使用成功意图。</summary>
    [MviReduce(typeof(InventoryIntent.PotionUsed))]
    private MviReduceResult<InventoryState, InventoryEffect> HandlePotionUsed(
        InventoryState state,
        InventoryIntent.PotionUsed intent)
    {
        InventoryState newState = state with { PotionCount = intent.NewPotionCount };
        return MviReduceResult.StateAndEffects<InventoryState, InventoryEffect>(
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
        InventoryIntent.PotionUseFailed intent)
    {
        return MviReduceResult.StateAndEffects<InventoryState, InventoryEffect>(
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
        InventoryIntent.OpenGoldBox intent)
        => MviReduceResult.State<InventoryState, InventoryEffect>(state);

    /// <summary>处理金币箱已打开意图。</summary>
    [MviReduce(typeof(InventoryIntent.GoldBoxOpened))]
    private MviReduceResult<InventoryState, InventoryEffect> HandleGoldBoxOpened(
        InventoryState state,
        InventoryIntent.GoldBoxOpened intent)
    {
        return MviReduceResult.StateAndEffects<InventoryState, InventoryEffect>(
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
        InventoryIntent.ConsumeMaterials intent)
    {
        InventoryState newState = state with
        {
            OreCount = Math.Max(0, state.OreCount - intent.OreCost),
            CrystalCount = Math.Max(0, state.CrystalCount - intent.CrystalCost),
        };
        return MviReduceResult.StateAndEffect<InventoryState, InventoryEffect>(
            newState,
            new InventoryEffect.Trace("Inventory ConsumeMaterials"));
    }

    /// <summary>处理更新锻造评分意图。</summary>
    [MviReduce(typeof(InventoryIntent.UpdateForgeScore))]
    private MviReduceResult<InventoryState, InventoryEffect> HandleUpdateForgeScore(
        InventoryState state,
        InventoryIntent.UpdateForgeScore intent)
    {
        InventoryState newState = state with { ForgeScore = intent.ForgeScore };
        return MviReduceResult.StateAndEffect<InventoryState, InventoryEffect>(
            newState,
            new InventoryEffect.Trace("Inventory UpdateForgeScore"));
    }
}
