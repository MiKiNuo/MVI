using System;
using System.Globalization;
using MiKiNuo.Mvi.Application.MVI.Effect;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片的统一副作用分发器。
/// 接收 CardEffect 后:
/// 1. 根据 source PageKey 在 <see cref="DashboardCardRegistry"/> 中查找同 SourceKey 组的兄弟卡片,
///    向每个兄弟 store 同步派发 <see cref="CardIntent.ApplyExternalUpdate"/>,实现组合页面内跨卡片数据流;
/// 2. 随后向 <see cref="IMviMediator"/> 发送 <see cref="DashboardComponentInteractionRequest"/>,
///    由容器侧 Mediator 路由把消息追加到父级 BusinessCompositePage 的 InteractionLog;
/// 3. 若 Effect 是 <see cref="CardEffect.RequestFormSubmission"/> 且来源卡是入院登记卡,
///    解析 FormValues 为 <see cref="Patient"/> 并写入 <see cref="IMviPatientRegistry"/>,
///    随后向同 SourceKey 组内全部 4 张卡片(含自身)派发 <see cref="CardIntent.ApplyPatientAdmitted"/>,
///    让床位总览 / 护理任务 / 病区风险 / 入院登记 4 张卡片同时显示新患者。
/// </summary>
public sealed class CardEffectDispatcher : IMviEffectDispatcher<CardEffect>
{
    private const string PrimaryActionKey = "主业务动作";
    private const string SecondaryActionKey = "辅助业务动作";
    private const string FormSubmissionActionKey = "表单提交";

    private readonly IMviMediator _mediator;
    private readonly IMviPatientRegistry _patientRegistry;
    private readonly PageKey _sourceKey;
    private readonly IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> _allStores;

    /// <summary>
    /// 初始化仪表板卡片副作用分发器。
    /// </summary>
    /// <param name="mediator">Mediator。</param>
    /// <param name="patientRegistry">跨卡片患者注册表。</param>
    /// <param name="sourceKey">本卡片对应的 PageKey,用于定位同组兄弟。</param>
    /// <param name="allStores">所有卡片的 store 字典。</param>
    public CardEffectDispatcher(
        IMviMediator mediator,
        IMviPatientRegistry patientRegistry,
        PageKey sourceKey,
        IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> allStores)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(patientRegistry);
        ArgumentNullException.ThrowIfNull(allStores);

