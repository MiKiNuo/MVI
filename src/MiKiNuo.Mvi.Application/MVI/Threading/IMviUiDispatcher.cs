namespace MiKiNuo.Mvi.Application.MVI.Threading;

/// <summary>
/// 表示 MVI 框架内的 UI 线程调度抽象。
/// </summary>
/// <remarks>
/// 平台无关的 <c>Post</c> 契约由各平台实现（如 Avalonia <c>Dispatcher.UIThread</c>、Godot <c>CallDeferred</c>），
/// Application 层只依赖此抽象以避免反向引用平台程序集。
/// </remarks>
public interface IMviUiDispatcher
{
    /// <summary>
    /// 将操作投递到 UI 线程。
    /// </summary>
    /// <param name="action">需要在 UI 线程上执行的操作。</param>
    public void Post(Action action);
}
