using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using TUnit.Assertions;
using TUnit.Core;
namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示仪表板"入院登记 → 床位总览 / 护理任务 / 病区风险"端到端流程测试。
/// 验证：用户报告"提交入院登记后，住院床位页面看不到患者信息"症状的回归测试。
/// 一旦 CardEffectDispatcher 链路漏派 ApplyPatientAdmitted 或 reducer 漏写 state，
/// 3 张兄弟卡片将看不到新患者，本测试会立即失败。
/// </summary>
public sealed class CardAdmissionFlowTests
{
    /// <summary>
    /// 验证：提交入院登记后，4 张住院卡片（床位总览 / 护理任务 / 病区风险 / 入院登记）的
    /// CurrentState.RecentAdmittedPatient 均不为 null，且 BedNo 等于表单填写的目标床号。
    /// </summary>
    [Test]
    public async Task AdmissionFormSubmit_PopulatesRecentAdmittedPatient_AcrossAllInpatientCardsAsync()
    {
        using CardAdmissionHarness harness = CardAdmissionHarness.CreateInpatientGroup();
        IReadOnlyList<CardFormValueEntry> formValues = new CardFormValueEntry[]
        {
            new("PatientName", "张三"),
            new("PatientAge", "68"),
            new("AdmissionDiagnosis", "急性心衰"),
            new("TargetBedNo", "A12-08"),
            new("NurseNote", "过敏史：青霉素"),
        };

        await harness.FillAdmissionFormAsync(formValues);
        await harness.SubmitAdmissionFormAsync();

        PageKey[] inpatientKeys = [PageKey.BedOverview, PageKey.NursingTaskBoard, PageKey.WardRiskPanel, PageKey.AdmissionCoordinator];
        foreach (PageKey key in inpatientKeys)
        {
            CardState state = harness.GetCurrentState(key);
            await Assert.That(state.RecentAdmittedPatient)
                .IsNotNull()
                .Because($"住院卡片 {key} 必须显示新入院患者");
            await Assert.That(state.RecentAdmittedPatient!.Name).IsEqualTo("张三");
            await Assert.That(state.RecentAdmittedPatient.BedNo).IsEqualTo("A12-08");
            await Assert.That(state.RecentAdmittedPatient.Diagnosis).IsEqualTo("急性心衰");
        }
    }

    /// <summary>
    /// 验证：跨 SourceKey 组（Lab）不会因为 Inpatient 入院登记而收到患者通知。
    /// </summary>
    [Test]
    public async Task AdmissionFormSubmit_DoesNotLeakPatient_ToOtherSourceKeyGroupsAsync()
    {
        using CardAdmissionHarness harness = CardAdmissionHarness.CreateInpatientGroup();
        IMviStore<CardState, CardIntent, CardEffect> labStore = harness.AttachLab(PageKey.LabOrderComposer);
        IReadOnlyList<CardFormValueEntry> formValues = new CardFormValueEntry[]
        {
            new("PatientName", "张三"),
            new("AdmissionDiagnosis", "急性心衰"),
            new("TargetBedNo", "A12-08"),
        };

        await harness.FillAdmissionFormAsync(formValues);
        await harness.SubmitAdmissionFormAsync();

        await Assert.That(labStore.CurrentState.RecentAdmittedPatient).IsNull();
    }

    /// <summary>
    /// 验证：CardViewModel 暴露 <c>RecentPatient</c> 属性，与 <c>state.RecentAdmittedPatient</c> 双向同步。
    /// 床位总览卡收到兄弟入院登记通知后，RecentPatient 必须自动等于新患者。
    /// </summary>
    [Test]
    public async Task BedOverviewViewModel_ExposesRecentPatient_AfterAdmissionNotificationAsync()
    {
        using CardAdmissionHarness harness = CardAdmissionHarness.CreateInpatientGroup();
        using CardViewModel bedOverview = new(harness.GetStore(PageKey.BedOverview));

        IReadOnlyList<CardFormValueEntry> formValues = new CardFormValueEntry[]
        {
            new("PatientName", "张三"),
            new("PatientAge", "68"),
            new("AdmissionDiagnosis", "急性心衰"),
            new("TargetBedNo", "A12-08"),
        };

        await harness.FillAdmissionFormAsync(formValues);
        await harness.SubmitAdmissionFormAsync();
        await Task.Delay(50);

        await Assert.That(bedOverview.RecentPatient).IsNotNull();
        await Assert.That(bedOverview.RecentPatient!.Name).IsEqualTo("张三");
        await Assert.That(bedOverview.RecentPatient.BedNo).IsEqualTo("A12-08");
    }
}

