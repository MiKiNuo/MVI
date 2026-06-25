using System;
using System.Collections.Generic;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalReminder;

/// <summary>
/// 表示临床提醒规约器。
/// </summary>
public sealed class ClinicalReminderReducer
    : MviReducerBase<ClinicalReminderState, ClinicalReminderIntent, ClinicalReminderEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<ClinicalReminderState, ClinicalReminderEffect> Reduce(
        ClinicalReminderState state,
        ClinicalReminderIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            ClinicalReminderIntent.LoadPatient loadPatient => ReduceLoadPatient(state, loadPatient),
            ClinicalReminderIntent.ResolvePrimaryAlert => MviReduceResult.State<ClinicalReminderState, ClinicalReminderEffect>(
                state with { PrimaryAlert = "首要提醒已处理。", HasAlert = false }),
            _ => MviReduceResult.State<ClinicalReminderState, ClinicalReminderEffect>(state),
        };
    }

    private static MviReduceResult<ClinicalReminderState, ClinicalReminderEffect> ReduceLoadPatient(
        ClinicalReminderState state,
        ClinicalReminderIntent.LoadPatient intent)
    {
        IReadOnlyList<string> alerts = intent.PatientName.Contains("胸闷", StringComparison.Ordinal)
            ? (IReadOnlyList<string>)["胸痛中心绿色通道评估。", "建议 10 分钟内完成心电图。", "核查阿司匹林禁忌。"]
            : ["核查过敏史。", "复核既往用药。", "完善生命体征记录。"];

        return MviReduceResult.State<ClinicalReminderState, ClinicalReminderEffect>(
            state with
            {
                PatientName = intent.PatientName,
                Alerts = alerts,
                PrimaryAlert = alerts[0],
                HasAlert = true,
            });
    }
}
