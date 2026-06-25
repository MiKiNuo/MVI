using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using MiKiNuo.Mvi.Tests.TestSupport;
using R3;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示仪表板卡片跨子组件数据流测试。
/// 验证一张卡片产生副作用时，CardEffectDispatcher 会同步派发 ApplyExternalUpdate 到同组的兄弟卡片。
/// 这是用户报告"组合式 MVI 子 view 不通信"症状的回归测试。
/// </summary>
public sealed class CardCrossComponentFlowTests
{
    /// <summary>
    /// 验证主动作在 Inpatient 组内会派发到三个兄弟卡片，且源卡片自身不接收自己的 ApplyExternalUpdate。
    /// </summary>
    [Test]
    public async Task PrimaryAction_Should_DispatchApplyExternalUpdateToSiblingCardsAsync()
    {
        CardCrossComponentHarness harness = CardCrossComponentHarness.CreateInpatientGroup();
        PageKey sourceKey = PageKey.BedOverview;
        string contextText = "床位总览：急诊转入住院";

        await harness.DispatchPrimaryAsync(sourceKey, contextText);

        await Assert.That(harness.GetActionLog(PageKey.AdmissionCoordinator)).Contains(contextText);
        await Assert.That(harness.GetActionLog(PageKey.NursingTaskBoard)).Contains(contextText);
        await Assert.That(harness.GetActionLog(PageKey.WardRiskPanel)).Contains(contextText);
        await Assert.That(harness.GetDetailText(sourceKey)).DoesNotContain(contextText);
    }

    /// <summary>
    /// 验证辅助动作在 Inpatient 组内会派发到三个兄弟卡片。
    /// </summary>
    [Test]
    public async Task SecondaryAction_Should_DispatchApplyExternalUpdateToSiblingCardsAsync()
    {
        CardCrossComponentHarness harness = CardCrossComponentHarness.CreateInpatientGroup();
        string contextText = "护理任务：升级护士长";

        await harness.DispatchSecondaryAsync(PageKey.NursingTaskBoard, contextText);

        await Assert.That(harness.GetActionLog(PageKey.BedOverview)).Contains(contextText);
        await Assert.That(harness.GetActionLog(PageKey.AdmissionCoordinator)).Contains(contextText);
        await Assert.That(harness.GetActionLog(PageKey.WardRiskPanel)).Contains(contextText);
    }

    /// <summary>
    /// 验证 Form Card 提交后同组兄弟卡片会收到 ApplyExternalUpdate。
    /// </summary>
    [Test]
    public async Task FormCardSubmit_Should_DispatchApplyExternalUpdateToSiblingCardsAsync()
    {
        CardCrossComponentHarness harness = CardCrossComponentHarness.CreateInpatientGroup();
        string contextText = "入院登记 已提交：PatientName=张三";

        await harness.DispatchFormSubmissionAsync(PageKey.AdmissionCoordinator, contextText);

        await Assert.That(harness.GetActionLog(PageKey.BedOverview)).Contains(contextText);
        await Assert.That(harness.GetActionLog(PageKey.NursingTaskBoard)).Contains(contextText);
        await Assert.That(harness.GetActionLog(PageKey.WardRiskPanel)).Contains(contextText);
    }

    /// <summary>
    /// 验证不同组的卡片之间不会互相派发 ApplyExternalUpdate。
    /// </summary>
    [Test]
    public async Task PrimaryAction_Should_NotDispatchAcrossDifferentSourceKeyGroupsAsync()
    {
        CardCrossComponentHarness inpatient = CardCrossComponentHarness.CreateInpatientGroup();
        CardCrossComponentHarness lab = CardCrossComponentHarness.CreateLabGroup();
        string labInitialDetail = lab.GetDetailText(PageKey.LabOrderComposer);

        await inpatient.DispatchPrimaryAsync(PageKey.BedOverview, "床位总览：急诊转入住院");

        await Assert.That(lab.GetDetailText(PageKey.LabOrderComposer)).IsEqualTo(labInitialDetail);
    }
}

