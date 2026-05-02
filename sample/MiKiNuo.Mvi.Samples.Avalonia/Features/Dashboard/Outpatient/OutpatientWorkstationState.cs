using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;

/// <summary>
/// 表示门诊工作站页面状态。
/// </summary>
/// <param name="QueueViewModel">候诊队列 ViewModel。</param>
/// <param name="ClinicalEditorViewModel">电子病历编辑 ViewModel。</param>
/// <param name="ClinicalReminderViewModel">临床提醒 ViewModel。</param>
public sealed record OutpatientWorkstationState(
    object QueueViewModel,
    object ClinicalEditorViewModel,
    object ClinicalReminderViewModel) : IMviState;
