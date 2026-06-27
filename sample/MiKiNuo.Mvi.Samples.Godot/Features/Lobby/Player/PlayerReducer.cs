using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示玩家资料规约器。
/// </summary>
public sealed partial class PlayerReducer
    : MviReducerBase<PlayerState, PlayerIntent, PlayerEffect>
{
    /// <summary>处理设置玩家资料意图。</summary>
    [MviReduce(typeof(PlayerIntent.SetPlayer))]
    private MviReduceResult<PlayerState, PlayerEffect> HandleSetPlayer(
        PlayerState state,
        PlayerIntent.SetPlayer intent)
    {
        PlayerProfile profile = intent.Profile;
        PlayerState newState = new(profile.PlayerName, profile.Level, profile.Gold, profile.Stamina);
        return MviReduceResult.StateAndEffects<PlayerState, PlayerEffect>(
            newState,
            new PlayerEffect[]
            {
                new PlayerEffect.Trace("Player SetPlayer"),
                new PlayerEffect.LogActivity($"登录玩家：{profile.PlayerName}。"),
            });
    }

    /// <summary>处理玩家资料已设意图。</summary>
    [MviReduce(typeof(PlayerIntent.PlayerSet))]
    private MviReduceResult<PlayerState, PlayerEffect> HandlePlayerSet(
        PlayerState state,
        PlayerIntent.PlayerSet intent)
    {
        return MviReduceResult.StateAndEffects<PlayerState, PlayerEffect>(
            state,
            new PlayerEffect[]
            {
                new PlayerEffect.UpdateBattleReadyText(intent.BattleReadyText),
                new PlayerEffect.Trace("Player PlayerSet"),
            });
    }

    /// <summary>处理消耗金币意图。</summary>
    [MviReduce(typeof(PlayerIntent.ConsumeGold))]
    private MviReduceResult<PlayerState, PlayerEffect> HandleConsumeGold(
        PlayerState state,
        PlayerIntent.ConsumeGold intent)
    {
        PlayerState newState = state with { Gold = Math.Max(0, state.Gold - intent.Amount) };
        return MviReduceResult.StateAndEffect<PlayerState, PlayerEffect>(
            newState,
            new PlayerEffect.Trace($"Player ConsumeGold {intent.Amount}"));
    }

    /// <summary>处理增加金币意图。</summary>
    [MviReduce(typeof(PlayerIntent.AddGold))]
    private MviReduceResult<PlayerState, PlayerEffect> HandleAddGold(
        PlayerState state,
        PlayerIntent.AddGold intent)
    {
        PlayerState newState = state with { Gold = state.Gold + intent.Amount };
        return MviReduceResult.StateAndEffect<PlayerState, PlayerEffect>(
            newState,
            new PlayerEffect.Trace($"Player AddGold {intent.Amount}"));
    }

    /// <summary>处理消耗体力意图。</summary>
    [MviReduce(typeof(PlayerIntent.ConsumeStamina))]
    private MviReduceResult<PlayerState, PlayerEffect> HandleConsumeStamina(
        PlayerState state,
        PlayerIntent.ConsumeStamina intent)
    {
        PlayerState newState = state with { Stamina = Math.Max(0, state.Stamina - intent.Amount) };
        return MviReduceResult.StateAndEffect<PlayerState, PlayerEffect>(
            newState,
            new PlayerEffect.Trace($"Player ConsumeStamina {intent.Amount}"));
    }

    /// <summary>处理恢复体力意图。</summary>
    [MviReduce(typeof(PlayerIntent.RestoreStamina))]
    private MviReduceResult<PlayerState, PlayerEffect> HandleRestoreStamina(
        PlayerState state,
        PlayerIntent.RestoreStamina intent)
    {
        PlayerState newState = state with { Stamina = intent.NewStamina };
        return MviReduceResult.StateAndEffect<PlayerState, PlayerEffect>(
            newState,
            new PlayerEffect.Trace($"Player RestoreStamina {intent.NewStamina}"));
    }
}
