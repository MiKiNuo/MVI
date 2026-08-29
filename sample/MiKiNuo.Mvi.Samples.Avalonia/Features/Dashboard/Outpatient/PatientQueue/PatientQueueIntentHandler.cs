using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列意图处理器。
/// </summary>
public sealed class PatientQueueIntentHandler
    : MviIntentHandlerBase<PatientQueueState, PatientQueueIntent>
{
    /// <summary>处理具体业务逻辑。</summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>业务结果;无业务时返回 null。</returns>
    protected override async ValueTask<PatientQueueIntent?> HandleCoreAsync(
        PatientQueueState state,
        PatientQueueIntent intent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return null;
    }
}
