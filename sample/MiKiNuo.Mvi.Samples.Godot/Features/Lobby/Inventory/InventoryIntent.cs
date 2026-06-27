using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示背包仓库 MVI 意图。
/// </summary>
public abstract partial record InventoryIntent : IMviIntent
{
    /// <summary>表示使用药水的意图。</summary>
    public sealed partial record UsePotion : InventoryIntent;

    /// <summary>表示药水使用成功的意图。</summary>
    /// <param name="NewPotionCount">剩余药水。</param>
    /// <param name="NewStamina">恢复后体力。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record PotionUsed(
        int NewPotionCount,
        int NewStamina,
        string BattleReadyText) : InventoryIntent;

    /// <summary>表示药水使用失败的意图。</summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record PotionUseFailed(string ErrorMessage) : InventoryIntent;

    /// <summary>表示打开金币箱的意图。</summary>
    public sealed partial record OpenGoldBox : InventoryIntent;

    /// <summary>表示金币箱已打开的意图。</summary>
    /// <param name="Gold">获得金币。</param>
    public sealed partial record GoldBoxOpened(int Gold) : InventoryIntent;

    /// <summary>表示消耗材料的意图。</summary>
    /// <param name="OreCost">矿石消耗。</param>
    /// <param name="CrystalCost">水晶消耗。</param>
    public sealed partial record ConsumeMaterials(int OreCost, int CrystalCost) : InventoryIntent;

    /// <summary>表示更新锻造评分的意图。</summary>
    /// <param name="ForgeScore">锻造评分。</param>
    public sealed partial record UpdateForgeScore(int ForgeScore) : InventoryIntent;
}
