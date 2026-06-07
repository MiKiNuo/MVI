using MiKiNuo.Mvi.Application.MVI.Reducer;
using MiKiNuo.Mvi.Domain.MVI.Reducer;
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
