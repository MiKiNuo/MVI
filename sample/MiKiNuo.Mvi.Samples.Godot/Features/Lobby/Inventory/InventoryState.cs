using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示背包仓库子状态。
/// </summary>
public sealed record InventoryState : IMviState
{
    /// <summary>初始化背包仓库子状态。</summary>
    public InventoryState(int potionCount, int oreCount, int crystalCount, int forgeScore)
    {
        PotionCount = potionCount;
        OreCount = oreCount;
        CrystalCount = crystalCount;
        ForgeScore = forgeScore;
    }

    /// <summary>获取药水数量。</summary>
    public int PotionCount { get; init; }

    /// <summary>获取矿石数量。</summary>
    public int OreCount { get; init; }

    /// <summary>获取水晶数量。</summary>
    public int CrystalCount { get; init; }

    /// <summary>获取锻造评分。</summary>
    public int ForgeScore { get; init; }

    /// <summary>获取初始背包状态。</summary>
    public static InventoryState Initial { get; } = new(2, 4, 1, 0);
}
