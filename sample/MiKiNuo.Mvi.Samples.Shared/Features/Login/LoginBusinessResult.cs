using MiKiNuo.Mvi.Domain.MVI.Business;

namespace MiKiNuo.Mvi.Samples.Shared.Features.Login;

/// <summary>
/// 表示登录业务结果基类。
/// </summary>
public abstract record LoginBusinessResult : IMviBusinessResult
{
    /// <summary>
    /// 表示登录成功业务结果。
    /// </summary>
    /// <param name="Profile">登录用户资料。</param>
    public sealed record Success(ILoginProfile Profile) : LoginBusinessResult;

    /// <summary>
    /// 表示登录失败业务结果。
    /// </summary>
    /// <param name="ErrorMessage">错误消息。</param>
    public sealed record Failure(string ErrorMessage) : LoginBusinessResult;
}
