using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.NursingTaskBoard;

/// <summary>
/// 表示护理任务 MVI副作用。
/// </summary>
public abstract partial record NursingTaskBoardEffect : IMviEffect
{
    /// <summary>
    /// 表示请求主业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestPrimaryWorkflow(string ContextText) : NursingTaskBoardEffect;

    /// <summary>
    /// 表示请求辅助业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestSecondaryWorkflow(string ContextText) : NursingTaskBoardEffect;
}
