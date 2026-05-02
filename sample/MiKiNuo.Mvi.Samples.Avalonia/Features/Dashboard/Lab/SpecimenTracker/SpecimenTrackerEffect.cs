using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.SpecimenTracker;

/// <summary>
/// 表示标本流转 MVI副作用。
/// </summary>
public abstract partial record SpecimenTrackerEffect : IMviEffect
{
    /// <summary>
    /// 表示请求主业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestPrimaryWorkflow(string ContextText) : SpecimenTrackerEffect;

    /// <summary>
    /// 表示请求辅助业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestSecondaryWorkflow(string ContextText) : SpecimenTrackerEffect;
}