/// <summary>
/// 入院登记端到端流程测试夹具。构造 4 张住院卡片 store（带真实 CardEffectDispatcher 与 InMemoryPatientRegistry），
/// 并把它们的状态流捕获到字典中以供断言。
/// </summary>
file sealed class CardAdmissionHarness : IDisposable
{
    private readonly IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> _stores;
    private readonly List<IMviStore<CardState, CardIntent, CardEffect>> _attachedForAssertion = new();
    private readonly List<InMemoryPatientRegistry> _registries = new();
    private bool _disposed;

    private CardAdmissionHarness(IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> stores)
    {
        _stores = stores;
    }

    /// <summary>
    /// 创建 4 张住院卡片（床位总览 / 护理任务 / 病区风险 / 入院登记）的真实 store 集合。
    /// </summary>
    /// <returns>配置好的夹具。</returns>
    public static CardAdmissionHarness CreateInpatientGroup()
    {
        PageKey[] keys = [PageKey.BedOverview, PageKey.NursingTaskBoard, PageKey.WardRiskPanel, PageKey.AdmissionCoordinator];
        return Create(keys);
    }

    private static CardAdmissionHarness Create(IReadOnlyList<PageKey> keys)
    {
        CardAdmissionHarness harness = new(new Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>>(keys.Count));
        harness._registries.Add(new InMemoryPatientRegistry());
        InMemoryPatientRegistry registry = harness._registries[0];
        Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> mutableStores = (Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>>)harness._stores;
        foreach (PageKey key in keys)
        {
            CardDefinition definition = DashboardCardRegistry.GetDefinition(key)
                ?? throw new InvalidOperationException($"未注册 {key}");
            MviStore<CardState, CardIntent, CardEffect> store = new(
                CardState.FromDefinition(definition),
                new CardReducer(),
                new CardEffectDispatcher(new NoopMediator(), registry, key, mutableStores));
            mutableStores[key] = store;
        }

        return harness;
    }

    /// <summary>
    /// 创建一张 Lab 卡片 store（独立的 dispatcher / registry），仅用于隔离断言。
    /// 不会注入 Inpatient 组的 store 字典，因此不会被 Inpatient 的派发影响。
    /// </summary>
    /// <param name="key">Lab 组 PageKey。</param>
    /// <returns>新增的 Lab 卡片 store。</returns>
    public IMviStore<CardState, CardIntent, CardEffect> AttachLab(PageKey key)
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(key)
            ?? throw new InvalidOperationException($"未注册 {key}");
        _registries.Add(new InMemoryPatientRegistry());
        InMemoryPatientRegistry registry = _registries[^1];
        Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> isolatedStores = new(1)
        {
            [key] = null!,
        };
        MviStore<CardState, CardIntent, CardEffect> store = new(
            CardState.FromDefinition(definition),
            new CardReducer(),
            new CardEffectDispatcher(new NoopMediator(), registry, key, isolatedStores));
        isolatedStores[key] = store;
        _attachedForAssertion.Add(store);
        return store;
    }

    /// <summary>
    /// 获取指定卡片最新状态（基于 store.States 流的最新快照）。
    /// </summary>
    /// <param name="key">PageKey。</param>
    /// <returns>最新 CardState。</returns>
    public CardState GetCurrentState(PageKey key)
    {
        return _stores[key].CurrentState;
    }

    /// <summary>
    /// 获取指定 PageKey 的 store，供 VM 构造 / 直接断言使用。
    /// </summary>
    /// <param name="key">PageKey。</param>
    /// <returns>store 实例。</returns>
    public IMviStore<CardState, CardIntent, CardEffect> GetStore(PageKey key)
    {
        return _stores[key];
    }

    /// <summary>
    /// 把表单字段值写入"入院登记 MVI"卡片的 state.FormValues。
    /// 走 SetFormField intent 路径，与生产 UX 行为一致。
    /// </summary>
    /// <param name="formValues">结构化表单字段值集合。</param>
    public async ValueTask FillAdmissionFormAsync(IReadOnlyList<CardFormValueEntry> formValues)
    {
        IMviStore<CardState, CardIntent, CardEffect> admissionStore = _stores[PageKey.AdmissionCoordinator];
        foreach (CardFormValueEntry entry in formValues)
        {
            await admissionStore.DispatchAsync(
                new CardIntent.SetFormField(entry.Key, entry.Value),
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 派发"入院登记 MVI"卡片的 SubmitForm intent。Store 内部 reducer → effect → dispatcher 链路
    /// 会完成 PatientRegistry 写入 + 同组 4 张卡片派发 ApplyPatientAdmitted。
    /// </summary>
    public async ValueTask SubmitAdmissionFormAsync()
    {
        await _stores[PageKey.AdmissionCoordinator].DispatchAsync(
            new CardIntent.SubmitForm(),
            CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (InMemoryPatientRegistry registry in _registries)
        {
            registry.Dispose();
        }
    }
}

/// <summary>
/// 内部测试辅助 Mediator：仅返回 TResponse 默认实例，不与兄弟卡片产生联动。
/// </summary>
file sealed class NoopMediator : IMviMediator
{
    /// <summary>
    /// 始终返回 TResponse 默认实例；不派发任何请求。
    /// </summary>
    /// <typeparam name="TRequest">请求类型。</typeparam>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">请求对象（忽略）。</param>
    /// <param name="cancellationToken">取消标记（忽略）。</param>
    /// <returns>默认构造的响应实例（可能为 null，测试不消费）。</returns>
    public ValueTask<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        _ = request;
        _ = cancellationToken;
        return new ValueTask<TResponse>((TResponse)default!);
    }
}
