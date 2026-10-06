using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
/// <summary>处理壳的导航；原有组合生成器仍负责请求接线。</summary>
/// <param name="endpoint">当前组件的通信端点。</param>
[MviFeature]
public sealed partial class AppShellHandler(MviMediatorEndpoint endpoint) : MviIntentHandler<AppShellState, AppShellIntent>
{
    [MviHandle(typeof(AppShellIntent))]
    private async ValueTask NavigateAsync(AppShellIntent intent, IIntentContext<AppShellState> context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        // 导航不是多个 Store 的原子事务；每个组件只修改自己的状态。
        context.Reduce(new AppShellMutation(intent.Page, intent.DisplayName));
        if (intent.Page == ShellPage.Home)
        {
            MviNotificationReport report = await endpoint.PublishAsync(new HomeEnteredNotification(intent.DisplayName ?? string.Empty), token);
            if (report.Deliveries.Any(d => d.Status != MviNotificationDeliveryStatus.Accepted))
                throw new InvalidOperationException("大厅未能接纳用户信息。");
        }
    }
    /// <summary>将外部导航契约转为本组件意图；不是 ViewModel 转发方法。</summary>
    /// <param name="request">导航请求。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>请求处理完成。</returns>
    [MviRouteHandler(typeof(NavigateToPageRequest))]
    public async ValueTask<bool> NavigateAsync(NavigateToPageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await DispatchIntentAsync(new AppShellIntent(request.Page, request.DisplayName), cancellationToken);
        return true;
    }
}
