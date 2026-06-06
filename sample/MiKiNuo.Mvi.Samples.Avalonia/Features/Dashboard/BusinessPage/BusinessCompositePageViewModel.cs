﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Mediator;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Application.MVI.Threading;
using MiKiNuo.Mvi.Application.MVI.ViewModel;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;
using MiKiNuo.Mvi.Domain.MVI.Binding;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.BusinessPage;

/// <summary>
/// 表示生产业务组合页面 ViewModel。
/// <para>
/// 不再为 4 个业务域维护 4 套不同的子布局；所有业务域共用同一套 2×2 「数据流节点」布局：
/// </para>
/// <code>
///   ① 节点  ──数据①──▶  ② 节点
///                            │
///                            ▼ 数据②
///   ④ 节点  ◀──数据③───  ③ 节点
/// </code>
/// <para>
/// 4 张卡片固定映射为 4 个数据流节点（Node1..Node4），具体业务由 <see cref="PageLayout"/> 切换：
/// 4 个 menuKey 共享同一套 XAML 模板，仅节点标题/角色/数据流标签由本 VM 动态解析。
/// </para>
/// </summary>
public sealed partial class BusinessCompositePageViewModel
    : MviViewModelBase<BusinessCompositePageState, BusinessCompositePageIntent, BusinessCompositePageEffect>
{
    private readonly CardStoreFactory _factory;

    /// <summary>
    /// 初始化生产业务组合页面 ViewModel。
    /// 16 个 CardViewModel 由 DI 容器注入的 <see cref="CardStoreFactory"/> 单例提供。
    /// </summary>
    /// <param name="store">业务页面状态存储。</param>
    /// <param name="factory">卡片 Store 与 ViewModel 工厂。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public BusinessCompositePageViewModel(
        IMviStore<BusinessCompositePageState, BusinessCompositePageIntent, BusinessCompositePageEffect> store,
        CardStoreFactory factory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    /// <summary>获取住院床位总览卡片 VM。</summary>
    public CardViewModel InpatientBedOverviewCard => _factory.GetViewModel(PageKey.BedOverview);

    /// <summary>获取入院协调卡片 VM（Form Card）。</summary>
    public CardViewModel InpatientAdmissionCoordinatorCard => _factory.GetViewModel(PageKey.AdmissionCoordinator);

    /// <summary>获取护理任务卡片 VM。</summary>
    public CardViewModel InpatientNursingTaskCard => _factory.GetViewModel(PageKey.NursingTaskBoard);

    /// <summary>获取病区风险事件卡片 VM（Form Card）。</summary>
    public CardViewModel InpatientRiskEventCard => _factory.GetViewModel(PageKey.WardRiskPanel);

    /// <summary>获取检验医嘱开立卡片 VM（Form Card）。</summary>
    public CardViewModel LabOrderCard => _factory.GetViewModel(PageKey.LabOrderComposer);

    /// <summary>获取标本流转卡片 VM。</summary>
    public CardViewModel LabSpecimenFlowCard => _factory.GetViewModel(PageKey.SpecimenTracker);

    /// <summary>获取危急值卡片 VM。</summary>
    public CardViewModel LabCriticalValueCard => _factory.GetViewModel(PageKey.CriticalValueMonitor);

    /// <summary>获取 TAT 监控卡片 VM。</summary>
    public CardViewModel LabTatCard => _factory.GetViewModel(PageKey.LabTurnaroundBoard);

    /// <summary>获取处方审核卡片 VM（Form Card）。</summary>
    public CardViewModel PharmacyPrescriptionCard => _factory.GetViewModel(PageKey.PrescriptionReviewBoard);

    /// <summary>获取库存水位卡片 VM。</summary>
    public CardViewModel PharmacyStockCard => _factory.GetViewModel(PageKey.DrugStockMonitor);

    /// <summary>获取补货计划卡片 VM。</summary>
    public CardViewModel PharmacyReplenishmentCard => _factory.GetViewModel(PageKey.ReplenishmentPlanner);

    /// <summary>获取用药安全卡片 VM。</summary>
    public CardViewModel PharmacySafetyCard => _factory.GetViewModel(PageKey.MedicationSafetyPanel);

    /// <summary>获取院级 KPI 卡片 VM。</summary>
    public CardViewModel QualityKpiCard => _factory.GetViewModel(PageKey.QualityKpiBoard);

    /// <summary>获取病历缺陷抽查卡片 VM。</summary>
    public CardViewModel QualityAuditCard => _factory.GetViewModel(PageKey.MedicalRecordAuditBoard);

    /// <summary>获取风险事件分级卡片 VM（Form Card）。</summary>
    public CardViewModel QualityRiskCard => _factory.GetViewModel(PageKey.RiskEventBoard);

    /// <summary>获取整改闭环追踪卡片 VM。</summary>
    public CardViewModel QualityRectificationCard => _factory.GetViewModel(PageKey.RectificationTracker);

    /// <summary>
    /// 获取场景标题。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.ScenarioTitle))]
    public partial string ScenarioTitle { get; private set; }

    /// <summary>
    /// 获取场景摘要。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.ScenarioSummary))]
    public partial string ScenarioSummary { get; private set; }

    /// <summary>
    /// 获取页面布局键。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.PageLayout))]
    public partial string PageLayout { get; private set; }

    /// <summary>
    /// 获取当前业务上下文。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.ActiveContext))]
    public partial string ActiveContext { get; private set; }

    /// <summary>
    /// 获取当前流程状态。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.FlowStatus))]
    public partial string FlowStatus { get; private set; }

    /// <summary>
    /// 获取父子 MVI 与子子 MVI 交互日志。
    /// </summary>
    [MviBind(nameof(BusinessCompositePageState.InteractionLog))]
    public partial string InteractionLog { get; private set; }

    /// <summary>
    /// 获取当前业务域的 SourceKey（用于 XAML 端读取对应业务域色板）。
    /// 未识别 PageLayout 时回退为 Other。
    /// </summary>
    public string DomainSourceKey => PageLayout switch
    {
        "Inpatient" => "Inpatient",
        "Lab" => "Lab",
        "Pharmacy" => "Pharmacy",
        "Quality" => "Quality",
        _ => "Other",
    };

    /// <summary>获取 1 号数据流节点卡片（业务入口/数据源）。</summary>
    public CardViewModel Node1 => PageLayout switch
    {
        "Inpatient" => InpatientBedOverviewCard,
        "Lab" => LabOrderCard,
        "Pharmacy" => PharmacyPrescriptionCard,
        "Quality" => QualityKpiCard,
        _ => InpatientBedOverviewCard,
    };

    /// <summary>获取 2 号数据流节点卡片（处理/流转）。</summary>
    public CardViewModel Node2 => PageLayout switch
    {
        "Inpatient" => InpatientAdmissionCoordinatorCard,
        "Lab" => LabSpecimenFlowCard,
        "Pharmacy" => PharmacyStockCard,
        "Quality" => QualityAuditCard,
        _ => InpatientAdmissionCoordinatorCard,
    };

    /// <summary>获取 3 号数据流节点卡片（校验/执行）。</summary>
    public CardViewModel Node3 => PageLayout switch
    {
        "Inpatient" => InpatientNursingTaskCard,
        "Lab" => LabCriticalValueCard,
        "Pharmacy" => PharmacyReplenishmentCard,
        "Quality" => QualityRiskCard,
        _ => InpatientNursingTaskCard,
    };

    /// <summary>获取 4 号数据流节点卡片（监控/闭环）。</summary>
    public CardViewModel Node4 => PageLayout switch
    {
        "Inpatient" => InpatientRiskEventCard,
        "Lab" => LabTatCard,
        "Pharmacy" => PharmacySafetyCard,
        "Quality" => QualityRectificationCard,
        _ => InpatientRiskEventCard,
    };

    /// <summary>获取 1 号节点角色（用于节点标题下方的副标）。</summary>
    public string Node1Role => PageLayout switch
    {
        "Inpatient" => "床位状态",
        "Lab" => "申请入口",
        "Pharmacy" => "处方审核",
        "Quality" => "指标总览",
        _ => "节点 1",
    };

    /// <summary>获取 2 号节点角色。</summary>
    public string Node2Role => PageLayout switch
    {
        "Inpatient" => "入院登记",
        "Lab" => "标本流转",
        "Pharmacy" => "库存水位",
        "Quality" => "病历抽查",
        _ => "节点 2",
    };

    /// <summary>获取 3 号节点角色。</summary>
    public string Node3Role => PageLayout switch
    {
        "Inpatient" => "任务执行",
        "Lab" => "危急值告警",
        "Pharmacy" => "补货计划",
        "Quality" => "风险分级",
        _ => "节点 3",
    };

    /// <summary>获取 4 号节点角色。</summary>
    public string Node4Role => PageLayout switch
    {
        "Inpatient" => "风险监控",
        "Lab" => "TAT 监控",
        "Pharmacy" => "用药拦截",
        "Quality" => "整改闭环",
        _ => "节点 4",
    };

    /// <summary>获取 1→2 节点间数据流标签（数据在节点间如何流转）。</summary>
    public string FlowLabel12 => PageLayout switch
    {
        "Inpatient" => "床位释放",
        "Lab" => "检验申请",
        "Pharmacy" => "处方下发",
        "Quality" => "指标下发",
        _ => "数据流 1",
    };

    /// <summary>获取 2→3 节点间数据流标签。</summary>
    public string FlowLabel23 => PageLayout switch
    {
        "Inpatient" => "入院任务",
        "Lab" => "标本完成",
        "Pharmacy" => "库存预警",
        "Quality" => "事件上报",
        _ => "数据流 2",
    };

    /// <summary>获取 3→4 节点间数据流标签。</summary>
    public string FlowLabel34 => PageLayout switch
    {
        "Inpatient" => "风险上报",
        "Lab" => "危急值",
        "Pharmacy" => "拦截规则",
        "Quality" => "整改派单",
        _ => "数据流 3",
    };

    /// <summary>获取业务域的简短流程摘要（用于顶部状态条）。</summary>
    public string FlowSummary => PageLayout switch
    {
        "Inpatient" => "床位释放 → 入院登记 → 护理执行 → 风险监控",
        "Lab" => "医嘱开立 → 标本流转 → 危急值告警 → TAT 监控",
        "Pharmacy" => "处方审核 → 库存水位 → 补货计划 → 用药拦截",
        "Quality" => "院级 KPI → 病历抽查 → 风险分级 → 整改闭环",
        _ => "数据流 ① → ② → ③ → ④",
    };
}
