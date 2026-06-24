using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒意图处理器。
/// </summary>
public sealed class ClinicalReminderIntentHandler
    : IMviIntentHandler<ClinicalReminderState, ClinicalReminderIntent, ClinicalReminderMutation, ClinicalReminderEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<ClinicalReminderMutation, ClinicalReminderEffect>> HandleAsync(
        ClinicalReminderState state,
        ClinicalReminderIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<ClinicalReminderMutation, ClinicalReminderEffect> result = intent switch
        {
            ClinicalReminderIntent.LoadPatient loadPatient => HandleLoadPatient(loadPatient),
            ClinicalReminderIntent.ResolvePrimaryAlert => HandleResolvePrimaryAlert(),
            _ => MviHandleResult.Empty<ClinicalReminderMutation, ClinicalReminderEffect>(),
        };

        return ValueTask.FromResult(result);
    }

    private static MviHandleResult<ClinicalReminderMutation, ClinicalReminderEffect> HandleLoadPatient(
        ClinicalReminderIntent.LoadPatient intent)
    {
        IReadOnlyList<string> alerts = intent.PatientName.Contains("胸闷", StringComparison.Ordinal)
            ? ["胸痛中心绿色通道评估。", "建议 10 分钟内完成心电图。", "核查阿司匹林禁忌。"]
            : ["核查过敏史。", "复核既往用药。", "完善生命体征记录。"];

        return MviHandleResult.Mutations<ClinicalReminderMutation, ClinicalReminderEffect>(
            new ClinicalReminderMutation.SetPatientName(intent.PatientName),
            new ClinicalReminderMutation.SetAlerts(alerts),
            new ClinicalReminderMutation.SetPrimaryAlert(alerts[0]),
            new ClinicalReminderMutation.SetHasAlert(true));
    }

    private static MviHandleResult<ClinicalReminderMutation, ClinicalReminderEffect> HandleResolvePrimaryAlert()
    {
        return MviHandleResult.Mutations<ClinicalReminderMutation, ClinicalReminderEffect>(
            new ClinicalReminderMutation.SetPrimaryAlert("首要提醒已处理。"),
            new ClinicalReminderMutation.SetHasAlert(false));
    }
}
