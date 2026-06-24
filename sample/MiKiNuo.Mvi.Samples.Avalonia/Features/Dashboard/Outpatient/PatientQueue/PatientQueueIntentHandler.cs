using MiKiNuo.Mvi.Application.MVI.IntentHandler;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.PatientQueue;

/// <summary>
/// 表示门诊队列意图处理器。
/// </summary>
public sealed class PatientQueueIntentHandler
    : IMviIntentHandler<PatientQueueState, PatientQueueIntent, PatientQueueMutation, PatientQueueEffect>
{
    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<PatientQueueMutation, PatientQueueEffect>> HandleAsync(
        PatientQueueState state,
        PatientQueueIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<PatientQueueMutation, PatientQueueEffect> result = intent switch
        {
            PatientQueueIntent.CallNext => HandleCallNext(state),
            _ => MviHandleResult.Empty<PatientQueueMutation, PatientQueueEffect>(),
        };
        return new ValueTask<MviHandleResult<PatientQueueMutation, PatientQueueEffect>>(result);
    }

    private static MviHandleResult<PatientQueueMutation, PatientQueueEffect> HandleCallNext(
        PatientQueueState state)
    {
        int nextIndex = Math.Min(state.CurrentIndex + 1, state.Patients.Count - 1);
        string patientName = state.Patients[nextIndex];
        int remaining = Math.Max(0, state.Patients.Count - nextIndex - 1);

        PatientQueueMutation[] mutations = new PatientQueueMutation[]
        {
            new PatientQueueMutation.SetCurrentIndex(nextIndex),
            new PatientQueueMutation.SetSelectedPatientName(patientName),
            new PatientQueueMutation.SetQueueSummary($"当前接诊：{patientName}；剩余候诊 {remaining} 人。"),
            new PatientQueueMutation.SetCanCallNext(remaining > 0),
        };
        PatientQueueEffect[] effects = new PatientQueueEffect[]
        {
            new PatientQueueEffect.PatientSelected(patientName),
        };
        return MviHandleResult.MutationsAndEffects<PatientQueueMutation, PatientQueueEffect>(mutations, effects);
    }
}
