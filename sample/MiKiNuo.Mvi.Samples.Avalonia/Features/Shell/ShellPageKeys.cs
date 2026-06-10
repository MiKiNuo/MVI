namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳顶层页面键。
/// <para>
/// 父 <see cref="AppShellState"/> 仅持有本页键字符串，View 层通过 <see cref="IShellPageFactory"/> 按需解析页面 ViewModel。
/// </para>
/// </summary>
public static class ShellPageKeys
{
    /// <summary>登录页键。</summary>
    public const string Login = "Login";

    /// <summary>事件绑定 Workbench 页键。</summary>
    public const string EventBindingWorkbench = "EventBindingWorkbench";
}
