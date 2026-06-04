using ZLinq;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示 16 张仪表板卡片的静态注册表。
/// Reducer 和 ViewModel 通过 PageKey 查找对应的 CardDefinition。
/// 顺序：先 Inpatient（4），再 Lab（4），再 Pharmacy（4），再 Quality（4）。
/// </summary>
public static class DashboardCardRegistry
{
    private static readonly IReadOnlyDictionary<PageKey, CardDefinition> DefinitionsByKey = BuildDefinitions();

    /// <summary>
    /// 获取所有卡片定义。
    /// </summary>
    public static IReadOnlyDictionary<PageKey, CardDefinition> All => DefinitionsByKey;

    /// <summary>
    /// 根据 PageKey 查找卡片定义。
    /// </summary>
    /// <param name="key">PageKey。</param>
    /// <returns>找到的 CardDefinition；若不存在则返回 null。</returns>
    public static CardDefinition? GetDefinition(PageKey key)
    {
        return DefinitionsByKey.TryGetValue(key, out CardDefinition? definition) ? definition : null;
    }

    /// <summary>
    /// 获取与给定 PageKey 同 SourceKey 组（住院床位 / 检验医嘱 / 处方 / 风险）的兄弟卡片 PageKey 集合。
    /// 同组卡片共享一个 4 卡片组合页面（如住院床位 = 床位总览 + 护理任务 + 病区风险 + 入院登记），
    /// 跨卡片副作用会通过此方法定位派发目标；不同 SourceKey 的卡片彼此隔离。
    /// </summary>
    /// <param name="key">源 PageKey。</param>
    /// <returns>同组的兄弟 PageKey 集合；找不到定义时返回空集合。</returns>
    public static IReadOnlyList<PageKey> GetSiblingKeys(PageKey key)
    {
        CardDefinition? source = GetDefinition(key);
        if (source is null)
        {
            return Array.Empty<PageKey>();
        }

        List<PageKey> siblings = new(DefinitionsByKey.Count);
        foreach (KeyValuePair<PageKey, CardDefinition> pair in DefinitionsByKey)
        {
            if (pair.Key == key)
            {
                continue;
            }

            if (string.Equals(pair.Value.SourceKey, source.SourceKey, StringComparison.Ordinal))
            {
                siblings.Add(pair.Key);
            }
        }

        return siblings;
    }

