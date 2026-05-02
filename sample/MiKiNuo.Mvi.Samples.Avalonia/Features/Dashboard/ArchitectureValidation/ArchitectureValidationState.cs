using MiKiNuo.Mvi.Domain.MVI.State;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;

/// <summary>
/// 表示架构验证中心状态。
/// </summary>
/// <param name="Title">页面标题。</param>
/// <param name="Description">页面说明。</param>
/// <param name="PatientSearchViewModel">复用患者检索 ViewModel。</param>
/// <param name="AuditTimelineViewModel">复用审计时间线 ViewModel。</param>
/// <param name="MiddlewareMetricViewModel">中间件指标卡片 ViewModel。</param>
/// <param name="ReuseMetricViewModel">复用指标卡片 ViewModel。</param>
/// <param name="MediatorMetricViewModel">中介者指标卡片 ViewModel。</param>
/// <param name="EffectMetricViewModel">副作用指标卡片 ViewModel。</param>
/// <param name="ActiveContext">当前业务上下文。</param>
/// <param name="FlowStatus">当前流程状态。</param>
/// <param name="InteractionLog">交互日志。</param>
public sealed record ArchitectureValidationState(
    string Title,
    string Description,
    object PatientSearchViewModel,
    object AuditTimelineViewModel,
    object MiddlewareMetricViewModel,
    object ReuseMetricViewModel,
    object MediatorMetricViewModel,
    object EffectMetricViewModel,
    string ActiveContext,
    string FlowStatus,
    string InteractionLog) : IMviState;
