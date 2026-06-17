﻿﻿﻿﻿using MiKiNuo.Mvi.Application.MVI.Store;
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
/// <para>
/// 本 VM 仅暴露 <see cref="PageKey"/> 判别器（<see cref="Node1Key"/>..<see cref="Node4Key"/>），
/// 不再持有任何 <see cref="CardViewModel"/> 引用；View 层通过 <see cref="CardStoreFactory"/>
/// 把 PageKey 解析为具体的 CardViewModel，符合 MVI "State 不存 ViewModel" 原则。
/// </para>
/// </summary>
public sealed partial class BusinessCompositePageViewModel
    : MviViewModelBase<BusinessCompositePageState, BusinessCompositePageIntent, BusinessCompositePageEffect>
{
    private readonly CardStoreFactory _cardStoreFactory;

    /// <summary>
    /// 初始化生产业务组合页面 ViewModel。
    /// </summary>
    /// <param name="store">业务页面状态存储。</param>
    /// <param name="cardStoreFactory">仪表板卡片工厂（用于按 PageKey 解析具体 CardViewModel）。</param>
    /// <param name="uiDispatcher">UI 调度器（可选，由 DI 容器注入以确保 Avalonia UI 线程触发 CanExecuteChanged）。</param>
    public BusinessCompositePageViewModel(
        IMviStore<BusinessCompositePageState, BusinessCompositePageIntent, BusinessCompositePageEffect> store,
        CardStoreFactory cardStoreFactory,
        IMviUiDispatcher? uiDispatcher = null)
        : base(store, uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(cardStoreFactory);
        _cardStoreFactory = cardStoreFactory;
    }

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

    /// <summary>获取 1 号数据流节点卡片的 PageKey（业务入口/数据源），供 View 解析为具体 CardViewModel。</summary>
    public PageKey Node1Key => ResolveNodeKey(0);

    /// <summary>获取 2 号数据流节点卡片的 PageKey（处理/流转），供 View 解析为具体 CardViewModel。</summary>
    public PageKey Node2Key => ResolveNodeKey(1);

    /// <summary>获取 3 号数据流节点卡片的 PageKey（校验/执行），供 View 解析为具体 CardViewModel。</summary>
    public PageKey Node3Key => ResolveNodeKey(2);

    /// <summary>获取 4 号数据流节点卡片的 PageKey（监控/闭环），供 View 解析为具体 CardViewModel。</summary>
    public PageKey Node4Key => ResolveNodeKey(3);

    /// <summary>
    /// 解析 1 号数据流节点卡片 ViewModel（经由 <see cref="CardStoreFactory"/> 共享 store/VM 实例）。
    /// 供 <c>[MviSlot]</c> 源生成器 emit 的 <c>OnBindSlots</c> 钩子调用。
    /// </summary>
    /// <returns>1 号节点 <c>CardViewModel</c> 实例。</returns>
    public object CreateNode1ViewModel() => _cardStoreFactory.GetViewModel(Node1Key);

    /// <summary>
    /// 解析 2 号数据流节点卡片 ViewModel（经由 <see cref="CardStoreFactory"/> 共享 store/VM 实例）。
    /// 供 <c>[MviSlot]</c> 源生成器 emit 的 <c>OnBindSlots</c> 钩子调用。
    /// </summary>
    /// <returns>2 号节点 <c>CardViewModel</c> 实例。</returns>
    public object CreateNode2ViewModel() => _cardStoreFactory.GetViewModel(Node2Key);

    /// <summary>
    /// 解析 3 号数据流节点卡片 ViewModel（经由 <see cref="CardStoreFactory"/> 共享 store/VM 实例）。
    /// 供 <c>[MviSlot]</c> 源生成器 emit 的 <c>OnBindSlots</c> 钩子调用。
    /// </summary>
    /// <returns>3 号节点 <c>CardViewModel</c> 实例。</returns>
    public object CreateNode3ViewModel() => _cardStoreFactory.GetViewModel(Node3Key);

    /// <summary>
    /// 解析 4 号数据流节点卡片 ViewModel（经由 <see cref="CardStoreFactory"/> 共享 store/VM 实例）。
    /// 供 <c>[MviSlot]</c> 源生成器 emit 的 <c>OnBindSlots</c> 钩子调用。
    /// </summary>
    /// <returns>4 号节点 <c>CardViewModel</c> 实例。</returns>
    public object CreateNode4ViewModel() => _cardStoreFactory.GetViewModel(Node4Key);

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

    /// <summary>
    /// 根据 <see cref="PageLayout"/> 与节点索引（0..3）解析出对应的 <see cref="PageKey"/>。
    /// 4 个节点固定为 4 张业务卡片，映射表集中维护以避免 <see cref="Node1Key"/>..<see cref="Node4Key"/>
    /// 4 个独立 switch 出现 drift。
    /// 未识别 PageLayout 时回退为 Inpatient 域的 4 个节点，保持单一确定性降级路径。
    /// </summary>
    /// <param name="nodeIndex">节点索引：0=Node1, 1=Node2, 2=Node3, 3=Node4。</param>
    /// <returns>节点对应的 PageKey。</returns>
    private PageKey ResolveNodeKey(int nodeIndex)
    {
        return PageLayout switch
        {
            "Inpatient" => nodeIndex switch
            {
                0 => PageKey.BedOverview,
                1 => PageKey.AdmissionCoordinator,
                2 => PageKey.NursingTaskBoard,
                _ => PageKey.WardRiskPanel,
            },
            "Lab" => nodeIndex switch
            {
                0 => PageKey.LabOrderComposer,
                1 => PageKey.SpecimenTracker,
                2 => PageKey.CriticalValueMonitor,
                _ => PageKey.LabTurnaroundBoard,
            },
            "Pharmacy" => nodeIndex switch
            {
                0 => PageKey.PrescriptionReviewBoard,
                1 => PageKey.DrugStockMonitor,
                2 => PageKey.ReplenishmentPlanner,
                _ => PageKey.MedicationSafetyPanel,
            },
            "Quality" => nodeIndex switch
            {
                0 => PageKey.QualityKpiBoard,
                1 => PageKey.MedicalRecordAuditBoard,
                2 => PageKey.RiskEventBoard,
                _ => PageKey.RectificationTracker,
            },
            _ => nodeIndex switch
            {
                0 => PageKey.BedOverview,
                1 => PageKey.AdmissionCoordinator,
                2 => PageKey.NursingTaskBoard,
                _ => PageKey.WardRiskPanel,
            },
        };
    }
}
