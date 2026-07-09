using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Tests.TestSupport;

/// <summary>
/// 表示空操作副作用分发器，
/// 用于测试中不需要验证副作用的场景。
/// </summary>
/// <typeparam name="TEffect">副作用类型。</typeparam>
public sealed class NoopEffectDispatcher<TEffect> : IMviEffectDispatcher<TEffect>
    where TEffect : IMviEffect
{
    /// <summary>
    /// 分发副作用(空操作)。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>已完成的任务。</returns>
    public ValueTask DispatchAsync(TEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
