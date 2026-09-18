using MiKiNuo.Mvi.Domain.DI;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

namespace MiKiNuo.Mvi.Samples.Avalonia.Composition;

/// <summary>
/// 表示示例应用组合声明：应用壳、登录、注册、重置密码与主页 Feature 的装配与接线。
/// </summary>
/// <remarks>
/// 源生成器据此 emit 组合构建器 <c>CreateAppCompositionAsync</c>：
/// 创建组合范围与五个成员实例，注册导航路由（应用壳为唯一提供方），
/// 并把各消费方与主页的进入通知订阅接线完成。
/// </remarks>
[MviComposition(
    typeof(AppShellReducer),
    typeof(LoginReducer),
    typeof(RegisterReducer),
    typeof(ResetPasswordReducer),
    typeof(HomeReducer))]
public sealed partial class AppComposition
{
}
