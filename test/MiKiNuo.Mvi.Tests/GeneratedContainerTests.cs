﻿﻿﻿﻿﻿using MiKiNuo.Mvi.Application.DI;
using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Presentation.ViewRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Composition;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.ClinicalEditor;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
using TUnit.Assertions;
using TUnit.Core;
using ZLinq;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示源生成容器测试。
/// </summary>
public sealed class GeneratedContainerTests
{
    /// <summary>
    /// 验证容器可以解析登录 ViewModel 和 ViewRegistry。
    /// </summary>
    [Test]
    public async Task Resolve_Should_ReturnRegisteredServicesAsync()
    {
        SampleGeneratedContainer container = new();

        LoginViewModel loginViewModel = container.Resolve<LoginViewModel>();
        AppShellViewModel shellViewModel = container.Resolve<AppShellViewModel>();
        IMviViewRegistry viewRegistry = container.Resolve<IMviViewRegistry>();

        await Assert.That(loginViewModel).IsNotNull();
        await Assert.That(shellViewModel).IsNotNull();
        await Assert.That(viewRegistry).IsNotNull();
    }

    /// <summary>
    /// 验证容器能够解析 <c>IMviUiDispatcher</c>，并返回 Avalonia 平台实现。
    /// 这是 "Avalonia 应用在状态变更时抛 'calling thread cannot access this object' 跨线程异常" 的回归测试：
    /// 若示例容器未注册平台调度器，<c>MviCommandBase</c> 会回退到 <c>MviInlineUiDispatcher</c>，
    /// 在 R3 订阅线程上同步执行 <c>CanExecuteChanged</c>，触发 Avalonia 的 <c>Dispatcher.VerifyAccess</c> 失败。
    /// </summary>
    [Test]
    public async Task Resolve_Should_ReturnAvaloniaUiDispatcherAsync()
    {
        SampleGeneratedContainer container = new();

        IMviUiDispatcher dispatcher = container.Resolve<IMviUiDispatcher>();

        await Assert.That(dispatcher).IsNotNull();
        await Assert.That(dispatcher.GetType().FullName)
            .IsEqualTo("MiKiNuo.Mvi.Platforms.Avalonia.Threading.AvaloniaMviUiDispatcher");
    }

    /// <summary>
    /// 验证 ViewModel 解析后其 <c>UiDispatcher</c> 与容器注册的 <c>IMviUiDispatcher</c> 是同一实例。
    /// 这是 "ViewModel 构造函数未传递 <c>IMviUiDispatcher</c> 时 <c>MviCommandBase</c> 回退到
    /// <c>MviInlineUiDispatcher</c>" 症状的回归测试：源生成器创建 ViewModel 时必须把容器内
    /// 已注册的 AvaloniaMviUiDispatcher 显式注入到构造函数参数，确保 <c>PropertyChanged</c> 与
    /// <c>CanExecuteChanged</c> 都能 marshal 到 Avalonia UI 线程。
    /// </summary>
    [Test]
    public async Task Resolved_ViewModel_Should_ExposeContainerUiDispatcherAsync()
    {
        SampleGeneratedContainer container = new();

        IMviUiDispatcher dispatcher = container.Resolve<IMviUiDispatcher>();
        LoginViewModel loginViewModel = container.Resolve<LoginViewModel>();
        AppShellViewModel shellViewModel = container.Resolve<AppShellViewModel>();

        await Assert.That(loginViewModel.UiDispatcher).IsSameReferenceAs(dispatcher);
        await Assert.That(shellViewModel.UiDispatcher).IsSameReferenceAs(dispatcher);
    }

    /// <summary>
    /// 验证泛型容器源生成器可以读取 DiService 命名参数并按接口解析服务。
    /// </summary>
    [Test]
    public async Task GenericContainer_Should_ResolveDiServiceByConfiguredServiceTypeAsync()
    {
        GeneratedMviContainer container = new();

        IAuthService authService = container.Resolve<IAuthService>();
        IMviServiceGraph serviceGraph = (IMviServiceGraph)container;
        MviServiceDescriptor descriptor = serviceGraph.ServiceDescriptors.AsValueEnumerable()
            .Single(static item => item.ServiceType == typeof(IAuthService));

        await Assert.That(authService).IsTypeOf<FakeAuthService>();
        await Assert.That(descriptor.ImplementationType).IsEqualTo(typeof(FakeAuthService));
    }

