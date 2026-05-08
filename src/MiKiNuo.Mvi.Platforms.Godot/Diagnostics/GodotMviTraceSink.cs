using System;
using System.Collections.Generic;
using System.Text;

namespace MiKiNuo.Mvi.Platforms.Godot.Diagnostics;

/// <summary>
/// 表示 Godot 示例和调试面板使用的 MVI 跟踪输出接收器。
/// </summary>
public sealed class GodotMviTraceSink
{
    private readonly object _syncRoot = new();
    private readonly List<string> _lines = new();

    /// <summary>
    /// 获取当前跟踪快照文本。
    /// </summary>
    public string SnapshotText
    {
        get
        {
            lock (_syncRoot)
            {
                return BuildSnapshotTextCore();
            }
        }
    }

    /// <summary>
    /// 添加一行跟踪文本。
    /// </summary>
    /// <param name="message">跟踪文本。</param>
    public void Append(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_syncRoot)
        {
            _lines.Add($"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}");
        }
    }

    /// <summary>
    /// 清理跟踪输出。
    /// </summary>
    public void Clear()
    {
        lock (_syncRoot)
        {
            _lines.Clear();
        }
    }

    /// <summary>
    /// 获取当前跟踪行快照。
    /// </summary>
    /// <returns>跟踪行快照。</returns>
    public IReadOnlyList<string> Snapshot()
    {
        lock (_syncRoot)
        {
            return _lines.ToArray();
        }
    }

    private string BuildSnapshotTextCore()
    {
        if (_lines.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        foreach (string line in _lines)
        {
            builder.AppendLine(line);
        }

        return builder.ToString();
    }
}
