using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;

/// <summary>
/// 表示 Dashboard 壳意图处理器。
/// </summary>
public sealed class DashboardIntentHandler
    : IMviIntentHandler<DashboardState, DashboardIntent, DashboardEffect>
{
    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>后续意图集合,由 Store 递归派发。</returns>
    public ValueTask<IReadOnlyList<DashboardIntent>> HandleAsync(
        DashboardState state,
        DashboardIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return new ValueTask<IReadOnlyList<DashboardIntent>>(Array.Empty<DashboardIntent>());
    }
}
