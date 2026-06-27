using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示战斗准备规约器。
/// </summary>
public sealed partial class BattlePrepReducer
    : MviReducerBase<BattlePrepState, BattlePrepIntent, BattlePrepEffect>
{
    /// <summary>处理请求准备战斗意图。</summary>
    [MviReduce(typeof(BattlePrepIntent.PrepareBattle))]
    private MviReduceResult<BattlePrepState, BattlePrepEffect> HandlePrepareBattle(
        BattlePrepState state,
        BattlePrepIntent.PrepareBattle intent)
        => MviReduceResult.State<BattlePrepState, BattlePrepEffect>(state);

    /// <summary>处理战斗准备完成意图。</summary>
    [MviReduce(typeof(BattlePrepIntent.BattlePrepared))]
    private MviReduceResult<BattlePrepState, BattlePrepEffect> HandleBattlePrepared(
        BattlePrepState state,
        BattlePrepIntent.BattlePrepared intent)
    {
        BattlePrepState newState = state with { BattleReadyText = intent.BattleReadyText };
        BattlePrepEffect[] effects = new BattlePrepEffect[]
        {
            new BattlePrepEffect.LogActivity("战斗准备汇总任务、英雄、背包数据。"),
            new BattlePrepEffect.Trace("BattlePrep Prepare"),
        };
        return MviReduceResult.StateAndEffects<BattlePrepState, BattlePrepEffect>(newState, effects);
    }

    /// <summary>处理更新战斗准备摘要意图。</summary>
    [MviReduce(typeof(BattlePrepIntent.UpdateReadyText))]
    private MviReduceResult<BattlePrepState, BattlePrepEffect> HandleUpdateReadyText(
        BattlePrepState state,
        BattlePrepIntent.UpdateReadyText intent)
    {
        BattlePrepState newState = state with { BattleReadyText = intent.ReadyText };
        return MviReduceResult.StateAndEffect<BattlePrepState, BattlePrepEffect>(
            newState,
            new BattlePrepEffect.Trace("BattlePrep UpdateReadyText"));
    }
}
