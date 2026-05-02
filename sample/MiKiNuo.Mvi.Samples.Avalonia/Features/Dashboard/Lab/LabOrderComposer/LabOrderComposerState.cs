using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabOrderComposer;

/// <summary>
/// 表示医嘱开立 MVI 状态。
/// </summary>
/// <param name="Title">标题。</param>
/// <param name="MainValue">核心指标。</param>
/// <param name="StatusText">状态文本。</param>
/// <param name="DetailText">详情文本。</param>
/// <param name="ActionLog">动作日志。</param>
/// <param name="PrimaryActionText">主动作文本。</param>
/// <param name="SecondaryActionText">辅助动作文本。</param>
/// <param name="CanPrimaryAction">是否允许执行主动作。</param>
/// <param name="CanSecondaryAction">是否允许执行辅助动作。</param>
/// <param name="PatientIdentity">患者标识。</param>
/// <param name="TestItem">检验项目。</param>
/// <param name="PriorityLevel">优先级。</param>
/// <param name="SpecimenType">标本类型。</param>
/// <param name="ClinicalIndication">临床指征。</param>
/// <param name="SubmitOrderText">提交医嘱按钮文本。</param>
/// <param name="CanSubmitOrder">是否允许提交医嘱。</param>
public sealed record LabOrderComposerState(
    string Title,
    string MainValue,
    string StatusText,
    string DetailText,
    string ActionLog,
    string PrimaryActionText,
    string SecondaryActionText,
    bool CanPrimaryAction,
    bool CanSecondaryAction,
    string PatientIdentity,
    string TestItem,
    string PriorityLevel,
    string SpecimenType,
    string ClinicalIndication,
    string SubmitOrderText,
    bool CanSubmitOrder) : IMviState
{
    /// <summary>
    /// 获取初始状态。
    /// </summary>
    public static LabOrderComposerState Initial { get; } = new(
        "检验医嘱录入 MVI",
        "待开立 72 条",
        "等待医生录入医嘱",
        "医生录入患者、检验项目、标本和指征后提交，标本流转、危急值和 TAT 组件会联动更新。",
        "等待录入检验医嘱。",
        "开立急诊检验",
        "保存草稿",
        true,
        true,
        string.Empty,
        string.Empty,
        "常规",
        string.Empty,
        string.Empty,
        "提交检验医嘱",
        false);
}
