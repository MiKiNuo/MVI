namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 仪表板卡片注册表的风险/质控域分部。
/// 负责质量 KPI、病历质控、风险事件、整改闭环 4 张卡片的 <see cref="CardDefinition"/> 注册与风险事件验证器构造。
/// </summary>
public static partial class DashboardCardRegistry
{
    private static partial void AddQualityDefinitions(Dictionary<PageKey, CardDefinition> dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        dict[PageKey.QualityKpiBoard] = new CardDefinition(
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
            Validator: null);

        dict[PageKey.MedicalRecordAuditBoard] = new CardDefinition(
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
            Validator: null);

        dict[PageKey.RiskEventBoard] = new CardDefinition(
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
            Validator: BuildRiskEventValidator());

        dict[PageKey.RectificationTracker] = new CardDefinition(
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
            Validator: null);
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
