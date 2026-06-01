namespace MiKiNuo.Mvi.Application.MVI.Threading;

/// <summary>
/// 表示 MVI UI 通知调度器。
/// </summary>
public static class MviUiNotificationDispatcher
{
    private static readonly object s_gate = new();
    private static Action<Action>? s_post;

    /// <summary>
    /// 配置 UI 通知投递器，并返回可恢复上一层配置的作用域。
    /// </summary>
    /// <param name="post">将操作投递到 UI 线程的委托。</param>
    /// <returns>释放时恢复上一层 UI 通知投递器配置的作用域。</returns>
    public static IDisposable Configure(Action<Action> post)
    {
        ArgumentNullException.ThrowIfNull(post);

        lock (s_gate)
        {
            Action<Action>? previous = s_post;
            s_post = post;
            return new DispatcherConfiguration(previous);
        }
    }

    /// <summary>
    /// 将操作投递到 UI 线程；如果未配置 UI 调度器，则在当前线程执行。
    /// </summary>
    /// <param name="action">需要执行的操作。</param>
    public static void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Action<Action>? post;
        lock (s_gate)
        {
            post = s_post;
        }

        if (post is null)
        {
            action();
            return;
        }

        post(action);
    }

    private sealed class DispatcherConfiguration(Action<Action>? previous) : IDisposable
    {
        private bool _isDisposed;

        public void Dispose()
        {
            lock (s_gate)
            {
                if (_isDisposed)
                {
                    return;
                }

                s_post = previous;
                _isDisposed = true;
            }
        }
    }
}