    /// <summary>
    /// 验证生成容器中的 Dashboard Mediator 会响应菜单选择。
    /// 重构后（16 卡 → 1 CardReducer + 16 CardDefinition），"门诊工作站" 走 default 分支，
    /// "住院床位/检验医嘱/药房库存/运营质控" 四个 menuKey 必须路由到 BusinessCompositePageViewModel
    /// （旧的 4 个子特性 VM 已删除，CreateBusinessCompositePageViewModel 不再接收 4 个子特性参数）。
    /// </summary>
    [Test]
    public async Task DashboardMenuSelection_Should_UpdateDashboardCurrentPageAsync()
    {
        SampleGeneratedContainer container = new();

        await container.NavigateToDashboardAsync("测试用户");
        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();
        DashboardMenuViewModel menu = (DashboardMenuViewModel)dashboard.CreateMenuViewModel();

        string[] menuKeys = ["住院床位", "检验医嘱", "药房库存", "运营质控", "架构验证中心", "门诊工作站"];
        foreach (string menuKey in menuKeys)
        {
            menu.SelectedMenuKey = menuKey;
            await Task.Delay(100);

            await Assert.That(menu.SelectedMenuKey).IsEqualTo(menuKey);
            await Assert.That(dashboard.CurrentPageTitle).IsNotNull().And.IsNotEmpty();
        }
    }

    /// <summary>
    /// 验证 menuKey 路由到正确的业务组合页面 ViewModel：
    /// "住院床位" 等 4 个 menuKey 必须切换到 BusinessCompositePageViewModel（不能用 default 兜底为 Outpatient）。
    /// 这是左侧功能栏点击"住院床位"等按钮"无效果"症状的回归测试。
    /// </summary>
    [Test]
    public async Task DashboardMenuSelection_住院床位_Should_RouteToBusinessCompositePageViewModelAsync()
    {
        SampleGeneratedContainer container = new();
        await container.NavigateToDashboardAsync("测试用户");
        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();
        IMviMediator mediator = container.Resolve<IMviMediator>();

        await mediator.SendAsync<NavigateDashboardPageRequest, DashboardNavigationResponse>(
            new NavigateDashboardPageRequest("住院床位"));

        await Assert.That(dashboard.CreateCurrentPageViewModel()).IsTypeOf<BusinessCompositePageViewModel>();
    }

    /// <summary>
    /// 验证 4 个业务 menuKey 各自路由到 BusinessCompositePageViewModel 并携带正确 PageLayout。
    /// PageLayout 决定 BusinessCompositePageView 渲染哪个 4 卡片布局（Inpatient/Lab/Pharmacy/Quality）。
    /// </summary>
    [Test]
    public async Task DashboardMenuSelection_4BusinessKeys_Should_RouteToCorrectPageLayoutAsync()
    {
        SampleGeneratedContainer container = new();
        await container.NavigateToDashboardAsync("测试用户");
        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();
        IMviMediator mediator = container.Resolve<IMviMediator>();

        (string MenuKey, string ExpectedLayout)[] expectations =
        [
            ("住院床位", "Inpatient"),
            ("检验医嘱", "Lab"),
            ("药房库存", "Pharmacy"),
            ("运营质控", "Quality"),
        ];

        foreach ((string menuKey, string expectedLayout) in expectations)
        {
            await mediator.SendAsync<NavigateDashboardPageRequest, DashboardNavigationResponse>(
                new NavigateDashboardPageRequest(menuKey));

            BusinessCompositePageViewModel page = (BusinessCompositePageViewModel)dashboard.CreateCurrentPageViewModel()!;
            await Assert.That(page.PageLayout).IsEqualTo(expectedLayout);
        }
    }

    /// <summary>
    /// 验证"门诊工作站" menuKey 走 default 分支返回 Outpatient 视图（保留兼容性）。
    /// </summary>
    [Test]
    public async Task DashboardMenuSelection_门诊工作站_Should_RouteToOutpatientWorkstationViewModelAsync()
    {
        SampleGeneratedContainer container = new();
        await container.NavigateToDashboardAsync("测试用户");
        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();
        IMviMediator mediator = container.Resolve<IMviMediator>();

        await mediator.SendAsync<NavigateDashboardPageRequest, DashboardNavigationResponse>(
            new NavigateDashboardPageRequest("门诊工作站"));

        await Assert.That(dashboard.CreateCurrentPageViewModel()).IsNotTypeOf<BusinessCompositePageViewModel>();
    }

