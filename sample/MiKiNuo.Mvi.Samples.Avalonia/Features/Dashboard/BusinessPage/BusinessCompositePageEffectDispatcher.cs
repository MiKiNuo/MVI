using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示通用复杂业务页面副作用分发器。
/// </summary>
public sealed class BusinessCompositePageEffectDispatcher : IMviEffectDispatcher<BusinessCompositePageEffect>
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(BusinessCompositePageEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
