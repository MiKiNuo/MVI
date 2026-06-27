using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示任务副作用。
/// </summary>
public abstract partial record MissionEffect : IMviEffect
{
    /// <summary>
    /// 表示写入任务日志的副作用。
    /// </summary>
    public sealed partial record Trace : MissionEffect, ITraceEffect
    {
        /// <summary>
        /// 初始化写入任务日志副作用。
        /// </summary>
        /// <param name="text">日志文本。</param>
        public Trace(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            Text = text;
        }

        /// <summary>获取日志文本。</summary>
        public string Text { get; init; }
    }

    /// <summary>
    /// 表示消耗体力的副作用。
    /// </summary>
    /// <param name="Amount">消耗数量。</param>
    public sealed partial record ConsumeStamina(int Amount) : MissionEffect;

    /// <summary>
    /// 表示增加金币的副作用。
    /// </summary>
    /// <param name="Amount">增加数量。</param>
    public sealed partial record AddGold(int Amount) : MissionEffect;

    /// <summary>
    /// 表示更新战斗准备摘要的副作用。
    /// </summary>
    /// <param name="ReadyText">战斗准备摘要。</param>
    public sealed partial record UpdateBattleReadyText(string ReadyText) : MissionEffect;

    /// <summary>
    /// 表示记录活动日志的副作用。
    /// </summary>
    /// <param name="Message">日志消息。</param>
    public sealed partial record LogActivity(string Message) : MissionEffect;
}
