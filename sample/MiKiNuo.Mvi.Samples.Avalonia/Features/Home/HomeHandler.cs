using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
/// <summary>处理大厅意图，并通过原有中介契约请求导航。</summary>
/// <param name="mediator">范围内中介者。</param>
[MviFeature]
public sealed partial class HomeHandler(IMviMediator mediator) : MviIntentHandler<HomeState, HomeIntent>
{
    [MviHandle(typeof(HomeIntent.ShowUser))]
    private ValueTask Show(HomeIntent.ShowUser intent, IIntentContext<HomeState> context, CancellationToken token)
    { context.Reduce(new HomeMutation(intent.DisplayName)); return ValueTask.CompletedTask; }
    [MviHandle(typeof(HomeIntent.Logout))]
    private async ValueTask Logout(HomeIntent.Logout intent, IIntentContext<HomeState> context, CancellationToken token)
    { context.Reduce(new HomeMutation(string.Empty)); await mediator.SendAsync(new NavigateToPageRequest(ShellPage.Login), token); }
    /// <summary>同步接纳通知，不在发布者回调中等待本地业务链。</summary>
    /// <param name="notification">已发生的导航事实。</param>
    [MviNotificationAcceptor(typeof(HomeEnteredNotification))]
    public void OnHomeEntered(HomeEnteredNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (!TryAcceptIntent(new HomeIntent.ShowUser(notification.DisplayName))) throw new InvalidOperationException("大厅队列已满或已关闭。");
    }
}
