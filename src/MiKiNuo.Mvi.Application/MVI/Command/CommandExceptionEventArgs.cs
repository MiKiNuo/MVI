namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示 MVI 命令在异步执行路径中抛出的未捕获异常及其触发参数。
/// </summary>
public sealed class CommandExceptionEventArgs : EventArgs
{
    /// <summary>
    /// 初始化命令异常事件参数。
    /// </summary>
    /// <param name="exception">未捕获的异常实例。</param>
    /// <param name="parameter">触发命令执行时的参数。</param>
    public CommandExceptionEventArgs(Exception exception, object? parameter)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception = exception;
        Parameter = parameter;
    }

    /// <summary>
    /// 获取未捕获的异常。
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// 获取触发命令执行的参数。
    /// </summary>
    public object? Parameter { get; }
}
