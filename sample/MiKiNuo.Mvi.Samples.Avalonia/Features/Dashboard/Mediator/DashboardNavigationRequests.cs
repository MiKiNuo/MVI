namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

/// <summary>
/// 表示请求导航到 Dashboard 业务页面。
/// </summary>
/// <param name="PageKey">页面键。</param>
public sealed record NavigateDashboardPageRequest(string PageKey);

/// <summary>
/// 表示 Dashboard 页面导航响应。
/// </summary>
/// <param name="PageTitle">页面标题。</param>
/// <param name="Changed">是否完成切换。</param>
public sealed record DashboardNavigationResponse(string PageTitle, bool Changed);

/// <summary>
/// 表示请求打开患者就诊上下文。
/// </summary>
/// <param name="PatientName">患者姓名。</param>
public sealed record OpenPatientEncounterRequest(string PatientName);

/// <summary>
/// 表示患者就诊上下文打开响应。
/// </summary>
/// <param name="PatientName">患者姓名。</param>
/// <param name="Changed">是否完成切换。</param>
public sealed record PatientEncounterResponse(string PatientName, bool Changed);

/// <summary>
/// 表示 Dashboard 子组件请求中介者协调生产业务流程。
/// </summary>
/// <param name="PageKey">页面键。</param>
/// <param name="SourceComponent">来源组件。</param>
/// <param name="ActionKey">动作键。</param>
/// <param name="ContextText">上下文文本。</param>
public sealed record DashboardComponentInteractionRequest(
    string PageKey,
    string SourceComponent,
    string ActionKey,
    string ContextText);

/// <summary>
/// 表示 Dashboard 子组件交互响应。
/// </summary>
/// <param name="Message">响应消息。</param>
/// <param name="Changed">是否产生状态变化。</param>
public sealed record DashboardComponentInteractionResponse(string Message, bool Changed);
