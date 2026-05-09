namespace MiKiNuo.Mvi.Application.MVI.Diagnostics;

/// <summary>
/// 表示内存中的 MVI 数据流诊断接收器。
/// 使用 ReaderWriterLockSlim 允许多个读操作并发执行，
/// 写操作独占锁，减少高并发诊断场景下的锁竞争。
/// </summary>
public sealed class MviMemoryDiagnosticSink : IMviDiagnosticSink, IDisposable
{
    private readonly List<MviDiagnosticEntry> _entries = [];
    private readonly ReaderWriterLockSlim _rwLock = new();
    private bool _isDisposed;

    /// <summary>
    /// 获取诊断条目快照。
    /// </summary>
    public IReadOnlyList<MviDiagnosticEntry> Entries
    {
        get
        {
            _rwLock.EnterReadLock();

            try
            {
                return _entries.ToArray();
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }
    }

    /// <inheritdoc />
    public void Record(MviDiagnosticEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _rwLock.EnterWriteLock();

        try
        {
            _entries.Add(entry);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _rwLock.Dispose();
        _isDisposed = true;
    }
}