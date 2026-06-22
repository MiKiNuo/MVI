using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Header;

/// <summary>
/// 表示 Dashboard 头部组件副作用分发器。
/// </summary>
public sealed class HeaderEffectDispatcher : IMviEffectDispatcher<HeaderEffect>
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(HeaderEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
