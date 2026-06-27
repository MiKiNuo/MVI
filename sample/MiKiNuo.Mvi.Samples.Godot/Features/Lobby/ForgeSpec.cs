namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示锻造规格。
/// </summary>
/// <param name="ItemName">装备名称。</param>
/// <param name="OreCost">矿石消耗。</param>
/// <param name="CrystalCost">水晶消耗。</param>
/// <param name="PowerBonus">战力加成。</param>
public sealed record ForgeSpec(
    string ItemName,
    int OreCost,
    int CrystalCost,
    int PowerBonus)
{
    /// <summary>获取武器锻造规格。</summary>
    public static ForgeSpec Weapon { get; } = new("武器", 2, 1, 8);

    /// <summary>获取护甲锻造规格。</summary>
    public static ForgeSpec Armor { get; } = new("护甲", 1, 1, 5);
}
