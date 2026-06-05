namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 仪表板卡片注册表的住院床位域分部。
/// 负责床位总览、护理任务、病区风险、入院流程 4 张卡片的 <see cref="CardDefinition"/> 注册与入院登记验证器构造。
/// </summary>
public static partial class DashboardCardRegistry
{
    private static partial void AddInpatientDefinitions(Dictionary<PageKey, CardDefinition> dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        dict[PageKey.BedOverview] = new CardDefinition(
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
            Validator: null);

        dict[PageKey.NursingTaskBoard] = new CardDefinition(
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
            Validator: null);

        dict[PageKey.WardRiskPanel] = new CardDefinition(
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
            Validator: null);

        dict[PageKey.AdmissionCoordinator] = new CardDefinition(
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
            Validator: BuildAdmissionValidator());
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
}
