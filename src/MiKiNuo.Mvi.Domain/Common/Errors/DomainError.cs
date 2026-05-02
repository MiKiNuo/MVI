namespace MiKiNuo.Mvi.Domain.Common.Errors;

/// <summary>
/// 表示领域错误信息。
/// </summary>
/// <param name="Code">错误编码。</param>
/// <param name="Message">错误消息。</param>
public sealed record DomainError(string Code, string Message);
