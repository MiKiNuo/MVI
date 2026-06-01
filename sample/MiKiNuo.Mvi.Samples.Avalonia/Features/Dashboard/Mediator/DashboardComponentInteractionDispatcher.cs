using MiKiNuo.Mvi.Application.MVI.Mediator;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

/// <summary>
/// 表示 Dashboard 子组件交互动作键。
/// </summary>
public static class DashboardComponentActionKeys
{
    /// <summary>
    /// 表示主流程动作。
    /// </summary>
    public const string Primary = "Primary";

    /// <summary>
    /// 表示次流程动作。
    /// </summary>
    public const string Secondary = "Secondary";
}

/// <summary>
/// 表示 Dashboard 子组件通过中介者请求父子协作的共享分发入口。
/// </summary>
public static class DashboardComponentInteractionDispatcher
{
    /// <summary>
    /// 发送 Dashboard 子组件交互请求。
    /// </summary>
    /// <param name="mediator">真正 Request/Response 中介者。</param>
    /// <param name="pageKey">页面键。</param>
    /// <param name="sourceComponent">来源组件。</param>
    /// <param name="actionKey">动作键。</param>
    /// <param name="contextText">上下文文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Dashboard 子组件交互响应。</returns>
    public static ValueTask<DashboardComponentInteractionResponse> SendComponentInteractionAsync(
        this IMviMediator mediator,
        string pageKey,
        string sourceComponent,
        string actionKey,
        string contextText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceComponent);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionKey);
        ArgumentNullException.ThrowIfNull(contextText);

        return mediator.SendAsync<DashboardComponentInteractionRequest, DashboardComponentInteractionResponse>(
            new DashboardComponentInteractionRequest(pageKey, sourceComponent, actionKey, contextText),
            cancellationToken);
    }
}
