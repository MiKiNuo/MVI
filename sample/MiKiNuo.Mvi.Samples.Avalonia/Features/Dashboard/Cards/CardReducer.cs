using System;
using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using ZLinq;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片规约器。
/// </summary>
public sealed partial class CardReducer
    : MviReducerBase<CardState, CardIntent, CardEffect>
{
    private readonly IReadOnlyDictionary<PageKey, CardDefinition> _definitions;

    /// <summary>
    /// 初始化仪表板卡片规约器。
    /// </summary>
    /// <param name="definitions">卡片定义字典。</param>
    public CardReducer(IReadOnlyDictionary<PageKey, CardDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        _definitions = definitions;
    }

    /// <summary>
    /// 处理主操作意图。
    /// </summary>
    [MviReduce(typeof(CardIntent.ExecutePrimaryAction))]
    private MviReduceResult<CardState, CardEffect> HandleExecutePrimaryAction(
        CardState state,
        CardIntent.ExecutePrimaryAction intent,
        IMviBusinessResult? result)
    {
        CardState newState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟卡片。",
        };
        return WithEffect(
            newState,
            new CardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>
    /// 处理次操作意图。
    /// </summary>
    [MviReduce(typeof(CardIntent.ExecuteSecondaryAction))]
    private MviReduceResult<CardState, CardEffect> HandleExecuteSecondaryAction(
        CardState state,
        CardIntent.ExecuteSecondaryAction intent,
        IMviBusinessResult? result)
    {
        CardState newState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。",
        };
        return WithEffect(
            newState,
            new CardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>
    /// 处理外部更新意图。
    /// </summary>
    [MviReduce(typeof(CardIntent.ApplyExternalUpdate))]
    private MviReduceResult<CardState, CardEffect> HandleApplyExternalUpdate(
        CardState state,
        CardIntent.ApplyExternalUpdate intent,
        IMviBusinessResult? result)
    {
        CardState newState = state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟卡片更新：{intent.Message}",
        };
        return Unchanged(newState);
    }

    /// <summary>
    /// 处理入院通知意图。
    /// </summary>
    [MviReduce(typeof(CardIntent.ApplyPatientAdmitted))]
    private MviReduceResult<CardState, CardEffect> HandleApplyPatientAdmitted(
        CardState state,
        CardIntent.ApplyPatientAdmitted intent,
        IMviBusinessResult? result)
    {
        Patient patient = intent.Patient;
        CardState newState = state with
        {
            RecentAdmittedPatient = patient,
            StatusText = $"已接收入院：{patient.Name}（床 {patient.BedNo}）",
            DetailText = BuildPatientDetailLine(patient),
            ActionLog = $"兄弟卡片入院通知：{patient.Name}（{patient.Diagnosis}）入住 {patient.BedNo}。",
        };
        return Unchanged(newState);
    }

    private string BuildPatientDetailLine(Patient patient)
    {
        string ageFragment = patient.Age.HasValue ? $"{patient.Age}岁" : "年龄未填";
        string noteFragment = string.IsNullOrEmpty(patient.NurseNote) ? string.Empty : $" 备注：{patient.NurseNote}";
        return $"{patient.Name}（{ageFragment}，{patient.Diagnosis}）于 {patient.AdmittedAt.LocalDateTime:HH:mm} 入院，目标床位 {patient.BedNo}。{noteFragment}".TrimEnd();
    }

    /// <summary>
    /// 处理表单字段变更。
    /// </summary>
    [MviReduce(typeof(CardIntent.SetFormField))]
    private MviReduceResult<CardState, CardEffect> HandleSetFormField(
        CardState state,
        CardIntent.SetFormField intent,
        IMviBusinessResult? result)
    {
        CardDefinition? definition = ResolveDefinition(state);
        if (definition is null || !definition.IsFormCard)
        {
            return Unchanged(state);
        }

        CardState nextState = state.WithFormValue(intent.Key, intent.Value);
        if (definition.Validator is null)
        {
            CardState newState = nextState with { FormValues = nextState.FormValues };
            return Unchanged(newState);
        }

        (bool _, string statusText, string actionLog) = definition.Validator(nextState.FormValues);
        CardState newStateWithValidation = nextState with
        {
            FormValues = nextState.FormValues,
            StatusText = statusText,
            ActionLog = actionLog,
        };
        return Unchanged(newStateWithValidation);
    }

    /// <summary>
    /// 处理表单提交意图。
    /// </summary>
    [MviReduce(typeof(CardIntent.SubmitForm))]
    private MviReduceResult<CardState, CardEffect> HandleSubmitForm(
        CardState state,
        CardIntent.SubmitForm intent,
        IMviBusinessResult? result)
    {
        CardDefinition? definition = ResolveDefinition(state);
        if (definition is null || !definition.IsFormCard || definition.Validator is null)
        {
            return Unchanged(state);
        }

        (bool canSubmit, string statusText, string actionLog) = definition.Validator(state.FormValues);
        if (!canSubmit)
        {
            CardState newState = state with
            {
                StatusText = statusText,
                ActionLog = actionLog,
                FormErrorMessage = statusText,
            };
            return Unchanged(newState);
        }

        string contextText = state.FormValues.AsValueEnumerable()
            .Select(static value => $"{value.Key}={value.Value}")
            .JoinToString("；");
        string summaryText = $"{definition.Title} 已提交：{contextText}";
        CardState submittedState = state with
        {
            StatusText = $"{definition.Title} 已提交，等待兄弟卡片处理",
            ActionLog = $"{definition.SourceDisplayName} 提交 -> {contextText} -> 通过 Mediator 分发。",
        };
        return WithEffect(
            submittedState,
            new CardEffect.RequestFormSubmission(state.FormValues, summaryText));
    }

    /// <summary>
    /// 处理床位筛选变更。
    /// </summary>
    [MviReduce(typeof(CardIntent.SetBedFilter))]
    private MviReduceResult<CardState, CardEffect> HandleSetBedFilter(
        CardState state,
        CardIntent.SetBedFilter intent,
        IMviBusinessResult? result)
    {
        if (state.PageKey != PageKey.BedOverview)
        {
            return Unchanged(state);
        }

        if (state.CurrentBedFilter == intent.Filter)
        {
            return Unchanged(state);
        }

        int count = BedCatalog.CountCombined(intent.Filter, state.SelectedBedTypes, state.SelectedBedStatuses);
        CardState newState = state with
        {
            CurrentBedFilter = intent.Filter,
            FilteredBedCount = count,
            ActionLog = $"床位筛选已切换为：{BedFilterOption.All.Single(option => option.Value == intent.Filter).DisplayName}，匹配 {count} 张。",
        };
        return Unchanged(newState);
    }

    /// <summary>
    /// 处理床位类型切换。
    /// </summary>
    [MviReduce(typeof(CardIntent.ToggleBedType))]
    private MviReduceResult<CardState, CardEffect> HandleToggleBedType(
        CardState state,
        CardIntent.ToggleBedType intent,
        IMviBusinessResult? result)
    {
        if (state.PageKey != PageKey.BedOverview)
        {
            return Unchanged(state);
        }

        HashSet<BedType> nextTypes = new(state.SelectedBedTypes);
        if (intent.IsSelected)
        {
            nextTypes.Add(intent.BedType);
        }
        else
        {
            nextTypes.Remove(intent.BedType);
        }

        if (nextTypes.SetEquals(state.SelectedBedTypes))
        {
            return Unchanged(state);
        }

        int count = BedCatalog.CountCombined(state.CurrentBedFilter, nextTypes, state.SelectedBedStatuses);
        string verb = intent.IsSelected ? "加入" : "移除";
        CardState newState = state with
        {
            SelectedBedTypes = nextTypes,
            FilteredBedCount = count,
            ActionLog = $"床位类型筛选：{BedRecordRow.ToTypeText(intent.BedType)} 已{verb}；当前匹配 {count} 张。",
        };
        return Unchanged(newState);
    }

    /// <summary>
    /// 处理床位状态切换。
    /// </summary>
    [MviReduce(typeof(CardIntent.ToggleBedStatus))]
    private MviReduceResult<CardState, CardEffect> HandleToggleBedStatus(
        CardState state,
        CardIntent.ToggleBedStatus intent,
        IMviBusinessResult? result)
    {
        if (state.PageKey != PageKey.BedOverview)
        {
            return Unchanged(state);
        }

        HashSet<BedStatus> nextStatuses = new(state.SelectedBedStatuses);
        if (intent.IsSelected)
        {
            nextStatuses.Add(intent.BedStatus);
        }
        else
        {
            nextStatuses.Remove(intent.BedStatus);
        }

        if (nextStatuses.SetEquals(state.SelectedBedStatuses))
        {
            return Unchanged(state);
        }

        int count = BedCatalog.CountCombined(state.CurrentBedFilter, state.SelectedBedTypes, nextStatuses);
        string verb = intent.IsSelected ? "加入" : "移除";
        CardState newState = state with
        {
            SelectedBedStatuses = nextStatuses,
            FilteredBedCount = count,
            ActionLog = $"床位状态筛选：{BedRecordRow.ToStatusText(intent.BedStatus)} 已{verb}；当前匹配 {count} 张。",
        };
        return Unchanged(newState);
    }

    private CardDefinition? ResolveDefinition(CardState state)
    {
        return _definitions.TryGetValue(state.PageKey, out CardDefinition? definition) ? definition : null;
    }
}
