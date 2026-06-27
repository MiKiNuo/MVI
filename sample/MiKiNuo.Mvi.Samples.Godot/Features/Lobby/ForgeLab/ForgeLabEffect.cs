using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示锻造工坊 MVI 副作用。
/// </summary>
public abstract partial record ForgeLabEffect : IMviEffect
{
    /// <summary>
    /// 表示写入追踪日志的副作用。
    /// </summary>
    public sealed partial record Trace : ForgeLabEffect, ITraceEffect
    {
        /// <summary>
        /// 初始化写入追踪日志副作用。
        /// </summary>
        /// <param name="text">日志文本。</param>
        public Trace(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            Text = text;
        }

        /// <summary>
        /// 获取日志文本。
        /// </summary>
        public string Text { get; init; }
    }

    /// <summary>
    /// 表示消耗锻造材料的副作用。
    /// </summary>
    /// <param name="OreCost">矿石消耗。</param>
    /// <param name="CrystalCost">水晶消耗。</param>
    public sealed partial record ConsumeMaterials(int OreCost, int CrystalCost) : ForgeLabEffect;

    /// <summary>
    /// 表示更新锻造评分的副作用。
    /// </summary>
    /// <param name="ForgeScore">锻造评分。</param>
    public sealed partial record UpdateForgeScore(int ForgeScore) : ForgeLabEffect;

    /// <summary>
    /// 表示增加英雄战力的副作用。
    /// </summary>
    /// <param name="Bonus">战力加成。</param>
    public sealed partial record AddPower(int Bonus) : ForgeLabEffect;

    /// <summary>
    /// 表示更新战斗准备摘要的副作用。
    /// </summary>
    /// <param name="ReadyText">战斗准备摘要。</param>
    public sealed partial record UpdateBattleReadyText(string ReadyText) : ForgeLabEffect;

    /// <summary>
    /// 表示追加活动日志的副作用。
    /// </summary>
    /// <param name="Message">日志消息。</param>
    public sealed partial record LogActivity(string Message) : ForgeLabEffect;
}
