using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示仪表板卡片的统一副作用。
/// EffectDispatcher 接收这些副作用后调用 Mediator 把消息转交给协调者。
/// </summary>
public abstract partial record CardEffect : IMviEffect
{
    /// <summary>请求主业务工作流。</summary>
    /// <param name="ContextText">业务上下文文本。</param>
    public sealed partial record RequestPrimaryWorkflow(string ContextText) : CardEffect;

    /// <summary>请求辅助业务工作流。</summary>
    /// <param name="ContextText">业务上下文文本。</param>
    public sealed partial record RequestSecondaryWorkflow(string ContextText) : CardEffect;

    /// <summary>请求 Form 提交业务。仅对 Form Card 有效。</summary>
    /// <param name="FormValues">提交时的字段值集合（结构化载荷，由 EffectDispatcher 解析为 <see cref="Patient"/>）。</param>
    /// <param name="ContextText">提交上下文文本，用于 Mediator 日志。</param>
    public sealed partial record RequestFormSubmission(
        IReadOnlyList<CardFormValueEntry> FormValues,
        string ContextText) : CardEffect;
}
