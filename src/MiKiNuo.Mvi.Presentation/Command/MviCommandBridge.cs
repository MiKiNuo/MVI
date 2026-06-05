using System.Windows.Input;

namespace MiKiNuo.Mvi.Presentation.Command;

/// <summary>
/// 提供 <see cref="IMviCommandBridge"/> 的默认实现，把 MVI 命令包装为
/// 转发调用与事件的 <see cref="ICommand"/> 适配器。
/// </summary>
public sealed class MviCommandBridge : IMviCommandBridge
{
    /// <summary>
    /// 获取进程级单例，避免在每次绑定时分配新的工厂实例。
    /// </summary>
    public static MviCommandBridge Instance { get; } = new();

    private MviCommandBridge()
    {
    }

    /// <inheritdoc />
    public ICommand Adapt(IMviCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new MviCommandAdapter(command);
    }

    /// <summary>
    /// 把指定的 MVI 命令包装为 <see cref="ICommand"/> 适配器，静态便捷入口。
    /// 等价于 <c>MviCommandBridge.Instance.Adapt(command)</c>。
    /// </summary>
    /// <param name="command">MVI 命令实例。</param>
    /// <returns>实现 <see cref="ICommand"/> 的包装器。</returns>
    public static ICommand ToICommand(IMviCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new MviCommandAdapter(command);
    }

    /// <summary>
    /// <see cref="ICommand"/> 适配器，把 <see cref="CanExecute(object?)"/>、
    /// <see cref="Execute(object?)"/> 与 <see cref="CanExecuteChanged"/> 事件
    /// 全部转发到底层 <see cref="IMviCommand"/>。
    /// </summary>
    private sealed class MviCommandAdapter : ICommand
    {
        private readonly IMviCommand _command;

        public MviCommandAdapter(IMviCommand command)
        {
            _command = command;
        }

        /// <inheritdoc />
        public bool CanExecute(object? parameter)
        {
            return _command.CanExecute(parameter);
        }

        /// <inheritdoc />
        public void Execute(object? parameter)
        {
            _command.Execute(parameter);
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged
        {
            add => _command.CanExecuteChanged += value;
            remove => _command.CanExecuteChanged -= value;
        }
    }
}
