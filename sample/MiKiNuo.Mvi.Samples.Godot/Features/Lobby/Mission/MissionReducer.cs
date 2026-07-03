﻿using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
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
        MissionIntent.Accept intent,
        IMviBusinessResult? result)
    {
        if (result is FollowUpIntentResult<MissionIntent> fur)
        {
            switch (fur.Intent)
            {
                case MissionIntent.Accepted accepted:
                {
                    MissionState newMission = new(
                        accepted.MissionName,
                        $"已接受 {accepted.MissionName}，消耗体力 {accepted.StaminaCost}，预计奖励 {accepted.Reward}。");
                    return WithEffects(
                        newMission,
                        new MissionEffect[]
                        {
                            new MissionEffect.ConsumeStamina(accepted.StaminaCost),
                            new MissionEffect.UpdateBattleReadyText(accepted.BattleReadyText),
                            new MissionEffect.LogActivity($"接受任务 {accepted.MissionName}。"),
                            new MissionEffect.Trace($"Mission Accept {accepted.MissionName}"),
                        });
                }
                case MissionIntent.AcceptFailed failed:
                {
                    return WithEffects(
                        state,
                        new MissionEffect[]
                        {
                            new MissionEffect.LogActivity(failed.ErrorMessage ?? "接受任务失败。"),
                            new MissionEffect.Trace("Mission Accept Failed"),
                        });
                }
            }
        }

        return Unchanged(state);
    }

    /// <summary>处理任务已接受结果。</summary>
    [MviReduce(typeof(MissionIntent.Accepted))]
    private MviReduceResult<MissionState, MissionEffect> HandleAccepted(
        MissionState state,
        MissionIntent.Accepted intent,
        IMviBusinessResult? result)
    {
        MissionState newMission = new(
            intent.MissionName,
            $"已接受 {intent.MissionName}，消耗体力 {intent.StaminaCost}，预计奖励 {intent.Reward}。");
        return WithEffects(
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
        MissionIntent.AcceptFailed intent,
        IMviBusinessResult? result)
    {
        return WithEffects(
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
        MissionIntent.Complete intent,
        IMviBusinessResult? result)
    {
        if (result is FollowUpIntentResult<MissionIntent> fur
            && fur.Intent is MissionIntent.Completed completed)
        {
            MissionState newMission = state with
            {
                MissionProgress = $"{state.SelectedMission} 已完成，获得金币 {completed.Reward}。",
            };
            return WithEffects(
                newMission,
                new MissionEffect[]
                {
                    new MissionEffect.AddGold(completed.Reward),
                    new MissionEffect.UpdateBattleReadyText(completed.BattleReadyText),
                    new MissionEffect.LogActivity($"任务完成，奖励 {completed.Reward}。"),
                    new MissionEffect.Trace($"Mission Complete reward={completed.Reward}"),
                });
        }

        return Unchanged(state);
    }

    /// <summary>处理任务已完成结果。</summary>
    [MviReduce(typeof(MissionIntent.Completed))]
    private MviReduceResult<MissionState, MissionEffect> HandleCompleted(
        MissionState state,
        MissionIntent.Completed intent,
        IMviBusinessResult? result)
    {
        MissionState newMission = state with
        {
            MissionProgress = $"{state.SelectedMission} 已完成，获得金币 {intent.Reward}。",
        };
        return WithEffects(
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
