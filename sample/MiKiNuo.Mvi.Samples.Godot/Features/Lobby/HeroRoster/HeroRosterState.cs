using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示英雄队伍子状态。
/// </summary>
public sealed record HeroRosterState : IMviState
{
    /// <summary>初始化英雄队伍子状态。</summary>
    public HeroRosterState(int heroTeamPower, int warriorLevel, int mageLevel, int archerLevel)
    {
        HeroTeamPower = heroTeamPower;
        WarriorLevel = warriorLevel;
        MageLevel = mageLevel;
        ArcherLevel = archerLevel;
    }

    /// <summary>获取英雄队伍战力。</summary>
    public int HeroTeamPower { get; init; }

    /// <summary>获取战士等级。</summary>
    public int WarriorLevel { get; init; }

    /// <summary>获取法师等级。</summary>
    public int MageLevel { get; init; }

    /// <summary>获取弓手等级。</summary>
    public int ArcherLevel { get; init; }

    /// <summary>获取初始英雄队伍状态。</summary>
    public static HeroRosterState Initial { get; } = new(36, 3, 2, 2);
}
