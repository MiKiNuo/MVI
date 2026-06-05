namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 仪表板卡片注册表的检验医嘱域分部。
/// 负责医嘱开立、标本流转、危急值闭环、TAT 监控 4 张卡片的 <see cref="CardDefinition"/> 注册与检验医嘱验证器构造。
/// </summary>
public static partial class DashboardCardRegistry
{
    private static partial void AddLabDefinitions(Dictionary<PageKey, CardDefinition> dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        dict[PageKey.LabOrderComposer] = new CardDefinition(
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
            Validator: BuildLabOrderValidator());

        dict[PageKey.SpecimenTracker] = new CardDefinition(
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
            Validator: null);

        dict[PageKey.CriticalValueMonitor] = new CardDefinition(
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
            Validator: null);

        dict[PageKey.LabTurnaroundBoard] = new CardDefinition(
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
            Validator: null);
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
}