        _mediator = mediator;
        _patientRegistry = patientRegistry;
        _sourceKey = sourceKey;
        _allStores = allStores;
    }

    /// <summary>
    /// 分发副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示异步分发过程的任务。</returns>
    public async ValueTask DispatchAsync(CardEffect effect, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is CardEffect.RequestPrimaryWorkflow primaryWorkflow)
        {
            await NotifySiblingsAsync(primaryWorkflow.ContextText, cancellationToken).ConfigureAwait(false);
            await SendAsync(primaryWorkflow.ContextText, PrimaryActionKey, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is CardEffect.RequestSecondaryWorkflow secondaryWorkflow)
        {
            await NotifySiblingsAsync(secondaryWorkflow.ContextText, cancellationToken).ConfigureAwait(false);
            await SendAsync(secondaryWorkflow.ContextText, SecondaryActionKey, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (effect is CardEffect.RequestFormSubmission formSubmission)
        {
            await HandleAdmissionFormSubmissionAsync(formSubmission, cancellationToken).ConfigureAwait(false);
            await NotifySiblingsAsync(formSubmission.ContextText, cancellationToken).ConfigureAwait(false);
            await SendAsync(formSubmission.ContextText, FormSubmissionActionKey, cancellationToken).ConfigureAwait(false);
            return;
        }
    }

    /// <summary>
    /// 处理入院登记表单提交:解析 Patient、写入注册表、派发 ApplyPatientAdmitted 给同组所有卡片。
    /// </summary>
    /// <param name="formSubmission">表单提交副作用。</param>
    /// <param name="cancellationToken">取消标记。</param>
    private async ValueTask HandleAdmissionFormSubmissionAsync(
        CardEffect.RequestFormSubmission formSubmission,
        CancellationToken cancellationToken)
    {
        if (_sourceKey != PageKey.AdmissionCoordinator)
        {
            return;
        }

        Patient? patient = TryParseAdmissionPatient(formSubmission.FormValues);
        if (patient is null)
        {
            return;
        }

        _patientRegistry.Register(patient);
        await NotifyAdmissionToGroupAsync(patient, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 把 FormValues 解析为 <see cref="Patient"/>。
    /// </summary>
    /// <param name="formValues">表单字段值集合。</param>
    /// <returns>解析成功返回 Patient;缺失必填字段返回 null。</returns>
    private static Patient? TryParseAdmissionPatient(IReadOnlyList<CardFormValueEntry> formValues)
    {
        ArgumentNullException.ThrowIfNull(formValues);

        Dictionary<string, string> lookup = new(StringComparer.Ordinal);
        foreach (CardFormValueEntry entry in formValues)
        {
            lookup.TryAdd(entry.Key, entry.Value);
        }

        if (!lookup.TryGetValue("PatientName", out string? name) || string.IsNullOrWhiteSpace(name)
            || !lookup.TryGetValue("AdmissionDiagnosis", out string? diagnosis) || string.IsNullOrWhiteSpace(diagnosis)
            || !lookup.TryGetValue("TargetBedNo", out string? bedNo) || string.IsNullOrWhiteSpace(bedNo))
        {
            return null;
        }

        int? age = null;
        if (lookup.TryGetValue("PatientAge", out string? ageText)
            && !string.IsNullOrWhiteSpace(ageText)
            && int.TryParse(ageText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedAge))
        {
            age = parsedAge;
        }

        string? nurseNote = null;
        if (lookup.TryGetValue("NurseNote", out string? note) && !string.IsNullOrWhiteSpace(note))
        {
            nurseNote = note;
        }

        return new Patient(
            Id: Guid.NewGuid(),
            Name: name,
            Age: age,
            Diagnosis: diagnosis,
            BedNo: bedNo,
            NurseNote: nurseNote,
            AdmittedAt: DateTimeOffset.Now);
    }

    /// <summary>
    /// 向同 SourceKey 组内的兄弟卡片 store 派发 <see cref="CardIntent.ApplyExternalUpdate"/>。
    /// 跳过自身(避免回环)。
    /// </summary>
    /// <param name="contextText">上下文文本。</param>
    /// <param name="cancellationToken">取消标记。</param>
    private async ValueTask NotifySiblingsAsync(string contextText, CancellationToken cancellationToken)
    {
        IReadOnlyList<PageKey> siblings = DashboardCardRegistry.GetSiblingKeys(_sourceKey);
        if (siblings.Count == 0)
        {
            return;
        }

        CardIntent intent = new CardIntent.ApplyExternalUpdate(contextText);
        foreach (PageKey siblingKey in siblings)
        {
            if (!_allStores.TryGetValue(siblingKey, out IMviStore<CardState, CardIntent, CardEffect>? store))
            {
                continue;
            }

            await store.DispatchAsync(intent, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 向同 SourceKey 组内全部卡片(含自身)派发 <see cref="CardIntent.ApplyPatientAdmitted"/>。
    /// </summary>
    /// <param name="patient">已注册的入院患者。</param>
    /// <param name="cancellationToken">取消标记。</param>
    private async ValueTask NotifyAdmissionToGroupAsync(Patient patient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patient);

        CardDefinition? source = DashboardCardRegistry.GetDefinition(_sourceKey);
        if (source is null)
        {
            return;
        }

        CardIntent intent = new CardIntent.ApplyPatientAdmitted(patient);
        foreach (KeyValuePair<PageKey, IMviStore<CardState, CardIntent, CardEffect>> pair in _allStores)
        {
            if (!CardDefinitionHasSourceKey(pair.Key, source.SourceKey))
            {
                continue;
            }

            await pair.Value.DispatchAsync(intent, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 判断指定 PageKey 是否属于指定 SourceKey 组。
    /// </summary>
    /// <param name="key">待检查的 PageKey。</param>
    /// <param name="sourceKey">来源组键。</param>
    /// <returns>属于该组返回 true;否则返回 false。</returns>
    private static bool CardDefinitionHasSourceKey(PageKey key, string sourceKey)
    {
        CardDefinition? definition = DashboardCardRegistry.GetDefinition(key);
        return definition is not null
            && string.Equals(definition.SourceKey, sourceKey, StringComparison.Ordinal);
    }

    private async ValueTask SendAsync(string contextText, string actionKey, CancellationToken cancellationToken)
    {
        await _mediator.SendComponentInteractionAsync(
            "Dashboard",
            "Card",
            actionKey,
            contextText,
            cancellationToken).ConfigureAwait(false);
    }
}
