using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示战斗准备 MVI 意图。
/// </summary>
public abstract partial record BattlePrepIntent : IMviIntent
{
    /// <summary>
    /// 表示请求准备战斗的意图。
    /// </summary>
    public sealed partial record PrepareBattle : BattlePrepIntent;

    /// <summary>
    /// 表示战斗准备完成的意图。
    /// </summary>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record BattlePrepared(string BattleReadyText) : BattlePrepIntent;

    /// <summary>
    /// 表示更新战斗准备摘要的意图。
    /// </summary>
    public sealed partial record UpdateReadyText : BattlePrepIntent
    {
        /// <summary>
        /// 初始化更新战斗准备摘要意图。
        /// </summary>
        /// <param name="readyText">战斗准备摘要。</param>
        public UpdateReadyText(string readyText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(readyText);
            ReadyText = readyText;
        }

        /// <summary>
        /// 获取战斗准备摘要。
        /// </summary>
        public string ReadyText { get; init; }
    }
}
