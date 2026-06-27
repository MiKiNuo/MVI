using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示英雄队伍 MVI 意图。
/// </summary>
public abstract partial record HeroRosterIntent : IMviIntent
{
    /// <summary>表示训练英雄的意图。</summary>
    /// <param name="Kind">英雄种类。</param>
    public sealed partial record Train(HeroKind Kind) : HeroRosterIntent;

    /// <summary>表示英雄训练成功的意图。</summary>
    /// <param name="Kind">英雄种类。</param>
    /// <param name="HeroName">英雄名称。</param>
    /// <param name="NewLevel">新等级。</param>
    /// <param name="Cost">消耗金币。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record Trained(
        HeroKind Kind,
        string HeroName,
        int NewLevel,
        int Cost,
        string BattleReadyText) : HeroRosterIntent;

    /// <summary>表示英雄训练失败的意图。</summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record TrainFailed(string ErrorMessage) : HeroRosterIntent;

    /// <summary>表示增加战力的意图。</summary>
    /// <param name="Bonus">战力加成。</param>
    public sealed partial record AddPower(int Bonus) : HeroRosterIntent;
}
