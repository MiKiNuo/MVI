using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示锻造工坊 MVI 意图。
/// </summary>
public abstract partial record ForgeLabIntent : IMviIntent
{
    /// <summary>
    /// 表示请求锻造装备的意图。
    /// </summary>
    public sealed partial record Forge : ForgeLabIntent
    {
        /// <summary>
        /// 初始化请求锻造装备的意图。
        /// </summary>
        /// <param name="spec">锻造规格。</param>
        public Forge(ForgeSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);
            Spec = spec;
        }

        /// <summary>
        /// 获取锻造规格。
        /// </summary>
        public ForgeSpec Spec { get; init; }
    }

    /// <summary>
    /// 表示锻造成功的意图。
    /// </summary>
    /// <param name="ItemName">装备名称。</param>
    /// <param name="OreCost">矿石消耗。</param>
    /// <param name="CrystalCost">水晶消耗。</param>
    /// <param name="PowerBonus">战力加成。</param>
    /// <param name="ForgeScore">锻造评分。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record Forged(
        string ItemName,
        int OreCost,
        int CrystalCost,
        int PowerBonus,
        int ForgeScore,
        string BattleReadyText) : ForgeLabIntent;

    /// <summary>
    /// 表示锻造失败的意图。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record ForgeFailed(string ErrorMessage) : ForgeLabIntent;
}
