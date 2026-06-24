using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using ZLinq;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片意图处理器。
/// </summary>
public sealed class CardIntentHandler
    : IMviIntentHandler<CardState, CardIntent, CardMutation, CardEffect>
{
    private readonly IReadOnlyDictionary<PageKey, CardDefinition> _definitions;

    /// <summary>
    /// 初始化仪表板卡片意图处理器。
    /// </summary>
    /// <param name="definitions">卡片定义字典。</param>
    public CardIntentHandler(IReadOnlyDictionary<PageKey, CardDefinition> definitions)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    /// <summary>
    /// 处理意图产生变更与副作用。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <param name="intent">用户意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理结果。</returns>
    public ValueTask<MviHandleResult<CardMutation, CardEffect>> HandleAsync(
        CardState state,
        CardIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        MviHandleResult<CardMutation, CardEffect> result = intent switch
        {
            CardIntent.ExecutePrimaryAction => HandleExecutePrimaryAction(state),
            CardIntent.ExecuteSecondaryAction => HandleExecuteSecondaryAction(state),
            CardIntent.ApplyExternalUpdate applyExternalUpdate => HandleApplyExternalUpdate(state, applyExternalUpdate),
            CardIntent.ApplyPatientAdmitted applyPatientAdmitted => HandleApplyPatientAdmitted(state, applyPatientAdmitted),
            CardIntent.SetFormField setFormField => HandleSetFormField(state, setFormField),
            CardIntent.SubmitForm => HandleSubmitForm(state),
            CardIntent.SetBedFilter setBedFilter => HandleSetBedFilter(state, setBedFilter),
            CardIntent.ToggleBedType toggleBedType => HandleToggleBedType(state, toggleBedType),
            CardIntent.ToggleBedStatus toggleBedStatus => HandleToggleBedStatus(state, toggleBedStatus),
            _ => MviHandleResult.Empty<CardMutation, CardEffect>(),
        };
        return new ValueTask<MviHandleResult<CardMutation, CardEffect>>(result);
    }

    private static MviHandleResult<CardMutation, CardEffect> HandleExecutePrimaryAction(CardState state)
    {
        CardMutation[] mutations =
        {
            new CardMutation.SetStatusText($"已发起：{state.PrimaryActionText}"),
            new CardMutation.SetActionLog($"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟卡片。"),
        };
        CardEffect[] effects = { new CardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}") };
        return MviHandleResult.MutationsAndEffects<CardMutation, CardEffect>(mutations, effects);
    }

    private static MviHandleResult<CardMutation, CardEffect> HandleExecuteSecondaryAction(CardState state)
    {
        CardMutation[] mutations =
        {
            new CardMutation.SetStatusText($"已发起：{state.SecondaryActionText}"),
            new CardMutation.SetActionLog($"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。"),
        };
        CardEffect[] effects = { new CardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}") };
        return MviHandleResult.MutationsAndEffects<CardMutation, CardEffect>(mutations, effects);
    }

    private static MviHandleResult<CardMutation, CardEffect> HandleApplyExternalUpdate(
        CardState state,
        CardIntent.ApplyExternalUpdate intent)
    {
        CardMutation[] mutations =
        {
            new CardMutation.SetDetailText(intent.Message),
            new CardMutation.SetActionLog($"收到父页面或兄弟卡片更新：{intent.Message}"),
        };
        return MviHandleResult.Mutations<CardMutation, CardEffect>(mutations);
    }

    private static MviHandleResult<CardMutation, CardEffect> HandleApplyPatientAdmitted(
        CardState state,
        CardIntent.ApplyPatientAdmitted intent)
    {
        Patient patient = intent.Patient;
        CardMutation[] mutations =
        {
            new CardMutation.SetRecentAdmittedPatient(patient),
            new CardMutation.SetStatusText($"已接收入院：{patient.Name}（床 {patient.BedNo}）"),
            new CardMutation.SetDetailText(BuildPatientDetailLine(patient)),
            new CardMutation.SetActionLog($"兄弟卡片入院通知：{patient.Name}（{patient.Diagnosis}）入住 {patient.BedNo}。"),
        };
        return MviHandleResult.Mutations<CardMutation, CardEffect>(mutations);
    }

    private static string BuildPatientDetailLine(Patient patient)
    {
        string ageFragment = patient.Age.HasValue ? $"{patient.Age}岁" : "年龄未填";
        string noteFragment = string.IsNullOrEmpty(patient.NurseNote) ? string.Empty : $" 备注：{patient.NurseNote}";
        return $"{patient.Name}（{ageFragment}，{patient.Diagnosis}）于 {patient.AdmittedAt.LocalDateTime:HH:mm} 入院，目标床位 {patient.BedNo}。{noteFragment}".TrimEnd();
    }

    private MviHandleResult<CardMutation, CardEffect> HandleSetFormField(
        CardState state,
        CardIntent.SetFormField intent)
    {
        CardDefinition? definition = ResolveDefinition(state);
        if (definition is null || !definition.IsFormCard)
        {
            return MviHandleResult.Empty<CardMutation, CardEffect>();
        }

        CardState nextState = state.WithFormValue(intent.Key, intent.Value);
        if (definition.Validator is null)
        {
            return MviHandleResult.Mutations<CardMutation, CardEffect>(
                new CardMutation.SetFormValues(nextState.FormValues));
        }

        (bool _, string statusText, string actionLog) = definition.Validator(nextState.FormValues);
        CardMutation[] mutations =
        {
            new CardMutation.SetFormValues(nextState.FormValues),
            new CardMutation.SetStatusText(statusText),
            new CardMutation.SetActionLog(actionLog),
        };
        return MviHandleResult.Mutations<CardMutation, CardEffect>(mutations);
    }

    private MviHandleResult<CardMutation, CardEffect> HandleSubmitForm(CardState state)
    {
        CardDefinition? definition = ResolveDefinition(state);
        if (definition is null || !definition.IsFormCard || definition.Validator is null)
        {
            return MviHandleResult.Empty<CardMutation, CardEffect>();
        }

        (bool canSubmit, string statusText, string actionLog) = definition.Validator(state.FormValues);
        if (!canSubmit)
        {
            CardMutation[] mutations =
            {
                new CardMutation.SetStatusText(statusText),
                new CardMutation.SetActionLog(actionLog),
                new CardMutation.SetFormErrorMessage(statusText),
            };
            return MviHandleResult.Mutations<CardMutation, CardEffect>(mutations);
        }

        string contextText = state.FormValues.AsValueEnumerable()
            .Select(static value => $"{value.Key}={value.Value}")
            .JoinToString("；");

        CardMutation[] submitMutations =
        {
            new CardMutation.SetStatusText($"{definition.Title} 已提交，等待兄弟卡片处理"),
            new CardMutation.SetActionLog($"{definition.SourceDisplayName} 提交 -> {contextText} -> 通过 Mediator 分发。"),
        };
        CardEffect[] effects = { new CardEffect.RequestFormSubmission(state.FormValues, contextText) };
        return MviHandleResult.MutationsAndEffects<CardMutation, CardEffect>(submitMutations, effects);
    }

    private static MviHandleResult<CardMutation, CardEffect> HandleSetBedFilter(
        CardState state,
        CardIntent.SetBedFilter intent)
    {
        if (state.PageKey != PageKey.BedOverview)
        {
            return MviHandleResult.Empty<CardMutation, CardEffect>();
        }

        if (state.CurrentBedFilter == intent.Filter)
        {
            return MviHandleResult.Empty<CardMutation, CardEffect>();
        }

        int count = BedCatalog.CountCombined(intent.Filter, state.SelectedBedTypes, state.SelectedBedStatuses);
        CardMutation[] mutations =
        {
            new CardMutation.SetCurrentBedFilter(intent.Filter),
            new CardMutation.SetFilteredBedCount(count),
            new CardMutation.SetActionLog($"床位筛选已切换为：{BedFilterOption.All.Single(option => option.Value == intent.Filter).DisplayName}，匹配 {count} 张。"),
        };
        return MviHandleResult.Mutations<CardMutation, CardEffect>(mutations);
    }

    private static MviHandleResult<CardMutation, CardEffect> HandleToggleBedType(
        CardState state,
        CardIntent.ToggleBedType intent)
    {
        if (state.PageKey != PageKey.BedOverview)
        {
            return MviHandleResult.Empty<CardMutation, CardEffect>();
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
            return MviHandleResult.Empty<CardMutation, CardEffect>();
        }

        int count = BedCatalog.CountCombined(state.CurrentBedFilter, nextTypes, state.SelectedBedStatuses);
        string verb = intent.IsSelected ? "加入" : "移除";
        CardMutation[] mutations =
        {
            new CardMutation.SetSelectedBedTypes(nextTypes),
            new CardMutation.SetFilteredBedCount(count),
            new CardMutation.SetActionLog($"床位类型筛选：{BedRecordRow.ToTypeText(intent.BedType)} 已{verb}；当前匹配 {count} 张。"),
        };
        return MviHandleResult.Mutations<CardMutation, CardEffect>(mutations);
    }

    private static MviHandleResult<CardMutation, CardEffect> HandleToggleBedStatus(
        CardState state,
        CardIntent.ToggleBedStatus intent)
    {
        if (state.PageKey != PageKey.BedOverview)
        {
            return MviHandleResult.Empty<CardMutation, CardEffect>();
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
            return MviHandleResult.Empty<CardMutation, CardEffect>();
        }

        int count = BedCatalog.CountCombined(state.CurrentBedFilter, state.SelectedBedTypes, nextStatuses);
        string verb = intent.IsSelected ? "加入" : "移除";
        CardMutation[] mutations =
        {
            new CardMutation.SetSelectedBedStatuses(nextStatuses),
            new CardMutation.SetFilteredBedCount(count),
            new CardMutation.SetActionLog($"床位状态筛选：{BedRecordRow.ToStatusText(intent.BedStatus)} 已{verb}；当前匹配 {count} 张。"),
        };
        return MviHandleResult.Mutations<CardMutation, CardEffect>(mutations);
    }

    private CardDefinition? ResolveDefinition(CardState state)
    {
        return _definitions.TryGetValue(state.PageKey, out CardDefinition? definition) ? definition : null;
    }
}
