using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Application.MVI.Effect;

/// <summary>
/// 表示空副作用分发器。
/// </summary>
public sealed class NullEffectDispatcher : IMviEffectDispatcher<UnitEffect>
{
    /// <summary>
    /// 获取空副作用分发器单例。
    /// </summary>
    public static NullEffectDispatcher Instance { get; } = new();

    private NullEffectDispatcher()
    {
    }

    /// <summary>
    /// 分发副作用(无操作)。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(UnitEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);
        return default;
    }
}
