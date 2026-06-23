using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒副作用分发器。
/// </summary>
public sealed class ClinicalReminderEffectDispatcher : IMviEffectDispatcher<ClinicalReminderEffect>
{
    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public ValueTask DispatchAsync(ClinicalReminderEffect effect, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("临床提醒当前无副作用需要派发。");
    }
}
