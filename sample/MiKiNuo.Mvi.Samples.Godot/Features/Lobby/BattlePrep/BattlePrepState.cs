using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示战斗准备 MVI 状态。
/// </summary>
public sealed partial record BattlePrepState : IMviState
{
    /// <summary>
    /// 初始化战斗准备状态。
    /// </summary>
    /// <param name="battleReadyText">战斗准备摘要。</param>
    public BattlePrepState(string battleReadyText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleReadyText);
        BattleReadyText = battleReadyText;
    }

    /// <summary>
    /// 获取战斗准备摘要。
    /// </summary>
    public string BattleReadyText { get; init; }

    /// <summary>
    /// 获取初始战斗准备状态。
    /// </summary>
    public static BattlePrepState Initial { get; } = new("等待大厅初始化。");
}