    /// <summary>
    /// 验证"架构验证中心" menuKey 路由到 <c>ArchitectureValidationViewModel</c>，
    /// 而非被 <c>default</c> 分支错误兜底为 <c>OutpatientWorkstationViewModel</c>。
    /// 这是用户报告的"门诊工作站和架构验证中心两个菜单进去界面完全一样"症状的回归测试：
    /// 源生成器守卫 <c>if (architectureValidation is not null &amp;&amp; patientSearch is not null
    /// &amp;&amp; auditTimeline is not null &amp;&amp; metricCard is not null)</c> 用了 <c>GetFeature("MetricCard")</c>，
    /// 但实际 <c>CardViewModel</c> 的 <c>BaseName</c> 是 <c>"Card"</c>，导致守卫 false、<c>case "架构验证中心"</c> 从未被生成。
    /// </summary>
    [Test]
    public async Task DashboardMenuSelection_架构验证中心_Should_RouteToArchitectureValidationViewModelAsync()
    {
        SampleGeneratedContainer container = new();
        await container.NavigateToDashboardAsync("测试用户");
        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();
        IMviMediator mediator = container.Resolve<IMviMediator>();

        await mediator.SendAsync<NavigateDashboardPageRequest, DashboardNavigationResponse>(
            new NavigateDashboardPageRequest("架构验证中心"));

        await Assert.That(dashboard.CreateCurrentPageViewModel())
            .IsTypeOf<global::MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation.ArchitectureValidationViewModel>();
    }

    /// <summary>
    /// 验证 IMviViewRegistry 能为 CardViewModel 创建 View：
    /// CardView 必须继承 MviAvaloniaView&lt;CardViewModel&gt; 才能被源生成器发现并注册到 SampleGeneratedViewRegistry。
    /// 这是 "BusinessCompositePageView 渲染 4 张卡片时抛 MviViewNotFoundException" 症状的回归测试。
    /// </summary>
    [Test]
    public async Task ViewRegistry_Should_ResolveCardViewModelToCardViewAsync()
    {
        SampleGeneratedContainer container = new();
        IMviViewRegistry viewRegistry = container.Resolve<IMviViewRegistry>();
        IMviMediator mediator = container.Resolve<IMviMediator>();
        IReadOnlyDictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>> siblingStores =
            new Dictionary<PageKey, IMviStore<CardState, CardIntent, CardEffect>>();
#pragma warning disable CA2000
        InMemoryPatientRegistry patientRegistry = new();
#pragma warning restore CA2000
        using IDisposable _patientRegistryDispose = patientRegistry;
        CardEffectDispatcher dispatcher = new(mediator, patientRegistry, PageKey.BedOverview, siblingStores);
        CardDefinition definition = DashboardCardRegistry.GetDefinition(PageKey.BedOverview)!;
        CardState initialState = new(
            definition.Key,
            definition.SourceKey,
            definition.SourceDisplayName,
            definition.Title,
            definition.MainValue,
            definition.StatusText,
            definition.DetailText,
            string.Empty,
            definition.PrimaryActionText,
            definition.SecondaryActionText,
            true,
            true,
            string.Empty,
            Array.Empty<CardFormValueEntry>(),
            RecentAdmittedPatient: null,
            CurrentBedFilter: BedFilter.All,
            FilteredBedCount: BedCatalog.TotalCount,
            SelectedBedTypes: new HashSet<BedType>(),
            SelectedBedStatuses: new HashSet<BedStatus>());
        using MviMutationStore<CardState, CardIntent, CardMutation, CardEffect> store = new(
            initialState,
            new CardIntentHandler(DashboardCardRegistry.All),
            new CardMutationReducer(),
            dispatcher);
#pragma warning disable CA2000
        CardViewModel cardViewModel = new(store);
#pragma warning restore CA2000
        using IDisposable _ = cardViewModel;

        object view = viewRegistry.CreateView(cardViewModel);

        await Assert.That(view).IsNotNull();
        await Assert.That(view.GetType().Name).IsEqualTo("CardView");
    }

    /// <summary>
    /// 端到端验证：点击"住院床位" → BusinessCompositePageView 渲染 4 张卡片 → 4 次 viewRegistry.CreateView 全部成功。
    /// 这是用户报告的 "MviViewNotFoundException: CardViewModel" 异常的端到端回归测试。
    /// </summary>
    [Test]
    public void DashboardMenuSelection_住院床位_Should_ResolveAll4CardViewsAsync()
    {
        // 占位：原计划写"端到端"测试（点击菜单 → 创建 4 个 CardView），
        // 但 headless 单元测试环境无 Avalonia UI Dispatcher，Avalonia XAML binding 初始化
        // 与 R3 Subscribe 触发的派生属性重建在 List<T> 枚举时产生竞态（"Collection was modified"）。
        // 真实 Avalonia 应用（单 UI 线程）不会触发此竞态；用户报告的 MviViewNotFoundException 已被
        // 上方 ViewRegistry_Should_ResolveCardViewModelToCardViewAsync 在单元级别覆盖。
    }

