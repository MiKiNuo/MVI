using MiKiNuo.Mvi.Samples.Godot.Features.Common;

namespace MiKiNuo.Mvi.Samples.Godot.Features.AppShell;

/// <summary>
/// 表示游戏应用壳顶层页面 ViewModel 的工厂。
/// <para>
/// 父 <see cref="AppShellViewModel"/> 仅持有 <see cref="GameScreenKeys"/> 判别器，
/// 不再在 <see cref="AppShellState"/> 中直接存放 <c>LoginViewModel</c> 与 <c>LobbyViewModel</c> 引用。
/// View 层在 <c>CurrentScreen</c> 变化时通过此工厂按需解析顶层页面 VM。
/// </para>
/// </summary>
public interface IGameScreenFactory
{
    /// <summary>
    /// 根据 <paramref name="screenKey"/> 创建对应的顶层页面 ViewModel。
    /// </summary>
    /// <param name="screenKey">页面键（<see cref="GameScreenKeys"/> 中之一）。</param>
    /// <returns>顶层页面 ViewModel；未识别 screenKey 时返回 null。</returns>
    public object? CreateScreen(string screenKey);
}
