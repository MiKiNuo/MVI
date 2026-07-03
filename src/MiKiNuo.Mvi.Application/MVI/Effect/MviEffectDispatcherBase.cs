using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Application.MVI.Effect;

/// <summary>
/// 表示 MVI 副作用派发器基类。
/// </summary>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <remarks>
/// 模板方法模式：
/// <para>
/// <see cref="DispatchAsync"/> 前置 null 检查与
/// 取消标记检查，再委托子类
/// <see cref="DispatchCoreAsync"/> 处理具体副作用。
/// </para>
/// </remarks>
public abstract class MviEffectDispatcherBase<TEffect>
    : IMviEffectDispatcher<TEffect>
    where TEffect : IMviEffect
{
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