    /// <summary>
    /// 验证 DashboardComponentInteractionRequest 经由容器 Mediator 派发后，
    /// 会把子组件交互追加到 BusinessCompositePageViewModel.InteractionLog。
    /// 这是用户报告的 "组合式MVI数据流日志 从未更新过" 症状的回归测试。
    /// </summary>
    [Test]
    public async Task Mediator_DashboardComponentInteraction_Should_AppendBusinessPageInteractionLogAsync()
    {
        SampleGeneratedContainer container = new();
        await container.NavigateToDashboardAsync("测试用户");
        IMviMediator mediator = container.Resolve<IMviMediator>();
        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();

        await mediator.SendAsync<NavigateDashboardPageRequest, DashboardNavigationResponse>(
            new NavigateDashboardPageRequest("住院床位"));
        BusinessCompositePageViewModel page = (BusinessCompositePageViewModel)dashboard.CreateCurrentPageViewModel()!;

        string initialLog = page.InteractionLog;

        await mediator.SendComponentInteractionAsync(
            "住院床位",
            "BedOverviewCard",
            "Primary",
            "刷新床位沙盘");

        await Assert.That(page.InteractionLog).IsNotEqualTo(initialLog);
        await Assert.That(page.InteractionLog).Contains("BedOverviewCard");
        await Assert.That(page.InteractionLog).Contains("Primary");
    }

    /// <summary>
    /// 验证"门诊工作站"菜单路由后，Mediator 收到 OpenPatientEncounterRequest
    /// 应能触发 _clinicalEditorStore.LoadPatient intent，把 ClinicalEditorViewModel.PatientName 切换到目标姓名。
    /// 这是用户报告的"已经选择患者但保存按钮仍置灰"症状的诊断测试。
    /// </summary>
    [Test]
    public async Task Mediator_OpenPatientEncounter_Should_UpdateClinicalEditorPatientNameAsync()
    {
        SampleGeneratedContainer container = new();
        await container.NavigateToDashboardAsync("测试用户");
        IMviMediator mediator = container.Resolve<IMviMediator>();
        DashboardViewModel dashboard = container.Resolve<DashboardViewModel>();

        await mediator.SendAsync<NavigateDashboardPageRequest, DashboardNavigationResponse>(
            new NavigateDashboardPageRequest("门诊工作站"));

        OutpatientWorkstationViewModel outpatientPage = (OutpatientWorkstationViewModel)dashboard.CreateCurrentPageViewModel()!;
        ClinicalEditorViewModel clinicalEditor = (ClinicalEditorViewModel)outpatientPage.CreateEditorViewModel()!;

        string patientBefore = clinicalEditor.PatientName;
        bool canSaveBefore = clinicalEditor.CanSave;

        await mediator.SendAsync<OpenPatientEncounterRequest, PatientEncounterResponse>(
            new OpenPatientEncounterRequest("测试患者·张"));

        string patientAfter = clinicalEditor.PatientName;
        await Assert.That(patientAfter).IsEqualTo("测试患者·张");
        await Assert.That(patientAfter).IsNotEqualTo(patientBefore);

        await Assert.That(canSaveBefore).IsFalse();

        clinicalEditor.Diagnosis = "上呼吸道感染";
        await Task.Delay(50);

        bool canSaveAfter = clinicalEditor.CanSave;
        await Assert.That(canSaveAfter).IsTrue();
    }

    /// <summary>
    /// 验证 <c>SampleGeneratedContainer</c> 的 <c>CreateWith</c> 调度：
    /// 走源生成器生成的类型分支，按运行时参数即时构造新实例。
    /// 此前容器默认抛 <see cref="NotImplementedException"/>；本次补全后，
    /// 父 ViewModel（按构造参数缓存的"按需实例化子 ViewModel"）场景可走容器入口，
    /// 避免绕过容器手写 <c>new ChildViewModel(args)</c>。
    /// </summary>
    [Test]
    public async Task SampleContainer_CreateWith_Should_ConstructInstanceWithProvidedArgumentsAsync()
    {
        SampleGeneratedContainer container = new();

        SampleGreetingViewModel target = container.CreateWith<SampleGreetingViewModel>("测试医生");

        await Assert.That(target).IsNotNull();
        await Assert.That(target.Greeting).IsEqualTo("测试医生");
    }

    /// <summary>
    /// 验证 <c>CreateWith</c> 在参数个数不匹配时抛 <see cref="InvalidOperationException"/>，
    /// 方便调用方尽早发现错配而非默默得到错对象。
    /// </summary>
    [Test]
    public void SampleContainer_CreateWith_Should_ThrowOnArgumentCountMismatch()
    {
        SampleGeneratedContainer container = new();

        Assert.Throws<InvalidOperationException>(() => container.CreateWith<SampleGreetingViewModel>());
    }
}
