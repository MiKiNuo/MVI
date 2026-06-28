using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Shared.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定组合根意图处理器。
/// </summary>
public sealed class EventBindingWorkbenchIntentHandler
    : IMviIntentHandler<EventBindingWorkbenchState, EventBindingWorkbenchIntent, UnitEffect>
{
    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>业务结果;无业务时返回 null。</returns>
    public ValueTask<IMviBusinessResult?> HandleAsync(
        EventBindingWorkbenchState state,
        EventBindingWorkbenchIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return ValueTask.FromResult<IMviBusinessResult?>(null);
    }
}
