namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片在 PageKey 维度的唯一强类型标识。
/// 同一 enum 字面量同时承担两个角色：Mediator 路由键和 CardDefinition 注册表查表键。
/// 新增字面量为 load-bearing API（详见 .gsd/DECISIONS.md 2026-06-02），改名是跨卡片级 breaking change。
/// </summary>
public enum PageKey
{
    /// <summary>床位总览。</summary>
    BedOverview = 1,

    /// <summary>护理任务看板。</summary>
    NursingTaskBoard = 2,

    /// <summary>病区风险面板。</summary>
    WardRiskPanel = 3,

    /// <summary>入院登记。</summary>
    AdmissionCoordinator = 4,

    /// <summary>检验医嘱开立。</summary>
    LabOrderComposer = 5,

    /// <summary>标本流转追踪。</summary>
    SpecimenTracker = 6,

    /// <summary>危急值闭环。</summary>
    CriticalValueMonitor = 7,

    /// <summary>TAT 实时监控。</summary>
    LabTurnaroundBoard = 8,

    /// <summary>处方审核队列。</summary>
    PrescriptionReviewBoard = 9,

    /// <summary>药品库存水位。</summary>
    DrugStockMonitor = 10,

    /// <summary>补货计划与采购建议。</summary>
    ReplenishmentPlanner = 11,

    /// <summary>用药安全与拦截规则。</summary>
    MedicationSafetyPanel = 12,

    /// <summary>院级质量 KPI。</summary>
    QualityKpiBoard = 13,

    /// <summary>病历缺陷抽查。</summary>
    MedicalRecordAuditBoard = 14,

    /// <summary>风险事件分级。</summary>
    RiskEventBoard = 15,

    /// <summary>整改闭环追踪。</summary>
    RectificationTracker = 16,
}
