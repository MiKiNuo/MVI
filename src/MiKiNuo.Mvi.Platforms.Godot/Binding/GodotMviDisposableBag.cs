using System;
using System.Collections.Generic;
using System.Threading;

namespace MiKiNuo.Mvi.Platforms.Godot.Binding;

/// <summary>
/// 表示 Godot View 绑定生命周期内使用的可释放资源集合。
/// </summary>
public sealed class GodotMviDisposableBag : IDisposable
{
    private readonly List<IDisposable> _items = new();
    private bool _disposed;

    /// <summary>
    /// 添加需要释放的资源。
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

    private sealed class ActionDisposable : IDisposable
    {
        private Action? _disposeAction;

        public ActionDisposable(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
        }
    }
}
