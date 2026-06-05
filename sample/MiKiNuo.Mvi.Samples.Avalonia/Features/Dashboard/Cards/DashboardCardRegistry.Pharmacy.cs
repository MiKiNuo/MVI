namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 仪表板卡片注册表的处方域分部。
/// 负责处方审核、库存监控、补货计划、用药安全 4 张卡片的 <see cref="CardDefinition"/> 注册与处方审核验证器构造。
/// </summary>
public static partial class DashboardCardRegistry
{
    private static partial void AddPharmacyDefinitions(Dictionary<PageKey, CardDefinition> dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        dict[PageKey.PrescriptionReviewBoard] = new CardDefinition(
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
            Validator: BuildPrescriptionValidator());

        dict[PageKey.DrugStockMonitor] = new CardDefinition(
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
            Validator: null);

        dict[PageKey.ReplenishmentPlanner] = new CardDefinition(
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
            Validator: null);

        dict[PageKey.MedicationSafetyPanel] = new CardDefinition(
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
            Validator: null);
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
}
