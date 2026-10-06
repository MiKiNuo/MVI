using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Register;
using MiKiNuo.Mvi.Samples.Avalonia.Features.ResetPassword;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Home;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;
namespace MiKiNuo.Mvi.Samples.Avalonia.Composition;
/// <summary>仍使用原有声明式组合机制；每个页面拥有独立 Handler、Reducer、Store 和 ViewModel。</summary>
[MviComposition(typeof(AppShellHandler), typeof(LoginHandler), typeof(RegisterHandler), typeof(ResetPasswordHandler), typeof(HomeHandler))]
public sealed partial class AppComposition { }
