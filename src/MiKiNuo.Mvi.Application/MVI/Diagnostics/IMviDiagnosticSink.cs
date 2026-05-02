namespace MiKiNuo.Mvi.Application.MVI.Diagnostics;

/// <summary>
/// 表示 MVI 数据流诊断接收器。
/// </summary>
public interface IMviDiagnosticSink
{
    /// <summary>
    /// 记录诊断条目。
    /// </summary>
    /// <param name="entry">诊断条目。</param>
    public void Record(MviDiagnosticEntry entry);
}
