using MiKiNuo.Mvi.Binding.EventBinding;
namespace MiKiNuo.Mvi.Binding.Disposables;
/// <summary>拥有绑定订阅；并发 Add/Dispose 安全，逆序清理失败后仍继续释放其余资源。</summary>
public sealed class MviDisposableBag : IDisposable
{
    private readonly object _gate = new();
    private List<IDisposable>? _items = [];
    /// <summary>登记拥有的资源；集合已关闭时立即释放新资源。</summary>
    /// <param name="disposable">资源。</param>
    public void Add(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        lock (_gate)
        { if (_items is not null) { _items.Add(disposable); return; } }
        disposable.Dispose();
    }
    /// <summary>登记一次性清理动作。</summary>
    /// <param name="disposeAction">清理动作。</param>
    public void Add(Action disposeAction)
    {
        ArgumentNullException.ThrowIfNull(disposeAction);

        ActionDisposable? subscription = null;
        try
        {
            subscription = new ActionDisposable(disposeAction);
            Add(subscription);

            // 登记成功后由集合负责释放；集合已关闭时，Add 已立即释放资源。
            subscription = null;
        }
        finally
        {
            // 登记失败时回收尚未移交的资源；ActionDisposable 保证清理动作最多执行一次。
            subscription?.Dispose();
        }
    }
    /// <summary>先关闭登记入口，再在锁外逆序清理，最终聚合所有故障。</summary>
    public void Dispose()
    {
        List<IDisposable>? snapshot;
        lock (_gate) { snapshot = _items; _items = null; }
        if (snapshot is null) return;
        List<Exception> errors = [];
        for (int index = snapshot.Count - 1; index >= 0; index--)
            try { snapshot[index].Dispose(); } catch (Exception exception) { errors.Add(exception); }
        snapshot.Clear();
        GC.SuppressFinalize(this);
        if (errors.Count != 0) throw new AggregateException("绑定资源清理失败。", errors);
    }
}
