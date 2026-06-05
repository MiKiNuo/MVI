using MiKiNuo.Mvi.Application.MVI.Command;

namespace MiKiNuo.Mvi.Presentation.Events;

/// <summary>
/// 表示跨平台 ViewEvent 到 MVI 命令的运行时绑定。
/// </summary>
/// <remarks>
/// 内部只依赖平台无关的 <see cref="IMviCommand"/>：
/// 防抖、payload 转发、可执行判断等核心逻辑都不依赖 <c>System.Windows.Input</c>。
/// 平台层需要 <c>ICommand</c> 时通过 <see cref="Command.IMviCommandBridge"/> 适配。
/// </remarks>
public sealed class MviViewEventCommandBinding : IDisposable
{
    private readonly object _gate = new();
    private readonly IMviCommand _command;
    private readonly MviViewEventBindingOptions _options;
    private CancellationTokenSource? _pendingDebounce;
    private bool _isDisposed;

    /// <summary>
    /// 初始化跨平台 ViewEvent 命令绑定。
    /// </summary>
    /// <param name="command">目标 MVI 命令。</param>
    /// <param name="options">绑定选项。</param>
    public MviViewEventCommandBinding(IMviCommand command, MviViewEventBindingOptions options)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(options);

        _command = command;
        _options = options;
    }

    /// <summary>
    /// 处理 ViewEvent 载荷。
    /// </summary>
    /// <param name="payload">事件载荷。</param>
    public void Handle(object? payload)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_options.Debounce is { } debounce && debounce > TimeSpan.Zero)
        {
            ScheduleDebounced(payload, debounce);
            return;
        }

        Execute(payload);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CancellationTokenSource? pendingDebounce;

        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            pendingDebounce = _pendingDebounce;
            _pendingDebounce = null;
        }

        pendingDebounce?.Cancel();
        GC.SuppressFinalize(this);
    }

    private void ScheduleDebounced(object? payload, TimeSpan debounce)
    {
        CancellationTokenSource cancellationTokenSource = new();
        CancellationTokenSource? previousDebounce;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            previousDebounce = _pendingDebounce;
            _pendingDebounce = cancellationTokenSource;
        }

        previousDebounce?.Cancel();
        _ = RunDebouncedAsync(payload, debounce, cancellationTokenSource);
    }

    private async Task RunDebouncedAsync(
        object? payload,
        TimeSpan debounce,
        CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            await Task.Delay(debounce, cancellationTokenSource.Token).ConfigureAwait(false);
            ExecuteDebounced(payload, cancellationTokenSource);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            ClearDebounce(cancellationTokenSource);
            cancellationTokenSource.Dispose();
        }
    }

    private void ExecuteDebounced(object? payload, CancellationTokenSource cancellationTokenSource)
    {
        lock (_gate)
        {
            if (_isDisposed || !ReferenceEquals(_pendingDebounce, cancellationTokenSource))
            {
                return;
            }

            _pendingDebounce = null;
        }

        Execute(payload);
    }

    private void ClearDebounce(CancellationTokenSource cancellationTokenSource)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_pendingDebounce, cancellationTokenSource))
            {
                _pendingDebounce = null;
            }
        }
    }

    private void Execute(object? payload)
    {
        if (_command.CanExecute(payload))
        {
            _command.Execute(payload);
        }
    }
}
