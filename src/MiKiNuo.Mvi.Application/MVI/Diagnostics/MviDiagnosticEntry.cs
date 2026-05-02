namespace MiKiNuo.Mvi.Application.MVI.Diagnostics;

/// <summary>
/// 表示 MVI 数据流诊断条目。
/// </summary>
/// <param name="Timestamp">记录时间。</param>
/// <param name="ComponentName">组件名称。</param>
/// <param name="Stage">数据流阶段。</param>
/// <param name="Message">诊断消息。</param>
/// <param name="ElapsedMilliseconds">耗时毫秒数。</param>
public sealed record MviDiagnosticEntry(
    DateTimeOffset Timestamp,
    string ComponentName,
    string Stage,
    string Message,
    long ElapsedMilliseconds)
{
    /// <summary>
    /// 格式化为界面可读文本。
    /// </summary>
    /// <returns>界面可读的诊断文本。</returns>
    public string ToDisplayText()
    {
        return $"{Timestamp:HH:mm:ss.fff} · [{ComponentName}] {Stage} · {Message} · {ElapsedMilliseconds}ms";
    }
}
