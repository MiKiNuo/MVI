namespace MiKiNuo.Mvi.Application.DI;

/// <summary>
/// 表示 MVI DI 运行时异常。
/// </summary>
public class MviDiException : Exception
{
    /// <summary>
    /// 初始化 MVI DI 运行时异常。
    /// </summary>
    /// <param name="message">异常消息。</param>
    public MviDiException(string message)
        : base(message ?? throw new ArgumentNullException(nameof(message)))
    {
    }
}
