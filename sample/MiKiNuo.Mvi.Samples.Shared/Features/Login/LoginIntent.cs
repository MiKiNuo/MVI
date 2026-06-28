using MiKiNuo.Mvi.Domain.MVI.Intent;

namespace MiKiNuo.Mvi.Samples.Shared.Features.Login;

/// <summary>
/// 表示登录界面意图。
/// </summary>
public abstract partial record LoginIntent : IMviIntent
{
    /// <summary>
    /// 表示修改账号意图。
    /// </summary>
    /// <param name="UserName">用户账号。</param>
    public sealed partial record ChangeUserName(string UserName) : LoginIntent;

    /// <summary>
    /// 表示修改密码意图。
    /// </summary>
    /// <param name="Password">用户密码。</param>
    public sealed partial record ChangePassword(string Password) : LoginIntent;

    /// <summary>
    /// 表示提交登录意图。
    /// </summary>
    public sealed partial record Submit : LoginIntent;
}
