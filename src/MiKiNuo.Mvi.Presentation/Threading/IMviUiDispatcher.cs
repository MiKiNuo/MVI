namespace MiKiNuo.Mvi.Presentation.Threading;

/// <summary>
/// 表示平台无关的 UI 调度器。
/// </summary>
public interface IMviUiDispatcher
{
    /// <summary>
    /// 在 UI 线程执行操作。
    /// </summary>
    /// <param name="action">操作。</param>
    public void Post(Action action);
}
