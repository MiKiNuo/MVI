using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using TUnit.Assertions;
using TUnit.Core;
using ZLinq;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 CardIntentHandler 端到端冒烟测试。
/// </summary>
public sealed class CardReducerSmokeTest
{
    /// <summary>
    /// 验证从 CardDefinition 构造的初始状态字段与定义一致。
    /// </summary>
    [Test]
    public async Task FromDefinitionSimpleCardShouldPopulateStateAsync()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.BedOverview)!;
        CardState state = CardState.FromDefinition(definition);

        await Assert.That(state.PageKey).IsEqualTo(PageKey.BedOverview);
        await Assert.That(state.Title).IsEqualTo(definition.Title);
        await Assert.That(state.MainValue).IsEqualTo(definition.MainValue);
        await Assert.That(state.FormValues).IsEmpty();
    }

    /// <summary>
    /// 验证 Form Card 的初始状态填入 FormValues 初值。
    /// </summary>
    [Test]
    public async Task FromDefinitionFormCardShouldPopulateFormValuesAsync()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.AdmissionCoordinator)!;
        CardState state = CardState.FromDefinition(definition);

        await Assert.That(state.FormValues).IsNotEmpty();
        await Assert.That(state.FormValues.Count).IsEqualTo(definition.FormFields!.Count);
    }

    /// <summary>
    /// 验证 Registry 注册了全部 PageKey：
    /// 4 个业务域各 4 张共 16 张 + 架构验证中心 4 张指标卡 = 20 张。
    /// <para>
    /// 2026-06-06 新增架构验证中心 4 张 PageKey（Middleware/Reuse/Mediator/Effect）后
    /// 由 16 增至 20。后续如果新增业务域 / 演示卡，必须同步更新此处的期望值，
    /// 否则 PageKey 与 CardDefinition 的注册就漏配了。
    /// </para>
    /// </summary>
    [Test]
    public async Task RegistryShouldHaveAll20PageKeysAsync()
    {
        PageKey[] allKeys = DashboardCardRegistry.All.Keys.ToArray();
        await Assert.That(allKeys.Length).IsEqualTo(20);
    }

    /// <summary>
    /// 验证 FormFields 列表引用在多次 RebuildDerivedProperties 调用后保持稳定（焦点不丢失的根因修复）。
    /// 这是 "TextBox 每次输入都丢失焦点" 症状的回归测试：
    /// 之前 RebuildDerivedProperties 每次都 .Select(field => new CardFormFieldEntry(...))，
    /// 导致 IReadOnlyList&lt;CardFormFieldEntry&gt; 引用变化，ItemsControl 重建容器 → TextBox 焦点丢失。
    /// </summary>
    [Test]
    public async Task CardViewModel_FormFieldsList_Should_BeReferenceStableAcrossRebuildsAsync()
    {
        IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> siblingStores =
            new Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>>();
#pragma warning disable CA2000
        InMemoryPatientRegistry patientRegistry = new();
#pragma warning restore CA2000
        using IDisposable _patientRegistryDispose = patientRegistry;
        CardEffectDispatcher dispatcher = new(new NoopMediator(), patientRegistry, PageKey.AdmissionCoordinator, siblingStores);
        PageKey key = PageKey.AdmissionCoordinator;
        CardDefinition definition = DashboardCardRegistry.GetDefinition(key)!;
        IReadOnlyList<CardFormField> fields = definition.FormFields!;
        IReadOnlyList<CardFormValueEntry> initialValues = fields
            .Select(static field => new CardFormValueEntry(field.Key, field.InitialValue))
            .ToArray();
        CardState initial = new(
            definition.Key,
            definition.SourceKey,
            definition.SourceDisplayName,
            definition.Title,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            definition.PrimaryActionText,
            definition.SecondaryActionText,
            true,
            true,
            string.Empty,
            initialValues,
            RecentAdmittedPatient: null,
            CurrentBedFilter: BedFilter.All,
            FilteredBedCount: 0,
            SelectedBedTypes: new HashSet<BedType>(),
            SelectedBedStatuses: new HashSet<BedStatus>());
        IMviStore<CardState, CardIntent, CardEffect> store = new MviStore<CardState, CardIntent, CardEffect>(
            initial,
            new CardIntentHandler(),
            new CardReducer(DashboardCardRegistry.All),
            dispatcher);
        using IDisposable __ = store;
#pragma warning disable CA2000
        CardViewModel cardViewModel = new(store);
#pragma warning restore CA2000
        using IDisposable _ = cardViewModel;
        IReadOnlyList<CardFormFieldEntry> firstList = cardViewModel.FormFields;
        await Assert.That(firstList.Count).IsEqualTo(fields.Count);
        CardFormFieldEntry firstFieldEntry = firstList[0];

        await store.DispatchAsync(new CardIntent.SetFormField(fields[0].Key, "新值"));
        IReadOnlyList<CardFormFieldEntry> secondList = cardViewModel.FormFields;

        await Assert.That(ReferenceEquals(secondList, firstList)).IsTrue();
        await Assert.That(ReferenceEquals(secondList[0], firstFieldEntry)).IsTrue();
        await Assert.That(secondList[0].Value).IsEqualTo("新值");
    }

    /// <summary>
    /// 验证 Form Card 的必填校验在缺少必填字段时拒绝提交。
    /// </summary>
    [Test]
    public async Task AdmissionCoordinatorValidatorShouldRejectEmptyRequiredFieldsAsync()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.AdmissionCoordinator)!;
        CardState state = CardState.FromDefinition(definition);
        IReadOnlyList<CardFormValueEntry> emptyValues = definition.FormFields!
            .Select(static f => new CardFormValueEntry(f.Key, string.Empty))
            .ToArray();

        (bool canSubmit, string _, string _) = definition.Validator!(emptyValues);

        await Assert.That(canSubmit).IsFalse();
        await Assert.That(definition.IsFormCard).IsTrue();
        await Assert.That(state.FormValues).IsNotEmpty();
    }

    /// <summary>
    /// 验证 SetFormField 意图在 Form Card 上更新对应键的值。
    /// </summary>
    [Test]
    public async Task CardReducerSetFormFieldShouldUpdateValueAsync()
    {
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.AdmissionCoordinator)!;
        CardState initial = CardState.FromDefinition(definition);
        string firstKey = definition.FormFields![0].Key;
        string firstInitial = initial.FormValues[0].Value;
        using MviStore<CardState, CardIntent, CardEffect> store = new(
            initial,
            new CardIntentHandler(),
            new CardReducer(DashboardCardRegistry.All),
            new NoopEffectDispatcher<CardEffect>());

        await store.DispatchAsync(new CardIntent.SetFormField(firstKey, "TEST-VALUE"));

        await Assert.That(store.CurrentState.FormValues[0].Key).IsEqualTo(firstKey);
        await Assert.That(store.CurrentState.FormValues[0].Value).IsEqualTo("TEST-VALUE");
        await Assert.That(firstInitial).IsNotEqualTo("TEST-VALUE");
    }

    /// <summary>
    /// 验证 CardIntentHandler 的有参构造函数支持外部注入卡片定义字典，
    /// 处理器不再直接依赖 DashboardCardRegistry 全局静态，
    /// 切断 "处理器是纯函数" 的破窗。
    /// </summary>
    [Test]
    public async Task CardReducer_ConstructorInjection_Should_NotTouchGlobalRegistryAsync()
    {
        // 构造一个孤立的"虚假"注册表，包含一个与 DashboardCardRegistry 无关的 Form Card。
        PageKey probeKey = PageKey.AdmissionCoordinator;
        CardFormField[] fields =
        [
            new("ProbeKey", "探测字段", "示例", string.Empty, isRequired: true),
        ];
        CardDefinition probeDefinition = new(
            Key: probeKey,
            SourceKey: "Probe",
            SourceDisplayName: "Probe",
            Title: "Probe",
            MainValue: string.Empty,
            StatusText: string.Empty,
            DetailText: string.Empty,
            PrimaryActionText: "主",
            SecondaryActionText: "次",
            FormFields: fields,
            RequiredFormFields: new[] { "ProbeKey" },
            Validator: BuildAlwaysValidProbeValidator());

        IReadOnlyDictionary<PageKey, CardDefinition> isolated = new Dictionary<PageKey, CardDefinition>
        {
            [probeKey] = probeDefinition,
        };

        CardState state = CardState.FromDefinition(probeDefinition);
        using MviStore<CardState, CardIntent, CardEffect> store = new(
            state,
            new CardIntentHandler(),
            new CardReducer(isolated),
            new NoopEffectDispatcher<CardEffect>());

        await store.DispatchAsync(new CardIntent.SetFormField("ProbeKey", "Hello"));

        await Assert.That(store.CurrentState.FormValues[0].Value).IsEqualTo("Hello");
        await Assert.That(store.CurrentState.StatusText).IsEqualTo("Probe 资料已完整，可以提交");
    }

    private static Func<IReadOnlyList<CardFormValueEntry>, (bool CanSubmit, string StatusText, string ActionLog)> BuildAlwaysValidProbeValidator()
    {
        return values => (true, "Probe 资料已完整，可以提交", "Probe 提交 -> 探测流程。");
    }
}
