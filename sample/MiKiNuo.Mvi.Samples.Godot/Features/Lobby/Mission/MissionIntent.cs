using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示任务意图。
/// </summary>
public abstract partial record MissionIntent : IMviIntent
{
    /// <summary>
    /// 表示接受任务请求。
    /// </summary>
    public sealed partial record Accept : MissionIntent
    {
        /// <summary>
        /// 初始化接受任务请求。
        /// </summary>
        /// <param name="spec">任务规格。</param>
        public Accept(MissionSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);
            Spec = spec;
        }

        /// <summary>获取任务规格。</summary>
        public MissionSpec Spec { get; init; }
    }

    /// <summary>
    /// 表示任务已接受结果。
    /// </summary>
    /// <param name="MissionName">任务名称。</param>
    /// <param name="StaminaCost">体力消耗。</param>
    /// <param name="Reward">预计奖励。</param>
    /// <param name="NewStamina">剩余体力。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record Accepted(
        string MissionName,
        int StaminaCost,
        int Reward,
        int NewStamina,
        string BattleReadyText) : MissionIntent;

    /// <summary>
    /// 表示任务接受失败结果。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed partial record AcceptFailed(string ErrorMessage) : MissionIntent;

    /// <summary>表示完成当前任务请求。</summary>
    public sealed partial record Complete : MissionIntent;

    /// <summary>
    /// 表示任务已完成结果。
    /// </summary>
    /// <param name="Reward">奖励金币。</param>
    /// <param name="BattleReadyText">战斗准备摘要。</param>
    public sealed partial record Completed(int Reward, string BattleReadyText) : MissionIntent;
}
