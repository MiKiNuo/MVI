using MiKiNuo.Mvi.Domain.Common.Errors;

namespace MiKiNuo.Mvi.Domain.Common.Results;

/// <summary>
/// 表示无返回值操作结果。
/// </summary>
public readonly record struct OperationResult
{
    /// <summary>
    /// 初始化操作结果。
    /// </summary>
    /// <param name="isSuccess">是否成功。</param>
    /// <param name="failureReason">失败原因。</param>
    private OperationResult(bool isSuccess, DomainError? failureReason)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
    }

    /// <summary>
    /// 获取是否成功。
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 获取是否失败。
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// 获取失败原因。
    /// </summary>
    public DomainError? FailureReason { get; }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <returns>成功结果。</returns>
    public static OperationResult Success()
    {
        return new OperationResult(true, null);
    }

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="failureReason">失败原因。</param>
    /// <returns>失败结果。</returns>
    public static OperationResult Failure(DomainError failureReason)
    {
        return new OperationResult(false, failureReason);
    }
}
