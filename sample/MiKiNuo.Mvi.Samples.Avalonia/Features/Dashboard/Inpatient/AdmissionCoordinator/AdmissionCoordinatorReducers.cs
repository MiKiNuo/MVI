using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.AdmissionCoordinator;

/// <summary>
/// 表示入院流程 MVI 规约器。
/// </summary>
public sealed partial class AdmissionCoordinatorReducer
    : MviReducerBase<AdmissionCoordinatorState, AdmissionCoordinatorIntent, AdmissionCoordinatorEffect>
{
    /// <summary>
    /// 处理患者姓名变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">患者姓名变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AdmissionCoordinatorState, AdmissionCoordinatorEffect> Reduce(
        AdmissionCoordinatorState state,
        AdmissionCoordinatorIntent.ChangePatientName intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(RefreshFormState(state with
        {
            PatientName = intent.PatientName
        }));
    }

    /// <summary>
    /// 处理患者年龄变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">患者年龄变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AdmissionCoordinatorState, AdmissionCoordinatorEffect> Reduce(
        AdmissionCoordinatorState state,
        AdmissionCoordinatorIntent.ChangePatientAge intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(RefreshFormState(state with
        {
            PatientAge = intent.PatientAge
        }));
    }

    /// <summary>
    /// 处理入院诊断变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">入院诊断变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AdmissionCoordinatorState, AdmissionCoordinatorEffect> Reduce(
        AdmissionCoordinatorState state,
        AdmissionCoordinatorIntent.ChangeAdmissionDiagnosis intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(RefreshFormState(state with
        {
            AdmissionDiagnosis = intent.AdmissionDiagnosis
        }));
    }

    /// <summary>
    /// 处理目标床号变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">目标床号变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AdmissionCoordinatorState, AdmissionCoordinatorEffect> Reduce(
        AdmissionCoordinatorState state,
        AdmissionCoordinatorIntent.ChangeTargetBedNo intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(RefreshFormState(state with
        {
            TargetBedNo = intent.TargetBedNo
        }));
    }

    /// <summary>
    /// 处理护士交接备注变更意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">护士交接备注变更意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AdmissionCoordinatorState, AdmissionCoordinatorEffect> Reduce(
        AdmissionCoordinatorState state,
        AdmissionCoordinatorIntent.ChangeNurseNote intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(RefreshFormState(state with
        {
            NurseNote = intent.NurseNote
        }));
    }

    /// <summary>
    /// 处理提交入院登记表单意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">提交入院登记表单意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AdmissionCoordinatorState, AdmissionCoordinatorEffect> Reduce(
        AdmissionCoordinatorState state,
        AdmissionCoordinatorIntent.SubmitAdmissionForm intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        AdmissionCoordinatorState refreshedState = RefreshFormState(state);
        if (!refreshedState.CanConfirmAdmission)
        {
            return MviReduceResult.State<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(refreshedState with
            {
                ActionLog = "入院登记校验失败：患者姓名、入院诊断、目标床号为必填项。"
            });
        }

        string contextText = $"患者={refreshedState.PatientName}；年龄={refreshedState.PatientAge}；诊断={refreshedState.AdmissionDiagnosis}；床号={refreshedState.TargetBedNo}；交接={refreshedState.NurseNote}";
        AdmissionCoordinatorState nextState = refreshedState with
        {
            StatusText = "已提交入院登记，等待床位、护理和风险组件处理",
            ActionLog = $"护士提交入院登记 -> {contextText} -> 通过 Mediator 分发给兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(
            nextState,
            new AdmissionCoordinatorEffect.RequestAdmissionRegistration(contextText));
    }

    /// <summary>
    /// 处理主业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">主业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AdmissionCoordinatorState, AdmissionCoordinatorEffect> Reduce(
        AdmissionCoordinatorState state,
        AdmissionCoordinatorIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        AdmissionCoordinatorState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟 MVI。"
        };

        return MviReduceResult.StateAndEffect<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(
            nextState,
            new AdmissionCoordinatorEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>
    /// 处理辅助业务动作意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">辅助业务动作意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AdmissionCoordinatorState, AdmissionCoordinatorEffect> Reduce(
        AdmissionCoordinatorState state,
        AdmissionCoordinatorIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        AdmissionCoordinatorState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。"
        };

        return MviReduceResult.StateAndEffect<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(
            nextState,
            new AdmissionCoordinatorEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">外部更新意图。</param>
    /// <returns>规约结果。</returns>
    [MviReduce]
    private MviReduceResult<AdmissionCoordinatorState, AdmissionCoordinatorEffect> Reduce(
        AdmissionCoordinatorState state,
        AdmissionCoordinatorIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<AdmissionCoordinatorState, AdmissionCoordinatorEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟 MVI 更新：{intent.Message}"
        });
    }

    private static AdmissionCoordinatorState RefreshFormState(AdmissionCoordinatorState state)
    {
        bool canConfirm = !string.IsNullOrWhiteSpace(state.PatientName)
            && !string.IsNullOrWhiteSpace(state.AdmissionDiagnosis)
            && !string.IsNullOrWhiteSpace(state.TargetBedNo);

        return state with
        {
            CanConfirmAdmission = canConfirm,
            StatusText = canConfirm ? "入院登记资料已完整，可以提交" : "请补齐患者姓名、诊断和床号",
            ActionLog = $"正在录入入院登记：患者={state.PatientName}，诊断={state.AdmissionDiagnosis}，床号={state.TargetBedNo}。"
        };
    }
}
