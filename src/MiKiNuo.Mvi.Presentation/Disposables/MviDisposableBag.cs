using MiKiNuo.Mvi.Application.MVI.EventBinding;

namespace MiKiNuo.Mvi.Presentation.Disposables;

/// <summary>
/// 表示 MVI View 绑定生命周期内统一使用的可释放资源集合。
/// Avalonia <c>MviAvaloniaView</c> 与 Godot <c>GodotMviControlView</c> 共用同一实现。
/// </summary>
/// <remarks>
/// 与 Godot 版本一致地处理"已释放后追加"的竞态：再追加的资源会立即被释放而不是泄漏。
/// <see cref="Add(Action)"/> 重载把 <see cref="Action"/> 包装为 <see cref="IDisposable"/>，
/// 内部复用 Application 层的 <see cref="ActionDisposable"/> 用 <c>Interlocked.Exchange</c> 防止重复执行。
/// </remarks>
public sealed class MviDisposableBag : IDisposable
{
    private readonly List<IDisposable> _items = new();
    private bool _disposed;

    /// <summary>
    /// 添加需要释放的资源。
    /// 集合已释放时直接释放该资源，避免泄漏。
    /// </summary>
    /// <param name="disposable">需要释放的资源。</param>
    public void Add(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);

        if (_disposed)
        {
            disposable.Dispose();
            return;
        }

        _items.Add(disposable);
    }

    /// <summary>
    /// 添加需要在解绑时执行的清理动作。
    /// </summary>
    /// <param name="disposeAction">清理动作。</param>
    public void Add(Action disposeAction)
    {
        ArgumentNullException.ThrowIfNull(disposeAction);
        Add(new ActionDisposable(disposeAction));
    }

    /// <summary>
    /// 释放所有绑定资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (int index = _items.Count - 1; index >= 0; index--)
        {
            _items[index].Dispose();
        }

        _items.Clear();
    }
}
