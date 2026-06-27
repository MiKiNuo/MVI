using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示任务规约器。
/// </summary>
public sealed partial class MissionReducer
    : MviReducerBase<MissionState, MissionIntent, MissionEffect>
{
    /// <summary>处理接受任务请求。</summary>
    [MviReduce(typeof(MissionIntent.Accept))]
    private MviReduceResult<MissionState, MissionEffect> HandleAccept(
        MissionState state,
        MissionIntent.Accept intent)
        => MviReduceResult.State<MissionState, MissionEffect>(state);

    /// <summary>处理任务已接受结果。</summary>
    [MviReduce(typeof(MissionIntent.Accepted))]
    private MviReduceResult<MissionState, MissionEffect> HandleAccepted(
        MissionState state,
        MissionIntent.Accepted intent)
    {
        MissionState newMission = new(
            intent.MissionName,
            $"已接受 {intent.MissionName}，消耗体力 {intent.StaminaCost}，预计奖励 {intent.Reward}。");
        return MviReduceResult.StateAndEffects<MissionState, MissionEffect>(
            newMission,
            new MissionEffect[]
            {
                new MissionEffect.ConsumeStamina(intent.StaminaCost),
                new MissionEffect.UpdateBattleReadyText(intent.BattleReadyText),
                new MissionEffect.LogActivity($"接受任务 {intent.MissionName}。"),
                new MissionEffect.Trace($"Mission Accept {intent.MissionName}"),
            });
    }

    /// <summary>处理任务接受失败结果。</summary>
    [MviReduce(typeof(MissionIntent.AcceptFailed))]
    private MviReduceResult<MissionState, MissionEffect> HandleAcceptFailed(
        MissionState state,
        MissionIntent.AcceptFailed intent)
    {
        return MviReduceResult.StateAndEffects<MissionState, MissionEffect>(
            state,
            new MissionEffect[]
            {
                new MissionEffect.LogActivity(intent.ErrorMessage ?? "接受任务失败。"),
                new MissionEffect.Trace("Mission Accept Failed"),
            });
    }

    /// <summary>处理完成任务请求。</summary>
    [MviReduce(typeof(MissionIntent.Complete))]
    private MviReduceResult<MissionState, MissionEffect> HandleComplete(
        MissionState state,
        MissionIntent.Complete intent)
        => MviReduceResult.State<MissionState, MissionEffect>(state);

    /// <summary>处理任务已完成结果。</summary>
    [MviReduce(typeof(MissionIntent.Completed))]
    private MviReduceResult<MissionState, MissionEffect> HandleCompleted(
        MissionState state,
        MissionIntent.Completed intent)
    {
        MissionState newMission = state with
        {
            MissionProgress = $"{state.SelectedMission} 已完成，获得金币 {intent.Reward}。",
        };
        return MviReduceResult.StateAndEffects<MissionState, MissionEffect>(
            newMission,
            new MissionEffect[]
            {
                new MissionEffect.AddGold(intent.Reward),
                new MissionEffect.UpdateBattleReadyText(intent.BattleReadyText),
                new MissionEffect.LogActivity($"任务完成，奖励 {intent.Reward}。"),
                new MissionEffect.Trace($"Mission Complete reward={intent.Reward}"),
            });
    }
}
