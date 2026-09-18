using MiKiNuo.Mvi.Application.MVI.Threading;

namespace MiKiNuo.Mvi.Samples.Avalonia.Composition;

/// <summary>
/// 表示示例应用组合根：创建生成的 DI 容器并启动应用组合。
/// </summary>
/// <remarks>
/// 各 Feature 实例与跨 Feature 接线由 [MviComposition] 源生成器装配，
/// 此处只做组合启动与窗口创建。
/// </remarks>
public sealed class SampleCompositionRoot
{
    private readonly GeneratedMviContainer _container;

    /// <summary>
    /// 初始化组合根。
    /// </summary>
    /// <param name="uiDispatcher">平台 UI 调度器。</param>
    public SampleCompositionRoot(IMviUiDispatcher uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        _container = new GeneratedMviContainer(uiDispatcher);
    }

    /// <summary>
    /// 创建主窗口。
    /// </summary>
    /// <returns>主窗口。</returns>
    public MainWindow CreateMainWindow()
    {
        AppComposition composition = _container
            .CreateAppCompositionAsync()
            .AsTask()
            .GetAwaiter()
            .GetResult();
        return new MainWindow(composition, _container);
    }
}
