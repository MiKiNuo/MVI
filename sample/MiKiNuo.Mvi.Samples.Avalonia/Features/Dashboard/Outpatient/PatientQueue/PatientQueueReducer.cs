using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列规约器。
/// </summary>
public sealed class PatientQueueReducer
    : MviReducerBase<PatientQueueState, PatientQueueIntent, PatientQueueEffect>
{
    /// <summary>
    /// 将意图规约为新状态与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <returns>规约结果。</returns>
    public override MviReduceResult<PatientQueueState, PatientQueueEffect> Reduce(
        PatientQueueState state,
        PatientQueueIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return intent switch
        {
            PatientQueueIntent.CallNext => HandleCallNext(state),
            _ => MviReduceResult.State<PatientQueueState, PatientQueueEffect>(state),
        };
    }

    private static MviReduceResult<PatientQueueState, PatientQueueEffect> HandleCallNext(
        PatientQueueState state)
    {
        int nextIndex = Math.Min(state.CurrentIndex + 1, state.Patients.Count - 1);
        string patientName = state.Patients[nextIndex];
        int remaining = Math.Max(0, state.Patients.Count - nextIndex - 1);

        PatientQueueState newState = state with
        {
            CurrentIndex = nextIndex,
            SelectedPatientName = patientName,
            QueueSummary = $"当前接诊：{patientName}；剩余候诊 {remaining} 人。",
            CanCallNext = remaining > 0,
        };

        return MviReduceResult.StateAndEffect<PatientQueueState, PatientQueueEffect>(
            newState,
            new PatientQueueEffect.PatientSelected(patientName));
    }
}
