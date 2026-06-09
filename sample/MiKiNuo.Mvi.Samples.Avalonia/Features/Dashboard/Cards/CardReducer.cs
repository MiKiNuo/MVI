using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using ZLinq;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片的统一归约器。
/// 处理器使用 <c>[MviReduce]</c> 标注的私有方法，由 MVI 源代码生成器发出 dispatch 逻辑。
/// 内部根据 state.PageKey 在构造函数注入的卡片定义字典中查找 <see cref="CardDefinition"/>，
/// 从而根据 16 个不同卡片执行差异化状态更新。设计为非泛型：1 个 reducer 覆盖 16 个 PageKey
/// （详见 .gsd/DECISIONS.md 2026-06-02 第 5 轮 grill）。
/// </summary>
/// <remarks>
/// 卡片定义字典通过构造函数注入而非直接读取 <see cref="DashboardCardRegistry"/>，从而让 reducer 自身
/// 保持纯函数（无全局可变状态依赖）。默认无参构造函数从全局注册表填充，便于现有调用方保持兼容；
/// 测试代码与自定义组合可通过有参构造函数注入替代字典。
/// </remarks>
public sealed partial class CardReducer
    : MviReducerBase<CardState, CardIntent, CardEffect>
{
    private readonly IReadOnlyDictionary<PageKey, CardDefinition> _definitions;

    /// <summary>
    /// 使用 <see cref="DashboardCardRegistry.All"/> 作为卡片定义来源初始化归约器。
    /// </summary>
    public CardReducer()
        : this(DashboardCardRegistry.All)
    {
    }

    /// <summary>
    /// 使用显式传入的卡片定义字典初始化归约器。
    /// </summary>
    /// <param name="definitions">按 <see cref="PageKey"/> 索引的卡片定义字典。</param>
    public CardReducer(IReadOnlyDictionary<PageKey, CardDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        _definitions = definitions;
    }

    /// <summary>
    /// 根据状态携带的 <see cref="PageKey"/> 在注入的字典中查找卡片定义。
    /// 找不到时返回 <c>null</c>，由各 reducer 自行决定是否短路。
    /// </summary>
    /// <param name="state">当前状态。</param>
    /// <returns>对应的卡片定义或 null。</returns>
    private CardDefinition? ResolveDefinition(CardState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return _definitions.TryGetValue(state.PageKey, out CardDefinition? definition) ? definition : null;
    }

    /// <summary>处理 ExecutePrimaryAction。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.ExecutePrimaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);

        CardState nextState = state with
        {
            StatusText = $"已发起：{state.PrimaryActionText}",
            ActionLog = $"{state.Title} -> {state.PrimaryActionText} -> 等待 Mediator 协调父页面和兄弟卡片。",
        };

        return MviReduceResult.StateAndEffect<CardState, CardEffect>(
            nextState,
            new CardEffect.RequestPrimaryWorkflow($"{state.Title}：{state.PrimaryActionText}"));
    }

    /// <summary>处理 ExecuteSecondaryAction。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.ExecuteSecondaryAction intent)
    {
        ArgumentNullException.ThrowIfNull(state);

        CardState nextState = state with
        {
            StatusText = $"已发起：{state.SecondaryActionText}",
            ActionLog = $"{state.Title} -> {state.SecondaryActionText} -> 等待 Mediator 协调副作用。",
        };

        return MviReduceResult.StateAndEffect<CardState, CardEffect>(
            nextState,
            new CardEffect.RequestSecondaryWorkflow($"{state.Title}：{state.SecondaryActionText}"));
    }

    /// <summary>处理 ApplyExternalUpdate。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.ApplyExternalUpdate intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        return MviReduceResult.State<CardState, CardEffect>(state with
        {
            DetailText = intent.Message,
            ActionLog = $"收到父页面或兄弟卡片更新：{intent.Message}",
        });
    }

    /// <summary>处理 ApplyPatientAdmitted：把新患者写入 RecentAdmittedPatient 并刷新状态文本。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.ApplyPatientAdmitted intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        Patient patient = intent.Patient;
        return MviReduceResult.State<CardState, CardEffect>(state with
        {
            RecentAdmittedPatient = patient,
            StatusText = $"已接收入院：{patient.Name}（床 {patient.BedNo}）",
            DetailText = BuildPatientDetailLine(patient),
            ActionLog = $"兄弟卡片入院通知：{patient.Name}（{patient.Diagnosis}）入住 {patient.BedNo}。",
        });
    }

    private static string BuildPatientDetailLine(Patient patient)
    {
        string ageFragment = patient.Age.HasValue ? $"{patient.Age}岁" : "年龄未填";
        string noteFragment = string.IsNullOrEmpty(patient.NurseNote) ? string.Empty : $" 备注：{patient.NurseNote}";
        return $"{patient.Name}（{ageFragment}，{patient.Diagnosis}）于 {patient.AdmittedAt.LocalDateTime:HH:mm} 入院，目标床位 {patient.BedNo}。{noteFragment}".TrimEnd();
    }

    /// <summary>处理 SetBedFilter：仅 BedOverview 卡片生效；其他卡片短路返回。其他卡片触发时 reducer 静默忽略，不污染兄弟卡片状态。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.SetBedFilter intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (state.PageKey != PageKey.BedOverview)
        {
            return MviReduceResult.State<CardState, CardEffect>(state);
        }

        if (state.CurrentBedFilter == intent.Filter)
        {
            return MviReduceResult.State<CardState, CardEffect>(state);
        }

        int count = QueryCombinedCount(intent.Filter, state.SelectedBedTypes, state.SelectedBedStatuses);
        return MviReduceResult.State<CardState, CardEffect>(state with
        {
            CurrentBedFilter = intent.Filter,
            FilteredBedCount = count,
            ActionLog = $"床位筛选已切换为：{BedFilterOption.All.Single(option => option.Value == intent.Filter).DisplayName}，匹配 {count} 张。",
        });
    }

    /// <summary>处理 ToggleBedType：仅 BedOverview 卡片生效；其他卡片短路返回。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.ToggleBedType intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (state.PageKey != PageKey.BedOverview)
        {
            return MviReduceResult.State<CardState, CardEffect>(state);
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
            return MviReduceResult.State<CardState, CardEffect>(state);
        }

        int count = QueryCombinedCount(state.CurrentBedFilter, nextTypes, state.SelectedBedStatuses);
        string verb = intent.IsSelected ? "加入" : "移除";
        return MviReduceResult.State<CardState, CardEffect>(state with
        {
            SelectedBedTypes = nextTypes,
            FilteredBedCount = count,
            ActionLog = $"床位类型筛选：{BedRecordRow.ToTypeText(intent.BedType)} 已{verb}；当前匹配 {count} 张。",
        });
    }

    /// <summary>处理 ToggleBedStatus：仅 BedOverview 卡片生效；其他卡片短路返回。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.ToggleBedStatus intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (state.PageKey != PageKey.BedOverview)
        {
            return MviReduceResult.State<CardState, CardEffect>(state);
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
            return MviReduceResult.State<CardState, CardEffect>(state);
        }

        int count = QueryCombinedCount(state.CurrentBedFilter, state.SelectedBedTypes, nextStatuses);
        string verb = intent.IsSelected ? "加入" : "移除";
        return MviReduceResult.State<CardState, CardEffect>(state with
        {
            SelectedBedStatuses = nextStatuses,
            FilteredBedCount = count,
            ActionLog = $"床位状态筛选：{BedRecordRow.ToStatusText(intent.BedStatus)} 已{verb}；当前匹配 {count} 张。",
        });
    }

    /// <summary>
    /// 合并三种筛选维度（ComboBox 单选 + CheckBox 多选类型 + CheckBox 多选状态）后统计匹配条数。
    /// 行为与 <c>CardViewModel.QueryCombined</c> 保持一致；reducer 不能复用 ViewModel 私有方法，因此单独复制。
    /// </summary>
    /// <param name="filter">ComboBox 单选筛选维度。</param>
    /// <param name="typeFilter">CheckBox 多选床位类型集合（空集合 = 不限类型）。</param>
    /// <param name="statusFilter">CheckBox 多选床位状态集合（空集合 = 不限状态）。</param>
    /// <returns>三个维度都满足的床位条数。</returns>
    private static int QueryCombinedCount(
        BedFilter filter,
        IReadOnlySet<BedType> typeFilter,
        IReadOnlySet<BedStatus> statusFilter)
    {
        ArgumentNullException.ThrowIfNull(typeFilter);
        ArgumentNullException.ThrowIfNull(statusFilter);

        BedStatus? filterStatus = filter == BedFilter.All ? null : ToStatusFromFilter(filter);
        bool filterTypeEmpty = typeFilter.Count == 0;
        bool filterStatusEmpty = statusFilter.Count == 0;
        if (filterStatus is null && filterTypeEmpty && filterStatusEmpty)
        {
            return BedCatalog.TotalCount;
        }

        int count = 0;
        foreach (BedRecord record in BedCatalog.All)
        {
            bool typeOk = filterTypeEmpty || typeFilter.Contains(record.Type);
            bool statusOk = true;
            if (filterStatus is not null)
            {
                statusOk = record.Status == filterStatus.Value;
            }

            if (statusOk && statusFilter.Count > 0)
            {
                statusOk = statusFilter.Contains(record.Status);
            }

            if (typeOk && statusOk)
            {
                count++;
            }
        }

        return count;
    }

    private static BedStatus ToStatusFromFilter(BedFilter filter)
    {
        return filter switch
        {
            BedFilter.Open => BedStatus.Open,
            BedFilter.Occupied => BedStatus.Occupied,
            BedFilter.Locked => BedStatus.Locked,
            BedFilter.Isolated => BedStatus.Isolated,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "未知的 BedFilter 值。"),
        };
    }

    /// <summary>处理 SetFormField。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.SetFormField intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        CardDefinition? definition = ResolveDefinition(state);
        if (definition is null || !definition.IsFormCard)
        {
            return MviReduceResult.State<CardState, CardEffect>(state);
        }

        CardState nextState = state.WithFormValue(intent.Key, intent.Value);
        if (definition.Validator is null)
        {
            return MviReduceResult.State<CardState, CardEffect>(nextState);
        }

        (bool _, string statusText, string actionLog) = definition.Validator(nextState.FormValues);
        return MviReduceResult.State<CardState, CardEffect>(nextState with
        {
            StatusText = statusText,
            ActionLog = actionLog,
        });
    }

    /// <summary>处理 SubmitForm。</summary>
    [MviReduce]
    private MviReduceResult<CardState, CardEffect> Reduce(
        CardState state,
        CardIntent.SubmitForm intent)
    {
        ArgumentNullException.ThrowIfNull(state);

        CardDefinition? definition = ResolveDefinition(state);
        if (definition is null || !definition.IsFormCard || definition.Validator is null)
        {
            return MviReduceResult.State<CardState, CardEffect>(state);
        }

        (bool canSubmit, string statusText, string actionLog) = definition.Validator(state.FormValues);
        if (!canSubmit)
        {
            return MviReduceResult.State<CardState, CardEffect>(state with
            {
                StatusText = statusText,
                ActionLog = actionLog,
                FormErrorMessage = statusText,
            });
        }

        string contextText = state.FormValues.AsValueEnumerable()
            .Select(static value => $"{value.Key}={value.Value}")
            .JoinToString("；");

        CardState nextState = state with
        {
            StatusText = $"{definition.Title} 已提交，等待兄弟卡片处理",
            ActionLog = $"{definition.SourceDisplayName} 提交 -> {contextText} -> 通过 Mediator 分发。",
        };

        return MviReduceResult.StateAndEffect<CardState, CardEffect>(
            nextState,
            new CardEffect.RequestFormSubmission(state.FormValues, contextText));
    }
}
