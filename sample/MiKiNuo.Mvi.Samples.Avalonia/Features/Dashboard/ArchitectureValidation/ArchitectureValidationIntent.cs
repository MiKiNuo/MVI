using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心意图。
/// </summary>
public abstract partial record ArchitectureValidationIntent : IMviIntent
{
    /// <summary>
    /// 表示更新当前架构验证上下文的意图。
    /// </summary>
    /// <param name="ActiveContext">当前业务上下文。</param>
    /// <param name="FlowStatus">当前流程状态。</param>
    public sealed partial record UpdateContext(string ActiveContext, string FlowStatus) : ArchitectureValidationIntent;

    /// <summary>
    /// 表示追加交互日志的意图。
    /// </summary>
    /// <param name="Message">日志消息。</param>
    public sealed partial record AppendInteractionLog(string Message) : ArchitectureValidationIntent;
}
