using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Domain.MVI.Business;

namespace MiKiNuo.Mvi.Samples.Godot.Features.Lobby;

/// <summary>
/// 表示活动日志意图处理器。
/// </summary>
public sealed class ActivityLogIntentHandler
    : MviIntentHandlerBase<ActivityLogState, ActivityLogIntent, ActivityLogEffect>
{
    /// <summary>
    /// 初始化活动日志意图处理器。
    /// </summary>
    public ActivityLogIntentHandler()
    {
    }

    /// <summary>
    /// 处理具体业务逻辑。
    /// </summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>业务结果;无业务时返回 null。</returns>
    protected override async ValueTask<IMviBusinessResult?> HandleCoreAsync(
        ActivityLogState state,
        ActivityLogIntent intent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return null;
    }
}
