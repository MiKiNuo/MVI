using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.Reducer;

/// <summary>
/// 表示 MVI 规约器基类。
/// </summary>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <remarks>
/// 双阶段 Reduce 协议：
/// <para>
/// <c>MviStore</c> 在单次 Intent 派发中对同一 Reducer 调用两次 Reduce。
/// </para>
/// <list type="number">
/// <item>第一次：传入 <c>result</c> 为 null，
/// 产中间状态（如 IsBusy=true）。</item>
/// <item>第二次：传入 IntentHandler 返回的业务结果，
/// 产最终状态与副作用。</item>
/// </list>
/// <para>
/// 若 IntentHandler 返回 null 则跳过第二次。
/// Handle* 方法内用 <c>if (result is null)</c> 区分两阶段。
/// </para>
/// <para>
/// Guard 谓词在两阶段各求值一次；
/// 需写 <c>CanSubmit || IsBusy</c> 允许第二阶段通过。
/// </para>
/// </remarks>
public abstract class MviReducerBase<TState, TIntent, TEffect>
    : IMviReducer<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>
    /// 将意图与业务结果规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="result">业务结果,无异步业务时为 null。</param>
    /// <returns>规约结果。</returns>
    public abstract MviReduceResult<TState, TEffect> Reduce(
        TState state,
        TIntent intent,
        IMviBusinessResult? result = null);

    /// <summary>
    /// 返回状态不变的规约结果（无副作用）。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <returns>仅包含状态的规约结果。</returns>
    protected static MviReduceResult<TState, TEffect> Unchanged(TState state)
    {
        return MviReduceResult.State<TState, TEffect>(state);
    }

    /// <summary>
    /// 返回新状态与单个副作用的规约结果。
    /// </summary>
    /// <param name="state">规约后的状态。</param>
    /// <param name="effect">副作用。</param>
    /// <returns>包含状态与副作用的规约结果。</returns>
    protected static MviReduceResult<TState, TEffect> WithEffect(
        TState state,
        TEffect effect)
    {
        return MviReduceResult.StateAndEffect<TState, TEffect>(state, effect);
    }

    /// <summary>
    /// 返回新状态与多个副作用的规约结果。
    /// </summary>
    /// <param name="state">规约后的状态。</param>
    /// <param name="effects">副作用集合。</param>
    /// <returns>包含状态与副作用集合的规约结果。</returns>
    protected static MviReduceResult<TState, TEffect> WithEffects(
        TState state,
        IReadOnlyList<TEffect> effects)
    {
        return MviReduceResult.StateAndEffects<TState, TEffect>(state, effects);
    }

    /// <summary>
    /// 返回新状态与多个副作用的规约结果。
    /// </summary>
    /// <param name="state">规约后的状态。</param>
    /// <param name="effects">副作用数组。</param>
    /// <returns>包含状态与副作用集合的规约结果。</returns>
    protected static MviReduceResult<TState, TEffect> WithEffects(
        TState state,
        params TEffect[] effects)
    {
        return MviReduceResult.StateAndEffects<TState, TEffect>(state, effects);
    }
}
