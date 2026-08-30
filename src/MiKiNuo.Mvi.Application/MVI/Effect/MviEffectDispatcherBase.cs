using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Application.MVI.Effect;

/// <summary>
/// 表示 MVI 副作用派发器基类。
/// </summary>
/// <typeparam name="TIntent">意图类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <remarks>
/// 模板方法模式：
/// <para>
/// <see cref="DispatchAsync"/> 前置 null 检查与
/// 取消标记检查，再委托子类
/// <see cref="DispatchCoreAsync"/> 处理具体副作用。
/// </para>
/// <para>
/// 副作用是框架中唯一的非纯层。执行完毕后可通过
/// <see cref="DispatchIntentAsync"/> 回流新意图，
/// 回流意图作为普通派发重新进入 Store，中间件全程可见。
/// 回流入口由 MviStore 构造时自动接线，无需手动设置。
/// </para>
/// </remarks>
public abstract class MviEffectDispatcherBase<TIntent, TEffect>
    : IMviEffectDispatcher<TEffect>, IMviIntentSinkAttachable<TIntent>
    where TIntent : IMviIntent
    where TEffect : IMviEffect
{
    private IMviIntentSink<TIntent>? _intentSink;

    void IMviIntentSinkAttachable<TIntent>.Attach(IMviIntentSink<TIntent> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _intentSink = sink;
    }

    /// <summary>
    /// 回流新意图到所属 Store。
    /// </summary>
    /// <param name="intent">回流的意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步派发过程的任务。</returns>
    /// <exception cref="InvalidOperationException">分发器未附加到 Store 时抛出。</exception>
    protected ValueTask DispatchIntentAsync(TIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (_intentSink is null)
        {
            throw new InvalidOperationException(
                $"副作用分发器 {GetType().FullName} 尚未附加到 Store，无法回流意图。请通过 MviStore 构造函数接线。");
        }

        return _intentSink.DispatchAsync(intent, cancellationToken);
    }

    /// <summary>
    /// 派发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步操作的 ValueTask。</returns>
    public ValueTask DispatchAsync(
        TEffect effect,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        cancellationToken.ThrowIfCancellationRequested();
        return DispatchCoreAsync(effect, cancellationToken);
    }

    /// <summary>
    /// 子类实现具体副作用派发逻辑。
    /// </summary>
    /// <param name="effect">副作用（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>表示异步操作的 ValueTask。</returns>
    protected abstract ValueTask DispatchCoreAsync(
        TEffect effect,
        CancellationToken cancellationToken);
}
