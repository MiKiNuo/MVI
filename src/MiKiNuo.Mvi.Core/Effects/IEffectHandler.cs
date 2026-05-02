using MiKiNuo.Mvi.Abstractions;

namespace MiKiNuo.Mvi.Core.Effects;

/// <summary>
/// 表示 MVI 副作用处理器。
/// </summary>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <typeparam name="TIntent">意图类型。</typeparam>
public interface IEffectHandler<TEffect, TIntent>
    where TEffect : IMviEffect
    where TIntent : IMviIntent
{
    /// <summary>
    /// 处理副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="sink">意图接收器。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    ValueTask HandleAsync(
        TEffect effect,
        IIntentSink<TIntent> sink,
        CancellationToken cancellationToken);
}