/// <summary>
/// 仪表板卡片跨子组件派发的测试夹具。
/// 创建同组（按 SourceKey 划分）的多张卡片并装配共享的 sibling store 字典；提供主动作/辅助动作/表单提交的派发便捷方法。
/// </summary>
file sealed class CardCrossComponentHarness
{
    private readonly IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> _stores;
    private readonly Dictionary<PageKey, CardState> _latestStates;

    private CardCrossComponentHarness(IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> stores)
    {
        _stores = stores;
        _latestStates = new Dictionary<PageKey, CardState>(stores.Count);
        foreach (KeyValuePair<PageKey, IMviStore<CardState, CardIntent, CardEffect>> pair in stores)
        {
            _latestStates[pair.Key] = pair.Value.CurrentState;
            _ = pair.Value.States.Subscribe(snapshot => UpdateLatest(snapshot));
        }
    }

    /// <summary>
    /// 创建 Inpatient 组（床位总览 / 护理任务 / 病区风险 / 入院登记）的夹具实例。
    /// </summary>
    /// <returns>配置好的夹具。</returns>
    public static CardCrossComponentHarness CreateInpatientGroup()
    {
        return Create([PageKey.BedOverview, PageKey.NursingTaskBoard, PageKey.WardRiskPanel, PageKey.AdmissionCoordinator]);
    }

    /// <summary>
    /// 创建 Lab 组（医嘱开立 / 标本流转 / 危急值 / TAT）的夹具实例。
    /// </summary>
    /// <returns>配置好的夹具。</returns>
    public static CardCrossComponentHarness CreateLabGroup()
    {
        return Create([PageKey.LabOrderComposer, PageKey.SpecimenTracker, PageKey.CriticalValueMonitor, PageKey.LabTurnaroundBoard]);
    }

    private static CardCrossComponentHarness Create(IReadOnlyList<PageKey> keys)
    {
        Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> stores = new(keys.Count);
        foreach (PageKey key in keys)
        {
            CardDefinition definition = DashboardCardRegistry.GetDefinition(key)
                ?? throw new InvalidOperationException($"未注册 {key}");
            NoopCardEffectDispatcher storeDispatcher = new();
            MviStore<CardState, CardIntent, CardEffect> store = new(
                CardState.FromDefinition(definition),
                new CardIntentHandler(),
                new CardReducer(DashboardCardRegistry.All),
                storeDispatcher);
            stores[key] = store;
        }

        return new CardCrossComponentHarness(stores);
    }

    /// <summary>
    /// 获取指定 PageKey 卡片最近一次规约后的 ActionLog 文本。
    /// </summary>
    /// <param name="key">目标 PageKey。</param>
    /// <returns>ActionLog 文本；若不存在则返回空字符串。</returns>
    public string GetActionLog(PageKey key) => _latestStates.TryGetValue(key, out CardState? state) ? state.ActionLog : string.Empty;

    /// <summary>
    /// 获取指定 PageKey 卡片最近一次规约后的 DetailText 文本。
    /// </summary>
    /// <param name="key">目标 PageKey。</param>
    /// <returns>DetailText 文本；若不存在则返回空字符串。</returns>
    public string GetDetailText(PageKey key) => _latestStates.TryGetValue(key, out CardState? state) ? state.DetailText : string.Empty;

    /// <summary>
    /// 派发主动作（带上下文文本）到指定源卡片。
    /// </summary>
    /// <param name="source">源卡片 PageKey。</param>
    /// <param name="contextText">派发的上下文文本。</param>
    public async ValueTask DispatchPrimaryAsync(PageKey source, string contextText)
    {
        await DispatchAsync(source, new CardEffect.RequestPrimaryWorkflow(contextText));
    }

    /// <summary>
    /// 派发辅助动作（带上下文文本）到指定源卡片。
    /// </summary>
    /// <param name="source">源卡片 PageKey。</param>
    /// <param name="contextText">派发的上下文文本。</param>
    public async ValueTask DispatchSecondaryAsync(PageKey source, string contextText)
    {
        await DispatchAsync(source, new CardEffect.RequestSecondaryWorkflow(contextText));
    }

    /// <summary>
    /// 派发表单提交动作（带结构化 FormValues 与上下文文本）到指定源卡片。
    /// </summary>
    /// <param name="source">源卡片 PageKey。</param>
    /// <param name="formValues">结构化字段值集合，会被 EffectDispatcher 解析为 Patient。</param>
    /// <param name="contextText">派发的上下文文本。</param>
    public async ValueTask DispatchFormSubmissionAsync(
        PageKey source,
        IReadOnlyList<CardFormValueEntry> formValues,
        string contextText)
    {
        await DispatchAsync(source, new CardEffect.RequestFormSubmission(formValues, contextText));
    }

    /// <summary>
    /// 派发表单提交动作（仅上下文文本，无 FormValues）到指定源卡片。
    /// 兼容旧测试路径：缺少 FormValues 时 dispatcher 不会尝试解析 Patient。
    /// </summary>
    /// <param name="source">源卡片 PageKey。</param>
    /// <param name="contextText">派发的上下文文本。</param>
    public async ValueTask DispatchFormSubmissionAsync(PageKey source, string contextText)
    {
        await DispatchAsync(source, new CardEffect.RequestFormSubmission(Array.Empty<CardFormValueEntry>(), contextText));
    }

    private async ValueTask DispatchAsync(PageKey source, CardEffect effect)
    {
#pragma warning disable CA2000
        InMemoryPatientRegistry registry = new();
#pragma warning restore CA2000
        using IDisposable __ = registry;
        CardEffectDispatcher dispatcher = new(new NoopMediator(), registry, source, _stores);
        await dispatcher.DispatchAsync(effect, CancellationToken.None);
    }

    private void UpdateLatest(CardState snapshot)
    {
        _latestStates[snapshot.PageKey] = snapshot;
    }
}

/// <summary>
/// 仅作为构造 MviStore 用的占位 EffectDispatcher：不实际派发任何副作用；
/// 测试中所有副作用都是通过 harness 手动构造的 <see cref="CardEffectDispatcher"/> 直接派发。
/// </summary>
file sealed class NoopCardEffectDispatcher : MiKiNuo.Mvi.Application.MVI.Effect.IMviEffectDispatcher<CardEffect>
{
    /// <summary>
    /// 空实现：直接返回完成的 ValueTask，不派发任何副作用。
    /// </summary>
    /// <param name="effect">副作用（忽略）。</param>
    /// <param name="cancellationToken">取消标记（忽略）。</param>
    /// <returns>已完成的任务。</returns>
    public ValueTask DispatchAsync(CardEffect effect, CancellationToken cancellationToken = default)
    {
        _ = effect;
        _ = cancellationToken;
        return ValueTask.CompletedTask;
    }
}
