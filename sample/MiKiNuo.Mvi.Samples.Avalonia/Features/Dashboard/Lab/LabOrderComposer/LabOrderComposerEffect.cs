using MiKiNuo.Mvi.Domain.MVI.Effect;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Lab.LabOrderComposer;

/// <summary>
/// 表示医嘱开立 MVI 副作用。
/// </summary>
public abstract partial record LabOrderComposerEffect : IMviEffect
{
    /// <summary>
    /// 表示请求提交检验医嘱副作用。
    /// </summary>
    /// <param name="ContextText">检验医嘱上下文。</param>
    public sealed partial record RequestLabOrderSubmission(string ContextText) : LabOrderComposerEffect;

    /// <summary>
    /// 表示请求主业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestPrimaryWorkflow(string ContextText) : LabOrderComposerEffect;

    /// <summary>
    /// 表示请求辅助业务工作流副作用。
    /// </summary>
    /// <param name="ContextText">业务上下文。</param>
    public sealed partial record RequestSecondaryWorkflow(string ContextText) : LabOrderComposerEffect;
}
