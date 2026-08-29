using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;

/// <summary>
/// 表示事件绑定选择面板意图处理器。
/// </summary>
public sealed class EventBindingSelectionIntentHandler
    : MviIntentHandlerBase<EventBindingSelectionState, EventBindingSelectionIntent>
{
    /// <summary>处理具体业务逻辑。</summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>后续意图;无后续工作时返回 null。</returns>
    protected override async ValueTask<EventBindingSelectionIntent?> HandleCoreAsync(
        EventBindingSelectionState state,
        EventBindingSelectionIntent intent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return null;
    }
}
