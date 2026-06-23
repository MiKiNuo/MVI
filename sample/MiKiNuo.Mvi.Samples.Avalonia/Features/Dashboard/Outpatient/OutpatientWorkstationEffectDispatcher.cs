using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面副作用分发器。
/// </summary>
public sealed class OutpatientWorkstationEffectDispatcher : IMviEffectDispatcher<OutpatientWorkstationEffect>
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(OutpatientWorkstationEffect effect, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("门诊工作站当前无副作用需要派发。");
    }
}
