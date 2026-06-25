using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;

/// <summary>
/// 表示可复用患者检索意图处理器。
/// </summary>
public sealed class PatientSearchIntentHandler
    : IMviIntentHandler<PatientSearchState, PatientSearchIntent, PatientSearchEffect>
{
    /// <summary>
    /// 处理意图并产生动作副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>动作副作用集合。</returns>
    public ValueTask<IReadOnlyList<PatientSearchEffect>> HandleAsync(
        PatientSearchState state,
        PatientSearchIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        IReadOnlyList<PatientSearchEffect> effects = intent switch
        {
            PatientSearchIntent.SelectFirstPatient when state.CanSelectPatient =>
                new PatientSearchEffect[]
                {
                    new PatientSearchEffect.RequestPatientContext(
                        state.PageKey,
                        state.SelectedPatientName,
                        state.SelectedPatientNo),
                },
            _ => Array.Empty<PatientSearchEffect>(),
        };

        return new ValueTask<IReadOnlyList<PatientSearchEffect>>(effects);
    }
}
