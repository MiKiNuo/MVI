using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒意图处理器。
/// </summary>
public sealed class ClinicalReminderIntentHandler
    : MviIntentHandlerBase<ClinicalReminderState, ClinicalReminderIntent>
{
    /// <summary>处理具体业务逻辑。</summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>后续意图;无后续工作时返回 null。</returns>
    protected override async ValueTask<ClinicalReminderIntent?> HandleCoreAsync(
        ClinicalReminderState state,
        ClinicalReminderIntent intent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return null;
    }
}