    private static IReadOnlyDictionary<PageKey, CardDefinition> BuildDefinitions()
    {
        return new Dictionary<PageKey, CardDefinition>
        {
            // ===== Inpatient =====
            [PageKey.BedOverview] = new CardDefinition(
                Key: PageKey.BedOverview,
                SourceKey: "住院床位",
                SourceDisplayName: "床位总览 MVI",
                Title: "床位总览 MVI",
                MainValue: "开放床位 186 / 220",
                StatusText: "床位紧张",
                DetailText: "点击急诊转入住院会触发床位候选患者选择，父页面、入院流程、护理任务和风险面板都会收到 Mediator 请求。",
                PrimaryActionText: "急诊转入住院",
                SecondaryActionText: "锁定 ICU 床位",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            [PageKey.NursingTaskBoard] = new CardDefinition(
                Key: PageKey.NursingTaskBoard,
                SourceKey: "住院床位",
                SourceDisplayName: "护理任务 MVI",
                Title: "护理任务 MVI",
                MainValue: "待闭环 124 项",
                StatusText: "高负载",
                DetailText: "根据入院确认自动生成首诊护理、跌倒评估、压疮评估和腕带核对任务。",
                PrimaryActionText: "完成首诊护理",
                SecondaryActionText: "升级护士长",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            [PageKey.WardRiskPanel] = new CardDefinition(
                Key: PageKey.WardRiskPanel,
                SourceKey: "住院床位",
                SourceDisplayName: "病区风险 MVI",
                Title: "病区风险 MVI",
                MainValue: "高危患者 18 人",
                StatusText: "需评估",
                DetailText: "联动入院流程和护理任务，自动评估跌倒、压疮、过敏和隔离风险。",
                PrimaryActionText: "重新评估风险",
                SecondaryActionText: "启动隔离预案",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            [PageKey.AdmissionCoordinator] = new CardDefinition(
                Key: PageKey.AdmissionCoordinator,
                SourceKey: "住院床位",
                SourceDisplayName: "入院流程 MVI",
                Title: "入院登记 MVI",
                MainValue: "待入院 37 人",
                StatusText: "等待护士录入入院资料",
                DetailText: "护士录入患者、诊断、床号和交接备注，确认后由副作用提交给 Mediator，再分发给床位总览、护理任务和病区风险 MVI。",
                PrimaryActionText: "确认入院",
                SecondaryActionText: "退回急诊",
                FormFields: new CardFormField[]
                {
                    new("PatientName", "患者姓名", "例如：张三", string.Empty, isRequired: true),
                    new("PatientAge", "年龄", "例如：68", string.Empty, isRequired: false),
                    new("AdmissionDiagnosis", "入院诊断", "例如：急性心衰", string.Empty, isRequired: true),
                    new("TargetBedNo", "目标床号", "例如：A12-08", string.Empty, isRequired: true),
                    new("NurseNote", "护士交接备注", "过敏史、跌倒风险、管路情况等", string.Empty, isRequired: false),
                },
                RequiredFormFields: new[] { "PatientName", "AdmissionDiagnosis", "TargetBedNo" },
                Validator: BuildAdmissionValidator()),

            // ===== Lab =====
            [PageKey.LabOrderComposer] = new CardDefinition(
                Key: PageKey.LabOrderComposer,
                SourceKey: "检验医嘱",
                SourceDisplayName: "医嘱开立 MVI",
                Title: "检验医嘱录入 MVI",
                MainValue: "待开立 72 条",
                StatusText: "等待医生录入医嘱",
                DetailText: "医生录入患者、检验项目、标本和指征后提交，标本流转、危急值和 TAT 组件会联动更新。",
                PrimaryActionText: "开立急诊检验",
                SecondaryActionText: "保存草稿",
                FormFields: new CardFormField[]
                {
                    new("PatientIdentity", "患者标识", "例如：住院号 / 身份证号", string.Empty, isRequired: true),
                    new("TestItem", "检验项目", "例如：血常规", string.Empty, isRequired: true),
                    new("PriorityLevel", "优先级", "例如：常规 / 急诊", "常规", isRequired: false),
                    new("SpecimenType", "标本类型", "例如：静脉血", string.Empty, isRequired: true),
                    new("ClinicalIndication", "临床指征", "例如：发热待查", string.Empty, isRequired: false),
                },
                RequiredFormFields: new[] { "PatientIdentity", "TestItem", "SpecimenType" },
                Validator: BuildLabOrderValidator()),

            [PageKey.SpecimenTracker] = new CardDefinition(
                Key: PageKey.SpecimenTracker,
                SourceKey: "检验医嘱",
                SourceDisplayName: "标本流转 MVI",
                Title: "标本流转 MVI",
                MainValue: "运输 18 批",
                StatusText: "冷链监控",
                DetailText: "接收医嘱后生成采样、签收、上机节点；采样完成后通知 TAT 和危急值组件。",
                PrimaryActionText: "标本已采集",
                SecondaryActionText: "标记冷链异常",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            [PageKey.CriticalValueMonitor] = new CardDefinition(
                Key: PageKey.CriticalValueMonitor,
                SourceKey: "检验医嘱",
                SourceDisplayName: "危急值闭环 MVI",
                Title: "危急值闭环 MVI",
                MainValue: "危急值 5 条",
                StatusText: "待医生确认",
                DetailText: "根据标本结果触发危急值通知，确认后反向更新医嘱和 TAT。",
                PrimaryActionText: "确认危急值",
                SecondaryActionText: "电话通知医生",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            [PageKey.LabTurnaroundBoard] = new CardDefinition(
                Key: PageKey.LabTurnaroundBoard,
                SourceKey: "检验医嘱",
                SourceDisplayName: "TAT 监控 MVI",
                Title: "TAT 监控 MVI",
                MainValue: "超时风险 3 批",
                StatusText: "倒计时",
                DetailText: "从医嘱和标本组件接收节点时间，计算检验全过程 TAT。",
                PrimaryActionText: "刷新 TAT",
                SecondaryActionText: "启动超时预警",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            // ===== Pharmacy =====
            [PageKey.PrescriptionReviewBoard] = new CardDefinition(
                Key: PageKey.PrescriptionReviewBoard,
                SourceKey: "处方",
                SourceDisplayName: "处方审核 MVI",
                Title: "处方审核 MVI",
                MainValue: "待审 126 张",
                StatusText: "等待药师录入审核意见",
                DetailText: "药师录入处方、药品、剂量和过敏史，确认后联动库存扣减、补货计划和用药安全 MVI。",
                PrimaryActionText: "通过处方",
                SecondaryActionText: "退回医生",
                FormFields: new CardFormField[]
                {
                    new("PrescriptionNo", "处方号", "例如：RX-20260602-01", string.Empty, isRequired: true),
                    new("PatientName", "患者姓名", "例如：张三", string.Empty, isRequired: true),
                    new("DrugName", "药品名称", "例如：阿莫西林", string.Empty, isRequired: true),
                    new("DoseText", "剂量用法", "例如：0.5g q8h", string.Empty, isRequired: true),
                    new("AllergyHistory", "过敏史", "例如：青霉素过敏", string.Empty, isRequired: false),
                },
                RequiredFormFields: new[] { "PrescriptionNo", "PatientName", "DrugName", "DoseText" },
                Validator: BuildPrescriptionValidator()),

            [PageKey.DrugStockMonitor] = new CardDefinition(
                Key: PageKey.DrugStockMonitor,
                SourceKey: "处方",
                SourceDisplayName: "库存监控 MVI",
                Title: "库存监控 MVI",
                MainValue: "预警 23 项",
                StatusText: "需补货",
                DetailText: "接收处方预占请求，库存低于安全线时通知补货计划和用药安全组件。",
                PrimaryActionText: "触发低库存",
                SecondaryActionText: "完成入库",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            [PageKey.ReplenishmentPlanner] = new CardDefinition(
                Key: PageKey.ReplenishmentPlanner,
                SourceKey: "处方",
                SourceDisplayName: "补货计划 MVI",
                Title: "补货计划 MVI",
                MainValue: "采购 8 单",
                StatusText: "待确认",
                DetailText: "根据库存预警和处方消耗生成补货计划，确认采购后反向通知库存组件。",
                PrimaryActionText: "确认采购单",
                SecondaryActionText: "切换供应商",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            [PageKey.MedicationSafetyPanel] = new CardDefinition(
                Key: PageKey.MedicationSafetyPanel,
                SourceKey: "处方",
                SourceDisplayName: "用药安全 MVI",
                Title: "用药安全 MVI",
                MainValue: "风险 14 条",
                StatusText: "药师复核",
                DetailText: "联动处方审核和库存监控，提示抗菌药、过敏、相互作用与替代用药。",
                PrimaryActionText: "完成药师复核",
                SecondaryActionText: "升级用药会诊",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            // ===== Quality =====
            [PageKey.QualityKpiBoard] = new CardDefinition(
                Key: PageKey.QualityKpiBoard,
                SourceKey: "风险",
                SourceDisplayName: "质量 KPI MVI",
                Title: "质量 KPI MVI",
                MainValue: "综合达标 97.6%",
                StatusText: "持续监控",
                DetailText: "接收病历缺陷、风险事件和整改完成结果，实时更新质控指标。",
                PrimaryActionText: "刷新 KPI",
                SecondaryActionText: "发布院周报",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            [PageKey.MedicalRecordAuditBoard] = new CardDefinition(
                Key: PageKey.MedicalRecordAuditBoard,
                SourceKey: "风险",
                SourceDisplayName: "病历质控 MVI",
                Title: "病历质控 MVI",
                MainValue: "待整改 31 份",
                StatusText: "科室整改",
                DetailText: "选择病历缺陷后会创建整改任务，并联动风险事件和 KPI。",
                PrimaryActionText: "选择缺陷病历",
                SecondaryActionText: "批量退回科室",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),

            [PageKey.RiskEventBoard] = new CardDefinition(
                Key: PageKey.RiskEventBoard,
                SourceKey: "风险",
                SourceDisplayName: "风险事件 MVI",
                Title: "风险事件上报 MVI",
                MainValue: "待处理 9 件",
                StatusText: "等待质控员录入风险事件",
                DetailText: "质控员录入事件、科室、等级、责任人和整改措施，确认后联动 KPI、病历质控和整改闭环 MVI。",
                PrimaryActionText: "升级高风险事件",
                SecondaryActionText: "关闭低风险事件",
                FormFields: new CardFormField[]
                {
                    new("EventTitle", "事件标题", "例如：手术器械清点异常", string.Empty, isRequired: true),
                    new("DepartmentName", "责任科室", "例如：骨科", string.Empty, isRequired: true),
                    new("SeverityLevel", "风险等级", "例如：一般 / 较大 / 重大", string.Empty, isRequired: true),
                    new("ResponsibleOwner", "责任人", "例如：王医生", string.Empty, isRequired: false),
                    new("CorrectiveAction", "整改措施", "例如：增加 2 人核对环节", string.Empty, isRequired: true),
                },
                RequiredFormFields: new[] { "EventTitle", "DepartmentName", "SeverityLevel", "CorrectiveAction" },
                Validator: BuildRiskEventValidator()),

            [PageKey.RectificationTracker] = new CardDefinition(
                Key: PageKey.RectificationTracker,
                SourceKey: "风险",
                SourceDisplayName: "整改闭环 MVI",
                Title: "整改闭环 MVI",
                MainValue: "进行中 14 项",
                StatusText: "科室承接",
                DetailText: "接收病历质控和风险事件的整改任务，确认完成后回写到 KPI 组件。",
                PrimaryActionText: "完成整改",
                SecondaryActionText: "升级院级",
                FormFields: null,
                RequiredFormFields: null,
                Validator: null),
        };
    }

    private static Func<IReadOnlyList<CardFormValueEntry>, (bool CanSubmit, string StatusText, string ActionLog)> BuildFormValidator(
        string[] requiredKeys,
        string contextName,
        string incompleteStatusText,
        string completeStatusText,
        (string Key, string DisplayName)[] logKeys)
    {
        return values =>
        {
            IReadOnlyDictionary<string, string> lookup = BuildLookup(values);
            bool canSubmit = requiredKeys.AsValueEnumerable()
                .All(key => !string.IsNullOrWhiteSpace(lookup[key]));
            string statusText = canSubmit ? completeStatusText : incompleteStatusText;
            string logParts = logKeys.AsValueEnumerable()
                .Select(entry => $"{entry.DisplayName}={lookup[entry.Key]}")
                .JoinToString("，");
            return (canSubmit, statusText, $"正在录入{contextName}：{logParts}。");
        };
    }

    private static IReadOnlyDictionary<string, string> BuildLookup(IReadOnlyList<CardFormValueEntry> values)
    {
        Dictionary<string, string> lookup = new(StringComparer.Ordinal);
        foreach (CardFormValueEntry entry in values)
        {
            // 与原始实现一致：仅在尚未出现过的键上记录，保留"首个匹配"的语义。
            lookup.TryAdd(entry.Key, entry.Value);
        }

        return lookup;
    }

    private static Func<IReadOnlyList<CardFormValueEntry>, (bool CanSubmit, string StatusText, string ActionLog)> BuildAdmissionValidator()
    {
        return BuildFormValidator(
            requiredKeys: ["PatientName", "AdmissionDiagnosis", "TargetBedNo"],
            contextName: "入院登记",
            incompleteStatusText: "请补齐患者姓名、诊断和床号",
            completeStatusText: "入院登记资料已完整，可以提交",
            logKeys:
            [
                ("PatientName", "患者"),
                ("AdmissionDiagnosis", "诊断"),
                ("TargetBedNo", "床号"),
            ]);
    }

    private static Func<IReadOnlyList<CardFormValueEntry>, (bool CanSubmit, string StatusText, string ActionLog)> BuildLabOrderValidator()
    {
        return BuildFormValidator(
            requiredKeys: ["PatientIdentity", "TestItem", "SpecimenType"],
            contextName: "检验医嘱",
            incompleteStatusText: "请补齐患者标识、检验项目和标本类型",
            completeStatusText: "检验医嘱资料已完整，可以提交",
            logKeys:
            [
                ("PatientIdentity", "患者"),
                ("TestItem", "项目"),
                ("SpecimenType", "标本"),
            ]);
    }

    private static Func<IReadOnlyList<CardFormValueEntry>, (bool CanSubmit, string StatusText, string ActionLog)> BuildPrescriptionValidator()
    {
        return BuildFormValidator(
            requiredKeys: ["PrescriptionNo", "PatientName", "DrugName", "DoseText"],
            contextName: "处方审核",
            incompleteStatusText: "请补齐处方号、患者、药品和剂量",
            completeStatusText: "处方审核资料已完整，可以提交",
            logKeys:
            [
                ("PrescriptionNo", "处方"),
                ("DrugName", "药品"),
                ("DoseText", "剂量"),
            ]);
    }

    private static Func<IReadOnlyList<CardFormValueEntry>, (bool CanSubmit, string StatusText, string ActionLog)> BuildRiskEventValidator()
    {
        return BuildFormValidator(
            requiredKeys: ["EventTitle", "DepartmentName", "SeverityLevel", "CorrectiveAction"],
            contextName: "风险事件",
            incompleteStatusText: "请补齐事件标题、责任科室、风险等级和整改措施",
            completeStatusText: "风险事件资料已完整，可以提交",
            logKeys:
            [
                ("EventTitle", "事件"),
                ("DepartmentName", "科室"),
                ("SeverityLevel", "等级"),
            ]);
    }
}
