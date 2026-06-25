using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.IntentHandler;

/// <summary>
/// 表示意图处理器,执行异步业务产生后续意图。
/// </summary>
/// <remarks>
/// IntentHandler 承担异步业务(API 调用、注册表写入等),
/// 产生的后续意图由 Store 递归派发到 Reducer 完成状态转换。
/// </remarks>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IMviIntentHandler<TState, TIntent, TEffect>
    where TState : IMviState
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图集合,由 Store 递归派发。</returns>
    public ValueTask<IReadOnlyList<TIntent>> HandleAsync(
        TState state,
        TIntent intent,
        CancellationToken cancellationToken = default);
}
