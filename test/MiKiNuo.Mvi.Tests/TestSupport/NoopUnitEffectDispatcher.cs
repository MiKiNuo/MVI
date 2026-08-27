using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Tests.TestSupport;

/// <summary>
/// 表示 UnitEffect 的空操作副作用分发器，
/// 用于 Feature Store 装配的端到端验证（生成器只发现源码内的非泛型具体实现）。
/// </summary>
public sealed class NoopUnitEffectDispatcher : IMviEffectDispatcher<UnitEffect>
{
    /// <summary>
    /// 分发副作用（空操作）。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>已完成的任务。</returns>
    public async ValueTask DispatchAsync(UnitEffect effect, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }
}
