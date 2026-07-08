using System;
using System.Threading;
using System.Threading.Tasks;
using MiKiNuo.Mvi.Application.MVI.IntentHandler;
using MiKiNuo.Mvi.Domain.MVI.Business;
using MiKiNuo.Mvi.Samples.Shared.Features.Login;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Login;

/// <summary>
/// 表示登录界面意图处理器。
/// </summary>
public sealed class LoginIntentHandler
    : MviIntentHandlerBase<LoginState, LoginIntent, LoginEffect>
{
    private readonly IAuthService _authService;

    /// <summary>
    /// 初始化登录界面意图处理器。
    /// </summary>
    /// <param name="authService">认证服务。</param>
    public LoginIntentHandler(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// 处理具体业务逻辑。
    /// </summary>
    /// <param name="state">当前状态（已通过 null 检查）。</param>
    /// <param name="intent">用户意图（已通过 null 检查）。</param>
    /// <param name="cancellationToken">取消标记（已通过检查）。</param>
    /// <returns>业务结果;无业务时返回 null。</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1062:Validate arguments of public methods",
        Justification = "由基类统一验证参数。")]
    protected override async ValueTask<IMviBusinessResult?> HandleCoreAsync(
        LoginState state,
        LoginIntent intent,
        CancellationToken cancellationToken)
    {
        if (intent is LoginIntent.Submit && state.CanSubmit)
        {
            LoginResult result = await _authService
                .LoginAsync(state.UserName, state.Password, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && result.Profile is not null)
            {
                return new LoginBusinessResult.Success(result.Profile);
            }

            return new LoginBusinessResult.Failure(result.ErrorMessage ?? "登录失败。");
        }

        return null;
    }
}
