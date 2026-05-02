using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Quality.RiskEventBoard;

/// <summary>
/// 表示风险事件 MVI 副作用。
/// </summary>
public abstract partial record RiskEventBoardEffect : IMviEffect
{
    /// <summary>
    /// 表示请求提交风险事件副作用。
    /// </summary>
    /// <param name="ContextText">风险事件上下文。</param>
    public sealed partial record RequestRiskEventSubmission(string ContextText) : RiskEventBoardEffect;

    /// <summary>
    /// 表示请求主业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestPrimaryWorkflow(string ContextText) : RiskEventBoardEffect;

    /// <summary>
    /// 表示请求辅助业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestSecondaryWorkflow(string ContextText) : RiskEventBoardEffect;
}
