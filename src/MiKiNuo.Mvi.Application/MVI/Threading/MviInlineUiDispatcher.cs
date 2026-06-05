namespace MiKiNuo.Mvi.Application.MVI.Threading;

/// <summary>
/// 表示同步执行的 <see cref="IMviUiDispatcher"/>。
/// </summary>
/// <remarks>
/// 用作未配置平台调度器时的安全默认实现：
/// 直接在当前线程执行回调，避免依赖静态可变状态。
/// 测试代码或非 UI 场景（如后台 worker）通常使用此实现。
/// </remarks>
public sealed class MviInlineUiDispatcher : IMviUiDispatcher
{
    /// <summary>
    /// 获取全局共享的同步调度器实例。
    /// </summary>
    public static MviInlineUiDispatcher Instance { get; } = new();

    private MviInlineUiDispatcher()
    {
    }

    /// <inheritdoc />
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action();
    }
}
