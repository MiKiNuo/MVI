namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Shell;

/// <summary>
/// 表示应用壳顶层页面 ViewModel 的工厂。
/// <para>
/// 父 <see cref="AppShellViewModel"/> 仅持有 <see cref="ShellPageKeys"/> 判别器，
/// 不再在 <see cref="AppShellState"/> 中直接存放页面 ViewModel 引用。
/// View 层在 <see cref="AppShellViewModel.CurrentPageKey"/> 变化时通过此工厂按需解析页面 VM。
/// </para>
/// </summary>
public interface IShellPageFactory
{
    /// <summary>
    /// 根据 <paramref name="pageKey"/> 创建对应的页面 ViewModel。
    /// </summary>
    /// <param name="pageKey">页面键（<see cref="ShellPageKeys"/> 中之一）。</param>
    /// <returns>页面 ViewModel；未识别 pageKey 时返回 null。</returns>
    public object? CreatePage(string pageKey);
}
