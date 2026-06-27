using System;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示玩家资料 MVI 状态。
/// </summary>
public sealed record PlayerState : IMviState
{
    /// <summary>初始化玩家资料状态。</summary>
    /// <param name="playerName">玩家名称。</param>
    /// <param name="playerLevel">玩家等级。</param>
    /// <param name="gold">金币数量。</param>
    /// <param name="stamina">体力值。</param>
    public PlayerState(string playerName, int playerLevel, int gold, int stamina)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
        PlayerName = playerName;
        PlayerLevel = playerLevel;
        Gold = gold;
        Stamina = stamina;
    }

    /// <summary>获取玩家名称。</summary>
    public string PlayerName { get; init; }

    /// <summary>获取玩家等级。</summary>
    public int PlayerLevel { get; init; }

    /// <summary>获取金币数量。</summary>
    public int Gold { get; init; }

    /// <summary>获取体力值。</summary>
    public int Stamina { get; init; }

    /// <summary>获取初始玩家状态。</summary>
    public static PlayerState Initial { get; } = new("未登录指挥官", 1, 0, 0);
}
