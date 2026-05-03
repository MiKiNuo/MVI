using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列规约器。
/// </summary>
public sealed partial class PatientQueueReducer
    : MviReducerBase<PatientQueueState, PatientQueueIntent, PatientQueueEffect>
{
    /// <summary>
    /// 处理接诊下一位患者意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">接诊下一位患者意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<PatientQueueState, PatientQueueEffect> Reduce(
        PatientQueueState state,
        PatientQueueIntent.CallNext intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        int nextIndex = Math.Min(state.CurrentIndex + 1, state.Patients.Count - 1);
        string patientName = state.Patients[nextIndex];
        int remaining = Math.Max(0, state.Patients.Count - nextIndex - 1);
        PatientQueueState nextState = state with
        {
            CurrentIndex = nextIndex,
            SelectedPatientName = patientName,
            QueueSummary = $"当前接诊：{patientName}；剩余候诊 {remaining} 人。",
            CanCallNext = remaining > 0
        };

        return MviReduceResult.StateAndEffect<PatientQueueState, PatientQueueEffect>(
            nextState,
            new PatientQueueEffect.PatientSelected(patientName));
    }
}
