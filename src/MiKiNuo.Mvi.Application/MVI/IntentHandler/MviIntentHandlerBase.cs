using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Application.MVI.IntentHandler;

/// <summary>
/// 表示 MVI 意图处理器基类。
/// </summary>
/// <remarks>
/// 模板方法模式:
/// <see cref="HandleAsync"/> 前置 null 检查与
/// 取消标记检查,再委托子类
/// <see cref="HandleCoreAsync"/> 处理具体业务。
/// </remarks>
/// <typeparam name="TState">状态类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
public abstract class MviIntentHandlerBase<TState, TIntent>
    : IMviIntentHandler<TState, TIntent>
    where TState : IMviState
    where TIntent : IMviIntent
{
    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图,无后续工作时返回 null。</returns>
    public async ValueTask<TIntent?> HandleAsync(
        TState state,
        TIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        cancellationToken.ThrowIfCancellationRequested();
        return await HandleCoreAsync(state, intent, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 子类实现具体业务处理逻辑。
    /// </summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>后续意图,无后续工作时返回 null。</returns>
    protected abstract ValueTask<TIntent?> HandleCoreAsync(
        TState state,
        TIntent intent,
        CancellationToken cancellationToken);
}
