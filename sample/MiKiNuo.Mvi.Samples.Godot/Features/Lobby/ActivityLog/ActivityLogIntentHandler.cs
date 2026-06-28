using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Domain.MVI.Business;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示活动日志意图处理器。
/// </summary>
public sealed class ActivityLogIntentHandler
    : IMviIntentHandler<ActivityLogState, ActivityLogIntent, ActivityLogEffect>
{
    /// <summary>
    /// 初始化活动日志意图处理器。
    /// </summary>
    public ActivityLogIntentHandler()
    {
    }

    /// <summary>
    /// 处理意图并产生后续意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>业务结果;无业务时返回 null。</returns>
    public ValueTask<IMviBusinessResult?> HandleAsync(
        ActivityLogState state,
        ActivityLogIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);
        return ValueTask.FromResult<IMviBusinessResult?>(null);
    }
}
