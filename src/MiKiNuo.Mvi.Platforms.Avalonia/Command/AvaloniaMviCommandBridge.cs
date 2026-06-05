using System.Windows.Input;
using MiKiNuo.Mvi.Application.MVI.Command;
using MiKiNuo.Mvi.Presentation.Command;

namespace MiKiNuo.Mvi.Platforms.Avalonia.Command;

/// <summary>
/// 表示 Avalonia 平台专用的 MVI 命令桥接器。
/// </summary>
/// <remarks>
/// 区别于 <see cref="MviCommandBridge"/>：
/// 1. 维护 Avalonia UI 线程语义：<see cref="ICommand.CanExecuteChanged"/> 在订阅/解订时
///    自动接入 <c>RequerySuggested</c> 风格的全局刷新信号，调用方在 Avalonia 路由
///    命令桥到 <c>Button.Command</c> 等内置 <c>ICommand</c> 属性时无需额外接线；
/// 2. 当 ViewModel 注入了 <c>MiKiNuo.Mvi.Application.MVI.Threading.IMviUiDispatcher</c> 时，
///    桥接器复用同一调度器触发 <c>CanExecuteChanged</c>，避免跨线程访问问题。
/// 平台无关场景请使用 <see cref="MviCommandBridge"/>，Avalonia 内置 <c>Button.Command</c>
///    绑定等需要 <c>ICommand</c> 的场景应使用本类。
/// </remarks>
public sealed class AvaloniaMviCommandBridge : IMviCommandBridge
{
    /// <summary>
    /// 获取 Avalonia 平台单例。
    /// </summary>
    public static AvaloniaMviCommandBridge Instance { get; } = new();

    private AvaloniaMviCommandBridge()
    {
    }

    /// <inheritdoc />
    public ICommand Adapt(IMviCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new AvaloniaMviCommandAdapter(command);
    }

    /// <summary>
    /// Avalonia 平台专用的 <see cref="ICommand"/> 适配器。
    /// 除把 <c>CanExecute</c>、<c>Execute</c> 转发到 <see cref="IMviCommand"/> 外，
    /// 还提供 <c>CanExecuteSuggested</c> 静态事件，让 Avalonia 控件在收到
    /// <c>CommandManager.InvalidateRequerySuggested</c> 风格的全局通知时刷新可用态。
    /// </summary>
    private sealed class AvaloniaMviCommandAdapter : ICommand
    {
        private readonly IMviCommand _command;

        public AvaloniaMviCommandAdapter(IMviCommand command)
        {
            _command = command;
        }

        /// <summary>
        /// 平台无关命令桥接：<c>MviCommandBridge.ToICommand</c>。
        /// </summary>
        public static ICommand Wrap(IMviCommand command) => MviCommandBridge.ToICommand(command);

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
