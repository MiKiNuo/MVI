using MiKiNuo.Mvi.Application.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒副作用分发器。
/// </summary>
public sealed class ClinicalReminderEffectDispatcher : IMviEffectDispatcher<ClinicalReminderEffect>
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(ClinicalReminderEffect effect, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
