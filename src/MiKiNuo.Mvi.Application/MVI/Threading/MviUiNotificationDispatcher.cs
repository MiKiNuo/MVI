namespace MiKiNuo.Mvi.Application.MVI.Threading;

/// <summary>
/// 表示 MVI UI 通知调度器。
/// </summary>
public static class MviUiNotificationDispatcher
{
    private static Action<Action>? s_post;

    /// <summary>
    /// 配置 UI 通知投递器。
    /// </summary>
    /// <param name="post">将操作投递到 UI 线程的委托。</param>
    public static void Configure(Action<Action> post)
    {
        ArgumentNullException.ThrowIfNull(post);

        s_post = post;
    }

    /// <summary>
    /// 将操作投递到 UI 线程；如果未配置 UI 调度器，则在当前线程执行。
    /// </summary>
    /// <param name="action">需要执行的操作。</param>
    public static void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Action<Action>? post = s_post;
        if (post is null)
        {
            action();
            return;
        }

        post(action);
    }
}
