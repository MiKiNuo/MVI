using System;
using MiKiNuo.Mvi.Samples.Shared.Features.Login;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Common;

/// <summary>
/// 表示登录成功后传递给游戏大厅的玩家资料。
/// </summary>
public sealed record PlayerProfile : ILoginProfile
{
    /// <summary>
    /// 初始化玩家资料。
    /// </summary>
    /// <param name="playerName">玩家名称。</param>
    /// <param name="level">玩家等级。</param>
    /// <param name="gold">金币数量。</param>
    /// <param name="stamina">体力值。</param>
    public PlayerProfile(string playerName, int level, int gold, int stamina)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
        PlayerName = playerName;
        Level = level;
        Gold = gold;
        Stamina = stamina;
    }

    /// <summary>
    /// 获取玩家名称。
    /// </summary>
    public string PlayerName { get; init; }

    /// <summary>
    /// 显式实现登录资料显示名，返回玩家名称。
    /// </summary>
    string ILoginProfile.DisplayName => PlayerName;

    /// <summary>
    /// 获取玩家等级。
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// 获取金币数量。
    /// </summary>
    public int Gold { get; init; }

    /// <summary>
    /// 获取体力值。
    /// </summary>
    public int Stamina { get; init; }
}
