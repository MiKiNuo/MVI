namespace MiKiNuo.Mvi.Application.MVI.Diagnostics;

/// <summary>
/// 表示内存中的 MVI 数据流诊断接收器。
/// </summary>
public sealed class MviMemoryDiagnosticSink : IMviDiagnosticSink
{
    private readonly List<MviDiagnosticEntry> _entries = [];
    private readonly object _syncRoot = new();

    /// <summary>
    /// 获取诊断条目快照。
    /// </summary>
    public IReadOnlyList<MviDiagnosticEntry> Entries
    {
        get
        {
            lock (_syncRoot)
            {
                return _entries.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void Record(MviDiagnosticEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_syncRoot)
        {
            _entries.Add(entry);
        }
    }
}
