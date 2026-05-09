using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Application.MVI.Effect;

/// <summary>
/// 表示 MVI 副作用分发器。
/// </summary>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public interface IMviEffectDispatcher<in TEffect>
    where TEffect : IMviEffect
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(TEffect effect, CancellationToken cancellationToken = default);
}
