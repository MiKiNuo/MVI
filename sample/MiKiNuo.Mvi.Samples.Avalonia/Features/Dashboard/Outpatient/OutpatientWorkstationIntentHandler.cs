using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面意图处理器。
/// </summary>
public sealed class OutpatientWorkstationIntentHandler
    : IMviIntentHandler<OutpatientWorkstationState, OutpatientWorkstationIntent, OutpatientWorkstationEffect>
{
    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图集合,由 Store 递归派发。</returns>
    public ValueTask<IReadOnlyList<OutpatientWorkstationIntent>> HandleAsync(
        OutpatientWorkstationState state,
        OutpatientWorkstationIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return new ValueTask<IReadOnlyList<OutpatientWorkstationIntent>>(Array.Empty<OutpatientWorkstationIntent>());
    }
}
